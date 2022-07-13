using BetterSubnautica.Utility;
using HarmonyLib;

namespace BetterSubnautica.Patches
{
    [HarmonyPatch(typeof(uGUI_TabbedControlsPanel))]
    [HarmonyPatch(nameof(uGUI_TabbedControlsPanel.AddTab))]
    class uGUITabbedControlsPanelAddTabPatch
    {
        static void Postfix(string label, int __result)
        {
            switch (label)
            {
                case "Accessibility":
                    uGUIUtility.AccessibilityTabIndex = __result;
                    break;
                case "General":
                    uGUIUtility.GeneralTabIndex = __result;
                    break;
                case "Graphics":
                    uGUIUtility.GraphicsTabIndex = __result;
                    break;
                case "Keyboard":
                    uGUIUtility.KeyboardTabIndex = __result;
                    break;
            }
        }
    }
}
