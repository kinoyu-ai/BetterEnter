using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BeterEnter
{
    [DataContract]
    internal class Settings
    {
        [DataMember] public string ComposerId = "";
        public static readonly string DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeterEnter");
        public static readonly string FilePath = Path.Combine(DirectoryPath, "settings.json");
        public static Settings Load()
        {
            try {
                string path = File.Exists(FilePath) ? FilePath : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EnterFix", "settings.json");
                using (FileStream stream = File.OpenRead(path)) return (Settings)new DataContractJsonSerializer(typeof(Settings)).ReadObject(stream);
            }
            catch { return new Settings(); }
        }
        public void Save()
        {
            Directory.CreateDirectory(DirectoryPath);
            using (FileStream stream = File.Create(FilePath)) new DataContractJsonSerializer(typeof(Settings)).WriteObject(stream, this);
        }
    }

    internal sealed class FocusSnapshot
    {
        public IntPtr Window, FocusWindow;
        public bool ChatGpt, Editable, Composer;
        public string Id = "", Name = "", Status = "ChatGPT の入力欄を待っています";
        public long Time;
    }

    internal sealed class BeterEnterContext : ApplicationContext
    {
        private readonly NotifyIcon tray;
        private readonly Icon activeIcon, pausedIcon;
        private readonly ContextMenuStrip trayMenu;
        private readonly Native.HookProc hookProc;
        private IntPtr hook;
        private volatile bool enabled = true, running = true;
        private bool suppressEnter;
        private volatile FocusSnapshot snapshot = new FocusSnapshot();
        private readonly Settings settings = Settings.Load();
        private readonly System.Windows.Forms.Timer uiTimer;
        private readonly ToolStripMenuItem enabledItem, statusItem, startupItem;
        private int sendFailures, reportedFailures, registerRequested, focusGeneration;
        private string notice;
        private int[] registeredRuntimeId;
        private readonly AutomationFocusChangedEventHandler focusChanged;
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public BeterEnterContext()
        {
            activeIcon = TrayIcons.Load(false); pausedIcon = TrayIcons.Load(true);
            ContextMenuStrip menu = new ContextMenuStrip(); trayMenu = menu;
            ToolStripMenuItem titleItem = new ToolStripMenuItem("BeterEnter 1.4.1"); titleItem.Enabled = false;
            menu.Items.Add(titleItem);
            enabledItem = new ToolStripMenuItem("有効（Enter：改行 / Ctrl+Enter：送信）");
            enabledItem.Checked = enabled;
            enabledItem.Click += delegate { SetEnabled(!enabled); };
            menu.Items.Add(enabledItem);
            statusItem = new ToolStripMenuItem("入力欄を待っています"); statusItem.Enabled = false;
            menu.Items.Add(statusItem); menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("入力欄を登録（5 秒後に判定）", null, delegate {
                Interlocked.Exchange(ref registerRequested, 1);
                tray.ShowBalloonTip(4500, "BeterEnter", "5 秒以内に ChatGPT アプリのメッセージ入力欄をクリックしてください。", ToolTipIcon.Info);
            });
            menu.Items.Add("登録を解除", null, delegate {
                lock (settings) { settings.ComposerId = ""; registeredRuntimeId = null; SaveSettings(); }
            });
            menu.Items.Add("診断情報", null, delegate {
                FocusSnapshot s = snapshot;
                MessageBox.Show("状態: " + s.Status + "\nChatGPT 判定: " + s.ChatGpt + "\n編集可能: " + s.Editable
                    + "\n入力欄 ID: " + s.Id + "\n入力欄ラベル: " + s.Name + "\nキー注入失敗: " + sendFailures
                    + "\n\n送信内容やキー入力のログは保存しません。", "BeterEnter 診断", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            startupItem = new ToolStripMenuItem("Windows 起動時に実行");
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey)) startupItem.Checked = key != null && key.GetValue("BeterEnter") != null;
            startupItem.Click += delegate {
                try {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey)) {
                        if (startupItem.Checked) key.DeleteValue("BeterEnter", false);
                        else key.SetValue("BeterEnter", "\"" + Application.ExecutablePath + "\"");
                    }
                    startupItem.Checked = !startupItem.Checked;
                } catch (Exception ex) { MessageBox.Show(ex.Message, "自動起動を変更できませんでした"); }
            };
            menu.Items.Add(startupItem); menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("終了", null, delegate { ExitThread(); });
            tray = new NotifyIcon { Icon = enabled ? activeIcon : pausedIcon, Text = "BeterEnter: " + (enabled ? "有効" : "一時停止中"), ContextMenuStrip = menu, Visible = true };
            tray.DoubleClick += delegate { SetEnabled(!enabled); };

            // UIA calls can block in another process. Run them outside the keyboard hook.
            focusChanged = delegate { Interlocked.Increment(ref focusGeneration); };
            Thread monitor = new Thread(MonitorFocus); monitor.IsBackground = true;
            monitor.SetApartmentState(ApartmentState.MTA); monitor.Start();
            hookProc = OnKey;
            hook = Native.SetWindowsHookEx(13, hookProc, Native.GetModuleHandle(null), 0);
            if (hook == IntPtr.Zero) { running = false; tray.Dispose(); menu.Dispose(); activeIcon.Dispose(); pausedIcon.Dispose(); throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()); }
            uiTimer = new System.Windows.Forms.Timer { Interval = 500 };
            uiTimer.Tick += delegate {
                statusItem.Text = enabled ? snapshot.Status : "一時停止中";
                string pendingNotice = Interlocked.Exchange(ref notice, null);
                if (pendingNotice != null) tray.ShowBalloonTip(4000, "BeterEnter", pendingNotice, ToolTipIcon.Info);
                if (sendFailures != reportedFailures) {
                    reportedFailures = sendFailures;
                    tray.ShowBalloonTip(5000, "BeterEnter", "キーの変更に失敗しました。ChatGPT と BeterEnter を通常権限で起動してください。", ToolTipIcon.Warning);
                }
            };
            uiTimer.Start();
            tray.ShowBalloonTip(3500, "BeterEnter を開始しました", "ChatGPT ではIME変換中を含め、Enterで改行、Ctrl+Enterで送信します。", ToolTipIcon.Info);
        }

        private void SetEnabled(bool value)
        {
            enabled = value; enabledItem.Checked = value;
            tray.Icon = value ? activeIcon : pausedIcon;
            tray.Text = "BeterEnter: " + (value ? "有効" : "一時停止中");
            statusItem.Text = value ? snapshot.Status : "一時停止中";
        }

        private void SaveSettings()
        {
            try { settings.Save(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "設定を保存できませんでした"); }
        }

        private void MonitorFocus()
        {
            long registerAt = 0;
            try { Automation.AddAutomationFocusChangedEventHandler(focusChanged); } catch { }
            while (running)
            {
                if (Interlocked.Exchange(ref registerRequested, 0) == 1) registerAt = Stopwatch.GetTimestamp() + 5 * Stopwatch.Frequency;
                FocusSnapshot next = new FocusSnapshot();
                int generation = Thread.VolatileRead(ref focusGeneration);
                try
                {
                    next.Window = Native.GetForegroundWindow();
                    uint pid; Native.GetWindowThreadProcessId(next.Window, out pid);
                    next.ChatGpt = IsChatGptWindow(next.Window);
                    if (next.ChatGpt)
                    {
                        AutomationElement element = AutomationElement.FocusedElement;
                        AutomationElement.AutomationElementInformation info = element.Current;
                        if (info.ProcessId == pid)
                        {
                            next.Id = info.AutomationId; next.Name = info.Name;
                            next.Editable = info.IsEnabled && info.HasKeyboardFocus && !info.IsPassword
                                && (info.ControlType == ControlType.Edit || info.ControlType == ControlType.Document);
                            object value;
                            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out value) && ((ValuePattern)value).Current.IsReadOnly) next.Editable = false;
                            next.FocusWindow = Native.FocusWindow(next.Window);
                            if (registerAt != 0 && Stopwatch.GetTimestamp() >= registerAt)
                            {
                                registerAt = 0;
                                if (next.Editable) {
                                    lock (settings) { settings.ComposerId = next.Id; registeredRuntimeId = String.IsNullOrEmpty(next.Id) ? element.GetRuntimeId() : null; settings.Save(); }
                                    notice = String.IsNullOrEmpty(next.Id) ? "入力欄を今回の起動中だけ登録しました。画面の再作成後は再登録してください。" : "メッセージ入力欄を登録しました。";
                                } else notice = "入力欄を識別できませんでした。ChatGPT のメッセージ入力欄で再試行してください。";
                            }
                            lock (settings) next.Composer = KeyPolicy.IsComposer(next.Id, next.Name, next.Editable, settings.ComposerId)
                                || (next.Editable && SameRuntimeId(registeredRuntimeId, element.GetRuntimeId()));
                            next.Status = next.Composer ? "ChatGPT 入力欄：有効" : "ChatGPT：入力欄の外（必要なら登録）";
                        }
                    }
                    if (registerAt != 0 && Stopwatch.GetTimestamp() >= registerAt) {
                        registerAt = 0;
                        notice = "ChatGPT アプリの入力欄が見つかりませんでした。";
                    }
                    if (Native.GetForegroundWindow() != next.Window || generation != Thread.VolatileRead(ref focusGeneration)) next.Composer = false;
                    next.Time = Stopwatch.GetTimestamp();
                }
                catch { next.Composer = false; next.Status = "入力欄を検出できません（診断情報を確認）"; }
                snapshot = next;
                Thread.Sleep(80);
            }
            try { Automation.RemoveAutomationFocusChangedEventHandler(focusChanged); } catch { }
        }

        private static bool SameRuntimeId(int[] first, int[] second)
        {
            if (first == null || second == null || first.Length != second.Length) return false;
            for (int i = 0; i < first.Length; i++) if (first[i] != second[i]) return false;
            return true;
        }

        private static bool IsChatGptWindow(IntPtr window)
        {
            if (window == IntPtr.Zero) return false;
            try
            {
                uint pid; Native.GetWindowThreadProcessId(window, out pid);
                using (Process process = Process.GetProcessById((int)pid))
                {
                    string path = null;
                    try { path = process.MainModule.FileName; } catch { }
                    return KeyPolicy.IsChatGpt(process.ProcessName, path);
                }
            }
            catch { return false; }
        }

        private IntPtr OnKey(int code, IntPtr message, IntPtr data)
        {
            if (code < 0) return Native.CallNextHookEx(hook, code, message, data);
            Native.HookData key = (Native.HookData)Marshal.PtrToStructure(data, typeof(Native.HookData));
            if (key.extra == Native.Tag || key.vk != Native.Enter) return Native.CallNextHookEx(hook, code, message, data);
            int msg = message.ToInt32();
            bool up = msg == 0x101 || msg == 0x105;
            if (suppressEnter) { if (up) suppressEnter = false; return new IntPtr(1); }
            if (up) return Native.CallNextHookEx(hook, code, message, data);
            try
            {
                FocusSnapshot s = snapshot;
                IntPtr foreground = Native.GetForegroundWindow();
                bool chatGpt = s.ChatGpt && s.Window == foreground
                    && Stopwatch.GetTimestamp() - s.Time < Stopwatch.Frequency / 2;
                if (!chatGpt) chatGpt = IsChatGptWindow(foreground);
                EnterAction action = KeyPolicy.Decide(enabled, chatGpt, Native.Down(Native.Ctrl), Native.Down(Native.Shift),
                    Native.Down(Native.Alt), Native.Down(Native.LWin) || Native.Down(Native.RWin));
                if (action != EnterAction.Pass)
                {
                    suppressEnter = true;
                    if (!Native.SendEnter(action)) Interlocked.Increment(ref sendFailures);
                    return new IntPtr(1);
                }
            }
            catch { /* Leave normal keyboard behavior intact if detection fails. */ }
            return Native.CallNextHookEx(hook, code, message, data);
        }

        protected override void ExitThreadCore()
        {
            running = false;
            if (hook != IntPtr.Zero) Native.UnhookWindowsHookEx(hook);
            uiTimer.Stop(); uiTimer.Dispose(); tray.Visible = false; tray.Dispose(); trayMenu.Dispose();
            activeIcon.Dispose(); pausedIcon.Dispose();
            base.ExitThreadCore();
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool first;
            using (Mutex mutex = new Mutex(true, @"Local\BeterEnter.ChatGPT.1", out first))
            {
                if (!first) { MessageBox.Show("BeterEnter はすでに起動しています。タスクトレイを確認してください。", "BeterEnter"); return; }
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                try { Application.Run(new BeterEnterContext()); }
                catch (Exception ex) { MessageBox.Show(ex.Message, "BeterEnter を起動できませんでした"); }
            }
        }
    }
}
