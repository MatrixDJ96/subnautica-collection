#if SUBNAUTICA

namespace BetterLights.MonoBehaviours.Lights
{
    public class SeamothLightsController : AbstractLightsController<SeaMoth>
    {
        protected override void GetSettings()
        {
            Color = Core.SeamothSettings.LightsColor;
            IntensityOffset = Core.SeamothSettings.LightsIntensityOffset;
            RangeOffset = Core.SeamothSettings.LightsRangeOffset;
        }
    }
}
#endif
