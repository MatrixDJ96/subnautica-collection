#if BELOWZERO
using BetterSubnautica.Components;
using BetterSubnautica.MonoBehaviours;

namespace BetterLights.MonoBehaviours
{
    public class SeatruckLightsContainer : AbstractSingletonContainer<SeatruckLightsContainer, int, IToggleLightsController>
    {
        private SeatruckLightsContainer() { }
    }
}
#endif
