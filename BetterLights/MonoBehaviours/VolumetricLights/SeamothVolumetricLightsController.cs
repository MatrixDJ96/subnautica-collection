#if SUBNAUTICA
namespace BetterLights.MonoBehaviours.VolumetricLights
{
    public class SeamothVolumetricLightsController : AbstractVolumetricLightsController<SeaMoth>
    {
        protected override void Awake()
        {
            base.Awake();

            if (component != null)
            {
                volumetricLights = component.volumeticLights;
            }
        }

        protected override void UpdateSettings()
        {
            IntensityOffset = Core.SeamothSettings.VolumetricLightsIntensityOffset;
        }
    }
}
#endif
