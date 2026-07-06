using BetterLights.MonoBehaviours.VolumetricLights;
using BetterSubnautica.MonoBehaviours;

namespace BetterLights.MonoBehaviours
{
    public class VolumetricLightsContainer : AbstractSingletonContainer<VolumetricLightsContainer, int, IVolumetricLightsController>
    {
        private VolumetricLightsContainer() { }
    }
}
