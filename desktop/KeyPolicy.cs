using System;

namespace BeterEnter
{
    internal enum EnterAction { Pass, Newline, Send }

    internal static class KeyPolicy
    {
        public static EnterAction Decide(bool enabled, bool chatGpt, bool ctrl, bool shift, bool alt, bool win, bool composing)
        {
            if (!enabled || !chatGpt || alt || win || composing) return EnterAction.Pass;
            if (ctrl) return EnterAction.Send;
            return shift ? EnterAction.Pass : EnterAction.Newline;
        }

        public static bool IsChatGpt(string processName, string executablePath)
        {
            if (!String.Equals(processName, "ChatGPT", StringComparison.OrdinalIgnoreCase)) return false;
            // Codex currently shares the ChatGPT.exe filename. Fail closed for its package.
            return String.IsNullOrEmpty(executablePath) || (executablePath.IndexOf("Codex", StringComparison.OrdinalIgnoreCase) < 0
                && executablePath.EndsWith("\\ChatGPT.exe", StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsComposer(string id, string name, bool editable, string customId)
        {
            if (!editable) return false;
            if (id == "prompt-textarea") return true;
            if (!String.IsNullOrEmpty(customId) && id == customId) return true;
            return name == "Message ChatGPT" || name == "Ask anything" || name == "Send a message"
                || name == "ChatGPT にメッセージを送信する" || name == "ChatGPT にメッセージを送信"
                || name == "ChatGPT にメッセージ" || name == "質問してみましょう" || name == "メッセージを送信する";
        }
    }
}
