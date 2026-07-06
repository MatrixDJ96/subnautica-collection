#if SUBNAUTICA
using BetterSubnautica.Utility;

namespace BetterQuickSlots
{
    public static class Buttons
    {
        public const string Category = "Better Quick Slots";

        // Index 0 = slot 6: the vanilla Slot1-Slot5 buttons already cover the native slots.
        public static GameInput.Button[] ExtraSlots { get; } = new GameInput.Button[5];

        internal static void Register()
        {
            ExtraSlots[0] = ModInputUtility.RegisterButton("BetterQuickSlotsSlot6", "Slot 6 Toggle", "Selects quick slot 6.", Category, Core.Settings.Slot6);
            ExtraSlots[1] = ModInputUtility.RegisterButton("BetterQuickSlotsSlot7", "Slot 7 Toggle", "Selects quick slot 7.", Category, Core.Settings.Slot7);
            ExtraSlots[2] = ModInputUtility.RegisterButton("BetterQuickSlotsSlot8", "Slot 8 Toggle", "Selects quick slot 8.", Category, Core.Settings.Slot8);
            ExtraSlots[3] = ModInputUtility.RegisterButton("BetterQuickSlotsSlot9", "Slot 9 Toggle", "Selects quick slot 9.", Category, Core.Settings.Slot9);
            ExtraSlots[4] = ModInputUtility.RegisterButton("BetterQuickSlotsSlot10", "Slot 10 Toggle", "Selects quick slot 10.", Category, Core.Settings.Slot10);
        }
    }
}
#elif BELOWZERO
using BetterQuickSlots.MonoBehaviours;
using BetterSubnautica.Utility;

namespace BetterQuickSlots
{
    public static class Buttons
    {
        public const string Category = "Better Quick Slots";

        internal static void Register()
        {
            var settings = Plugin.Core.Settings;

            ModInputUtility.RegisterKeybind(Category, "Slot 1 Toggle", "Selects quick slot 1.", settings, () => settings.Slot1, value => settings.Slot1 = value, ForceUpdateQuickSlots);
            ModInputUtility.RegisterKeybind(Category, "Slot 2 Toggle", "Selects quick slot 2.", settings, () => settings.Slot2, value => settings.Slot2 = value, ForceUpdateQuickSlots);
            ModInputUtility.RegisterKeybind(Category, "Slot 3 Toggle", "Selects quick slot 3.", settings, () => settings.Slot3, value => settings.Slot3 = value, ForceUpdateQuickSlots);
            ModInputUtility.RegisterKeybind(Category, "Slot 4 Toggle", "Selects quick slot 4.", settings, () => settings.Slot4, value => settings.Slot4 = value, ForceUpdateQuickSlots);
            ModInputUtility.RegisterKeybind(Category, "Slot 5 Toggle", "Selects quick slot 5.", settings, () => settings.Slot5, value => settings.Slot5 = value, ForceUpdateQuickSlots);
            ModInputUtility.RegisterKeybind(Category, "Slot 6 Toggle", "Selects quick slot 6.", settings, () => settings.Slot6, value => settings.Slot6 = value, ForceUpdateQuickSlots);
            ModInputUtility.RegisterKeybind(Category, "Slot 7 Toggle", "Selects quick slot 7.", settings, () => settings.Slot7, value => settings.Slot7 = value, ForceUpdateQuickSlots);
            ModInputUtility.RegisterKeybind(Category, "Slot 8 Toggle", "Selects quick slot 8.", settings, () => settings.Slot8, value => settings.Slot8 = value, ForceUpdateQuickSlots);
            ModInputUtility.RegisterKeybind(Category, "Slot 9 Toggle", "Selects quick slot 9.", settings, () => settings.Slot9, value => settings.Slot9 = value, ForceUpdateQuickSlots);
            ModInputUtility.RegisterKeybind(Category, "Slot 10 Toggle", "Selects quick slot 10.", settings, () => settings.Slot10, value => settings.Slot10 = value, ForceUpdateQuickSlots);
        }

        private static void ForceUpdateQuickSlots()
        {
            if (QuickSlotsController.Instance != null)
            {
                QuickSlotsController.Instance.ForceUpdate = true;
            }
        }
    }
}
#endif
