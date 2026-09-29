using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BeterEnter
{
    internal static class Native
    {
        public const int Enter = 0x0D, Shift = 0x10, Ctrl = 0x11, Alt = 0x12;
        public const int LShift = 0xA0, RShift = 0xA1, LCtrl = 0xA2, RCtrl = 0xA3, LWin = 0x5B, RWin = 0x5C;
        public static readonly UIntPtr Tag = new UIntPtr(0x45465831);
        public delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);

        [StructLayout(LayoutKind.Sequential)]
        public struct HookData { public uint vk, scan, flags, time; public UIntPtr extra; }
        [StructLayout(LayoutKind.Sequential)]
        public struct GuiInfo
        {
            public int size; public uint flags; public IntPtr active, focus, capture, menuOwner, moveSize, caret;
            public int left, top, right, bottom;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct KeyboardInput { public ushort vk, scan; public uint flags, time; public UIntPtr extra; }
        [StructLayout(LayoutKind.Sequential)]
        internal struct MouseInput { public int x, y; public uint data, flags, time; public UIntPtr extra; }
        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion
        {
            [FieldOffset(0)] public KeyboardInput keyboard;
            [FieldOffset(0)] public MouseInput mouse;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct Input { public uint type; public InputUnion data; }

        [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetWindowsHookEx(int id, HookProc proc, IntPtr module, uint thread);
        [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr GetModuleHandle(string module);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("user32.dll")] public static extern bool GetGUIThreadInfo(uint thread, ref GuiInfo info);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);

        public static bool Down(int key) { return (GetAsyncKeyState(key) & 0x8000) != 0; }
        public static IntPtr FocusWindow(IntPtr foreground)
        {
            uint process;
            uint thread = GetWindowThreadProcessId(foreground, out process);
            GuiInfo info = new GuiInfo(); info.size = Marshal.SizeOf(typeof(GuiInfo));
            return GetGUIThreadInfo(thread, ref info) && info.focus != IntPtr.Zero ? info.focus : foreground;
        }

        private static Input Key(int vk, bool up)
        {
            Input input = new Input(); input.type = 1;
            input.data.keyboard.vk = (ushort)vk;
            input.data.keyboard.flags = (up ? 2u : 0u) | (vk == RCtrl ? 1u : 0u);
            input.data.keyboard.extra = Tag;
            return input;
        }
        public static bool SendEnter(EnterAction action)
        {
            List<Input> inputs = new List<Input>();
            List<int> released = new List<int>();
            foreach (int vk in new int[] { LCtrl, RCtrl, LShift, RShift })
            {
                if (Down(vk)) { released.Add(vk); inputs.Add(Key(vk, true)); }
            }
            if (action == EnterAction.Newline) inputs.Add(Key(LShift, false));
            inputs.Add(Key(Enter, false)); inputs.Add(Key(Enter, true));
            if (action == EnterAction.Newline) inputs.Add(Key(LShift, true));
            foreach (int vk in released) inputs.Add(Key(vk, false));
            uint sent = SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(Input)));
            if (sent == inputs.Count) return true;
            // Recover modifier state after a partial injection without sending another Enter.
            List<Input> recovery = new List<Input>();
            if (action == EnterAction.Newline && !released.Contains(LShift)) recovery.Add(Key(LShift, true));
            foreach (int vk in released) recovery.Add(Key(vk, false));
            if (recovery.Count > 0) SendInput((uint)recovery.Count, recovery.ToArray(), Marshal.SizeOf(typeof(Input)));
            return false;
        }
    }
}
