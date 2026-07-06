#if BELOWZERO
using Nautilus.Json;
using Nautilus.Options.Attributes;
using UnityEngine;

namespace BetterLights.Settings
{
    [Menu("Better Lights - Flashlight Helmet")]
    public class FlashlightHelmetSettings : ConfigFile
    {
        public FlashlightHelmetSettings() : base("config_flashlight_helmet") { }

        public KeyCode LightsButtonToggle { get; set; } = KeyCode.Mouse1;

        [ColorPicker("Lights Color", Advanced = true)]
        public Color LightsColor { get; set; } = Color.white;

        [Slider("Lights Range Offset", -100f, 100f, Step = 1f)]
        public float LightsRangeOffset { get; set; } = 0f;

        [Slider("Lights Intensity Offset", -5f, 5f, Format = "{0:F2}")]
        public float LightsIntensityOffset { get; set; } = 0f;
    }
}
#endif
