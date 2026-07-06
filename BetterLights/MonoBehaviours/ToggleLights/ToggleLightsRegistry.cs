using System.Collections.Generic;
using BetterSubnautica.Components;

namespace BetterLights.MonoBehaviours.ToggleLights
{
    // Exact instance-level ownership of the vanilla ToggleLights components: the hierarchy
    // lookup from the vanilla child object misses the controller on some spawn paths, so the
    // vanilla-toggle patches ask this registry instead.
    public static class ToggleLightsRegistry
    {
        private static readonly Dictionary<global::ToggleLights, IToggleLightsController> Owners = new Dictionary<global::ToggleLights, IToggleLightsController>();

        public static void Register(global::ToggleLights toggleLights, IToggleLightsController controller)
        {
            Owners[toggleLights] = controller;
        }

        public static void Unregister(global::ToggleLights toggleLights, IToggleLightsController controller)
        {
            if (Owners.TryGetValue(toggleLights, out var owner) && owner == controller)
            {
                Owners.Remove(toggleLights);
            }
        }

        public static bool IsOwned(global::ToggleLights toggleLights)
        {
            return toggleLights != null && Owners.ContainsKey(toggleLights);
        }

        public static bool TryGetOwner(global::ToggleLights toggleLights, out IToggleLightsController controller)
        {
            if (toggleLights != null)
            {
                return Owners.TryGetValue(toggleLights, out controller);
            }

            controller = null;
            return false;
        }
    }
}
