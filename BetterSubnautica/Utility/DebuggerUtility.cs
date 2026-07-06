using BetterSubnautica.MonoBehaviours;

namespace BetterSubnautica.Utility
{
    public static class DebuggerUtility
    {
        public static void ShowWarning(string text, string key = null, bool prefix = true)
        {
            DebuggerController.Instance.ShowWarning(text, key, prefix);
        }

        public static void ShowMessage(string text, string key = null, bool prefix = true)
        {
            DebuggerController.Instance.AddMessage(text, key, prefix);
        }

        public static void RemoveMessage(string key)
        {
            DebuggerController.Instance.RemoveMessage(key);
        }
    }
}
