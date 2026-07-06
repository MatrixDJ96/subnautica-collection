#if BELOWZERO
using BetterSubnautica.Extensions;

namespace BetterLights.MonoBehaviours.VolumetricLights
{
    public class HoverbikeVolumetricLightsController : AbstractVolumetricLightsController<Hoverbike>
    {
        protected override void Awake()
        {
            base.Awake();

            if (component != null)
            {
                var toggleLights = component.GetToggleLights();

                if (toggleLights == null || toggleLights.lightsParent == null)
                {
                    Destroy(this);
                    return;
                }

                volumetricLights = toggleLights.lightsParent.GetComponentsInChildren<VFXVolumetricLight>();
            }
        }

        protected override void UpdateSettings()
        {
            IntensityOffset = Core.HoverbikeSettings.VolumetricLightsIntensityOffset;
        }
    }
}
#endif
