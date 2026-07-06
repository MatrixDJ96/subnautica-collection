#if SUBNAUTICA
using BetterSubnautica.Utility;

namespace BetterSavegames
{
    public static class Buttons
    {
        public const string Category = "Better Savegames";

        public static GameInput.Button Quicksave { get; private set; }

        public static GameInput.Button Quickload { get; private set; }

        internal static void Register()
        {
            Quicksave = ModInputUtility.RegisterButton("BetterSavegamesQuicksave", "Quicksave Button", "Saves the game to the quicksave slot.", Category, Core.Settings.Quicksave);
            Quickload = ModInputUtility.RegisterButton("BetterSavegamesQuickload", "Quickload Button", "Loads the most recent savegame slot.", Category, Core.Settings.Quickload);
        }
    }
}
#elif BELOWZERO
using BetterSubnautica.Utility;

namespace BetterSavegames
{
    public static class Buttons
    {
        public const string Category = "Better Savegames";

        internal static void Register()
        {
            var settings = Plugin.Core.Settings;

            ModInputUtility.RegisterKeybind(Category, "Quicksave Button", "Saves the game to the quicksave slot.", settings, () => settings.Quicksave, value => settings.Quicksave = value);
            ModInputUtility.RegisterKeybind(Category, "Quickload Button", "Loads the most recent savegame slot.", settings, () => settings.Quickload, value => settings.Quickload = value);
        }
    }
}
#endif
