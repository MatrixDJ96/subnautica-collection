#if BELOWZERO

namespace BetterLights.MonoBehaviours.Lights
{
    public class HoverbikeLightsController : AbstractLightsController<Hoverbike>
    {
        protected override void GetSettings()
        {
            Color = Core.HoverbikeSettings.LightsColor;
            IntensityOffset = Core.HoverbikeSettings.LightsIntensityOffset;
            RangeOffset = Core.HoverbikeSettings.LightsRangeOffset;
        }
    }
}
#endif
