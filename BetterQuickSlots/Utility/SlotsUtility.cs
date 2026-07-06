using BetterSubnautica.Utility;
using HarmonyLib;
using Nautilus.Options.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BetterQuickSlots.Utility
{
    public class SlotsUtility
    {
#if SUBNAUTICA
        public static int VanillaSlotCount => uGUI_QuickSlots.quickSlotButtons.Length;
#else
        public static int VanillaSlotCount => Player.quickSlotButtonsCount;
#endif

        public static KeyCode Slot1 => Core.Settings.Slot1;
        public static KeyCode Slot2 => Core.Settings.Slot2;
        public static KeyCode Slot3 => Core.Settings.Slot3;
        public static KeyCode Slot4 => Core.Settings.Slot4;
        public static KeyCode Slot5 => Core.Settings.Slot5;
        public static KeyCode Slot6 => Core.Settings.Slot6;
        public static KeyCode Slot7 => Core.Settings.Slot7;
        public static KeyCode Slot8 => Core.Settings.Slot8;
        public static KeyCode Slot9 => Core.Settings.Slot9;
        public static KeyCode Slot10 => Core.Settings.Slot10;

        private static string[] CreateSlotNames()
        {
            if (Core.Settings.GetType().GetProperty(nameof(Core.Settings.SlotCount)) is { } property)
            {
                var attribute = property.GetCustomAttribute<SliderAttribute>();

                if (attribute != null)
                {
                    var slotCount = (int)attribute.Max;
                    var slotNames = new string[slotCount + 1];

                    for (int i = 0; i < slotNames.Length; i++)
                    {
                        slotNames[i] = "QuickSlot" + i;
                    }

                    return slotNames;
                }
            }

            return QuickSlots.slotNames;
        }

        public static string[] SlotNames = CreateSlotNames();

        public static string GetInputSlotName(int slot, bool withColor = true)
        {
            if (slot < SlotNames.Length)
            {
#if SUBNAUTICA
                if (slot < VanillaSlotCount)
                {
                    return ModInputUtility.GetButtonName(uGUI_QuickSlots.quickSlotButtons[slot], withColor);
                }

                if (slot - VanillaSlotCount < Buttons.ExtraSlots.Length)
                {
                    return ModInputUtility.GetButtonName(Buttons.ExtraSlots[slot - VanillaSlotCount], withColor);
                }
#else
                var bindingFlags = BindingFlags.Public | BindingFlags.Static;

                if (typeof(SlotsUtility).GetProperty($"Slot{slot + 1}", bindingFlags) is { } property)
                {
                    return KeyCodeUtility.GetName((KeyCode)property.GetValue(null), withColor);
                }
#endif
            }

            return "";
        }

#if BELOWZERO
        public static void UpdateSlotBindings()
        {
            var device = GameInput.Device.Keyboard;
            var bindingSet = GameInput.BindingSet.Primary;
            var bindingFlags = BindingFlags.Public | BindingFlags.Static;

            for (int i = 0; i < VanillaSlotCount; i++)
            {
                if (typeof(SlotsUtility).GetProperty($"Slot{i + 1}", bindingFlags) is { } property)
                {
                    var keyCode = (KeyCode)property.GetValue(null);
                    var button = (GameInput.Button)Enum.Parse(typeof(GameInput.Button), property.Name);

                    if (GameInput.IsBindable(device, button))
                    {
                        KeyCodeUtility.SetKeyCode(button, keyCode, device, bindingSet);
                    }
                }
            }
        }
#endif

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = new List<CodeInstruction>(instructions);

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].operand is FieldInfo operand && operand == AccessTools.Field(typeof(QuickSlots), nameof(QuickSlots.slotNames)))
                {
                    list[i].operand = AccessTools.Field(typeof(SlotsUtility), nameof(SlotNames));
                }
            }

            return list;
        }
    }
}
