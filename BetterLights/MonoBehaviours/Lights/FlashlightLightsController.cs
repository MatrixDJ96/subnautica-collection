namespace BetterLights.MonoBehaviours.Lights
{
    public class FlashlightLightsController : AbstractLightsController<FlashLight>
    {
        protected override void GetSettings()
        {
            Color = Core.FlashlightSettings.LightsColor;
            IntensityOffset = Core.FlashlightSettings.LightsIntensityOffset;
            RangeOffset = Core.FlashlightSettings.LightsRangeOffset;
        }
    }
}
