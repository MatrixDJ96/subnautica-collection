#if BELOWZERO
using BetterSubnautica.Components;
using HarmonyLib;

namespace BetterLights.Patches
{
    [HarmonyPatch(typeof(Dockable))]
    [HarmonyPatch(nameof(Dockable.OnDockingStart))]
    class DockableOnDockingStartPatch
    {
        static void Prefix(Dockable __instance)
        {
            if (__instance.gameObject.GetComponent<IToggleLightsController>() is { } controller)
            {
                controller.SetLightsActive(false, true);
            }
        }
    }

    [HarmonyPatch(typeof(Dockable))]
    [HarmonyPatch(nameof(Dockable.OnUndockingComplete))]
    class DockableOnUndockingCompletePatch
    {
        static void Prefix(Dockable __instance)
        {
            if (__instance.gameObject.GetComponent<IToggleLightsController>() is { } controller)
            {
                controller.SetLightsActive(Core.VehiclesSettings.EnableLightsOnUndocking, true);
            }
        }
    }
}
#endif
