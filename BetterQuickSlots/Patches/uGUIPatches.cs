using BetterQuickSlots.MonoBehaviours;
using BetterQuickSlots.Utility;
using BetterSubnautica.Utility;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace BetterQuickSlots.Patches
{
    [HarmonyPatch(typeof(uGUI))]
    [HarmonyPatch(nameof(uGUI.Awake))]
    class uGUIAwakePatch
    {
        static void Postfix(uGUI __instance)
        {
            if (uGUI.main == __instance && __instance.quickSlots is uGUI_QuickSlots quickSlots)
            {
                if (quickSlots.gameObject.GetComponent<QuickSlotsController>() == null)
                {
                    quickSlots.gameObject.AddComponent<QuickSlotsController>();
                }
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_QuickSlots))]
    [HarmonyPatch(nameof(uGUI_QuickSlots.HandleInput))]
    class uGUIQuickSlotsHandleInputPatch
    {
        static void Postfix(uGUI_QuickSlots __instance)
        {
            if (Player.main.GetCanItemBeUsed() && !uGUI.isIntro && !uGUI.isLoading && __instance.target != null)
            {
                var bindingFlags = BindingFlags.Public | BindingFlags.Static;

                for (int i = Player.quickSlotButtonsCount; i < Core.Settings.SlotCount; i++)
                {
                    if (typeof(SlotsUtility).GetProperty($"Slot{i + 1}", bindingFlags) is PropertyInfo property)
                    {
                        var keyCode = (KeyCode)property.GetValue(null);

                        if (Input.GetKeyDown(keyCode))
                        {
                            __instance.target.SlotKeyDown(i);
                        }
                        if (Input.GetKeyUp(keyCode))
                        {
                            __instance.target.SlotKeyUp(i);
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_TabbedControlsPanel))]
    [HarmonyPatch(
        nameof(uGUI_TabbedControlsPanel.AddBindingOption),
        new Type[] { typeof(int), typeof(string), typeof(GameInput.Device), typeof(GameInput.Button), typeof(GameObject) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out }
    )]
    class uGUITabbedControlsPanelAddBindingOptionPatch
    {
        static void Postfix(uGUI_Bindings __result, int tabIndex, string label, GameInput.Device device, GameInput.Button button, GameObject bindingObject)
        {
            if (tabIndex == uGUIUtility.KeyboardTabIndex && device == GameInput.Device.Keyboard)
            {
                var bindingFlags = BindingFlags.Public | BindingFlags.Static;

                for (int i = 0; i < Player.quickSlotButtonsCount; i++)
                {
                    if (typeof(SlotsUtility).GetProperty($"Slot{i + 1}", bindingFlags) is PropertyInfo property)
                    {
                        if ("Option" + property.Name == label)
                        {
                            __result.transform.parent.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }
}
