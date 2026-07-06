namespace BetterLights.MonoBehaviours.Lights
{
    public class SeaglideLightsController : AbstractLightsController<Seaglide>
    {
        protected override void GetSettings()
        {
            Color = Core.SeaglideSettings.LightsColor;
            IntensityOffset = Core.SeaglideSettings.LightsIntensityOffset;
            RangeOffset = Core.SeaglideSettings.LightsRangeOffset;
        }
    }
}
