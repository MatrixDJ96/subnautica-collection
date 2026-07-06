using Nautilus.Json;
using Nautilus.Options.Attributes;

namespace BetterHUD
{
    [Menu("Better HUD")]
    public class Settings : ConfigFile
    {
        [Toggle("Show HUD Clock")]
        public bool ShowHUDClock { get; set; } = true;

        [Slider("Time Font Size", 0, 100)]
        public int TimeFontSize { get; set; } = 32;

        [Choice("Time Font Style", new[] { "Normal", "Bold", "Italic", "Bold & Italic" })]
        public int TimeFontStyle { get; set; } = 0;

        [Slider("Time Position X", 0, 512)]
        public int TimePositionX { get; set; } = 10;

        [Slider("Time Position Y", 0, 256)]
        public int TimePositionY { get; set; } = 10;
    }
}
