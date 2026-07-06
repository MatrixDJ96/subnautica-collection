using BetterSubnautica.Utility;
using HarmonyLib;
using UnityEngine.Events;

namespace BetterPDA.Patches
{
    [HarmonyPatch(typeof(uGUI_TabbedControlsPanel))]
    [HarmonyPatch(nameof(uGUI_TabbedControlsPanel.AddToggleOption))]
    class uGUITabbedControlsPanelAddToggleOptionPatch
    {
        static bool Prefix(int tabIndex, string label, bool value, UnityAction<bool> callback = null)
        {
            return !(label == "PDAPause" && tabIndex == uGUIUtility.AccessibilityTabIndex);
        }
    }
}
