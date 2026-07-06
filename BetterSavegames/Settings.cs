using Nautilus.Json;
using Nautilus.Options.Attributes;
using UnityEngine;

namespace BetterSavegames
{
    [Menu("Better Savegames")]
    public class Settings : ConfigFile
    {
#if SUBNAUTICA
        [Keybind("Quicksave Button")]
#endif
        public KeyCode Quicksave { get; set; } = KeyCode.F5;

#if SUBNAUTICA
        [Keybind("Quickload Button")]
#endif
        public KeyCode Quickload { get; set; } = KeyCode.F9;

        [Toggle("Enable Autosave")]
        public bool EnableAutosave { get; set; } = true;

        [Slider("Autosave Interval", Tooltip = "Interval in minutes", Min = 1, Max = 60)]
        public int AutosaveInterval { get; set; } = 10;

        [Toggle("Autoload Latest Savegame", Tooltip = "Load latest savegame on game launch")]
        public bool AutoloadLatestSavegame { get; set; } = false;

        [Toggle("Maximize Loading Speed", Tooltip = "Unlock FPS while loading to maximize speed")]
        public bool MaximizeLoadingSpeed { get; set; } = true;
    }
}
