using BetterQuickSlots.MonoBehaviours;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options.Attributes;
using UnityEngine;

namespace BetterQuickSlots
{
    [Menu("Better Quick Slots")]
    public class Settings : ConfigFile
    {
        [Slider("Slot Count", 5f, 10f, Step = 1f), OnChange(nameof(ForceUpdateQuickSlots))]
        public int SlotCount { get; set; } = 10;

        [Keybind("Slot 1 Toggle"), OnChange(nameof(ForceUpdateQuickSlots))]
        public KeyCode Slot1 { get; set; } = KeyCode.Alpha1;

        [Keybind("Slot 2 Toggle"), OnChange(nameof(ForceUpdateQuickSlots))]
        public KeyCode Slot2 { get; set; } = KeyCode.Alpha2;

        [Keybind("Slot 3 Toggle"), OnChange(nameof(ForceUpdateQuickSlots))]
        public KeyCode Slot3 { get; set; } = KeyCode.Alpha3;

        [Keybind("Slot 4 Toggle"), OnChange(nameof(ForceUpdateQuickSlots))]
        public KeyCode Slot4 { get; set; } = KeyCode.Alpha4;

        [Keybind("Slot 5 Toggle"), OnChange(nameof(ForceUpdateQuickSlots))]
        public KeyCode Slot5 { get; set; } = KeyCode.Alpha5;

        [Keybind("Slot 6 Toggle"), OnChange(nameof(ForceUpdateQuickSlots))]
        public KeyCode Slot6 { get; set; } = KeyCode.Alpha6;

        [Keybind("Slot 7 Toggle"), OnChange(nameof(ForceUpdateQuickSlots))]
        public KeyCode Slot7 { get; set; } = KeyCode.Alpha7;

        [Keybind("Slot 8 Toggle"), OnChange(nameof(ForceUpdateQuickSlots))]
        public KeyCode Slot8 { get; set; } = KeyCode.Alpha8;

        [Keybind("Slot 9 Toggle"), OnChange(nameof(ForceUpdateQuickSlots))]
        public KeyCode Slot9 { get; set; } = KeyCode.Alpha9;

        [Keybind("Slot 10 Toggle"), OnChange(nameof(ForceUpdateQuickSlots))]
        public KeyCode Slot10 { get; set; } = KeyCode.Alpha0;

        [Slider("Text Font Size", 0, 100), OnChange(nameof(ForceUpdateQuickSlots))]
        public int TextFontSize { get; set; } = 12;

        [Slider("Text Offset Y", 0, 100), OnChange(nameof(ForceUpdateQuickSlots))]
        public int TextOffsetY { get; set; } = 8;

        private void ForceUpdateQuickSlots()
        {
            if (QuickSlotsController.Instance != null)
            {
                QuickSlotsController.Instance.ForceUpdate = true;
            }
        }
    }
}
