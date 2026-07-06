#if SUBNAUTICA
namespace BetterLights.MonoBehaviours.VolumetricLights
{
    public class CyclopsVolumetricLightsController : AbstractVolumetricLightsController<SubRoot>
    {
        protected override void Awake()
        {
            base.Awake();

            if (component != null)
            {
                volumetricLights = component.dimFloodlightsOnEnter;
            }
        }

        protected override void UpdateSettings()
        {
            IntensityOffset = Core.CyclopsSettings.VolumetricLightsIntensityOffset;
        }
    }
}
#endif
