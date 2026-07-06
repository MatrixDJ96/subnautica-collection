using System.Collections.Generic;
using BetterSubnautica.Components;

namespace BetterLights.MonoBehaviours.ToggleLights
{
    // Exact instance-level ownership of the vanilla ToggleLights components: the hierarchy
    // lookup from the vanilla child object misses the controller on some spawn paths, so the
    // vanilla-toggle patches ask this registry instead.
    public static class ToggleLightsRegistry
    {
        private static readonly Dictionary<global::ToggleLights, IToggleLightsController> owners = new Dictionary<global::ToggleLights, IToggleLightsController>();

        public static void Register(global::ToggleLights toggleLights, IToggleLightsController controller)
        {
            owners[toggleLights] = controller;
        }

        public static void Unregister(global::ToggleLights toggleLights, IToggleLightsController controller)
        {
            if (owners.TryGetValue(toggleLights, out var owner) && owner == controller)
            {
                owners.Remove(toggleLights);
            }
        }

        public static bool IsOwned(global::ToggleLights toggleLights)
        {
            return toggleLights != null && owners.ContainsKey(toggleLights);
        }

        public static bool TryGetOwner(global::ToggleLights toggleLights, out IToggleLightsController controller)
        {
            if (toggleLights != null)
            {
                return owners.TryGetValue(toggleLights, out controller);
            }

            controller = null;
            return false;
        }
    }
}
