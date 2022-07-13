#if SUBNAUTICA
using BetterLights.MonoBehaviours.ToggleLights;
using HarmonyLib;

namespace BetterLights.Patches
{
    [HarmonyPatch(typeof(Vehicle))]
    [HarmonyPatch(nameof(Vehicle.OnDockedChanged))]
    class VehicleOnDockedChangedPatch
    {
        static void Prefix(Vehicle __instance, bool docked, Vehicle.DockType dockType)
        {
            if (__instance.gameObject.GetComponent<IToggleLightsController>() is IToggleLightsController controller)
            {
                if (docked || (!docked && Core.VehiclesSettings.EnableLightsOnUndocking))
                {
                    controller.SetLightsActive(!docked, true);
                }
            }
        }
    }
}
#endif
