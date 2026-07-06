#if SUBNAUTICA
using Nautilus.Json;
using Nautilus.Options.Attributes;

namespace BetterLights.Settings
{
    [Menu("Better Lights - Cyclops")]
    public class CyclopsSettings : ConfigFile
    {
        public CyclopsSettings() : base("config_cyclops") { }

        [Slider("Int. Lights Consumption", 0f, 0.2f, Step = 0.001f, Format = "{0:F3}")]
        public float InternalLightsConsumption { get; set; } = 0.012f;

        [Slider("Ext. Lights Consumption", 0f, 0.2f, Step = 0.001f, Format = "{0:F3}")]
        public float ExternalLightsConsumption { get; set; } = 0.025f;

        [Slider("Cam. Lights Consumption", 0f, 0.2f, Step = 0.001f, Format = "{0:F3}")]
        public float CameraLightsConsumption { get; set; } = 0.008f;

        [Slider("Ext. Lights Range Offset", -100, 100, Step = 1f)]
        public float ExternalLightsRangeOffset { get; set; } = 30f;

        [Slider("Cam. Lights Range Offset", -100, 100, Step = 1f)]
        public float CameraLightsRangeOffset { get; set; } = 25f;

        [Slider("Ext. Lights Intensity Offset", -5f, 5f, Format = "{0:F2}")]
        public float ExternalLightsIntensityOffset { get; set; } = 0f;

        [Slider("Cam. Lights Intensity Offset", -5f, 5f, Format = "{0:F2}")]
        public float CameraLightsIntensityOffset { get; set; } = 0.5f;

        [Slider("Vol. Lights Intensity Offset", -5f, 5f, Format = "{0:F2}")]
        public float VolumetricLightsIntensityOffset { get; set; } = 0f;
    }
}
#endif
