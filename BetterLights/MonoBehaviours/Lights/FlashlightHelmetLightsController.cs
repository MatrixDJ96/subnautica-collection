#if BELOWZERO

namespace BetterLights.MonoBehaviours.Lights
{
    public class FlashlightHelmetLightsController : AbstractLightsController<FlashlightHelmet>
    {
        protected override void GetSettings()
        {
            Color = Core.FlashlightHelmetSettings.LightsColor;
            IntensityOffset = Core.FlashlightHelmetSettings.LightsIntensityOffset;
            RangeOffset = Core.FlashlightHelmetSettings.LightsRangeOffset;
        }
    }
}
#endif
