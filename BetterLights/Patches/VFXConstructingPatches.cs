#if BELOWZERO
using BetterLights.MonoBehaviours.ToggleLights;
using BetterSubnautica.Extensions;
using HarmonyLib;

namespace BetterLights.Patches
{
    [HarmonyPatch(typeof(VFXConstructing))]
    [HarmonyPatch(nameof(VFXConstructing.WakeUpSubmarine))]
    class VFXConstructingWakeUpSubmarinePatch
    {
        static void Postfix(VFXConstructing __instance)
        {
            if (__instance.gameObject.GetComponent<SeaTruckSegment>() is { } seatruck && seatruck.IsMainSegment())
            {
                // During the construction animation the light children are inactive, so the
                // controller added by the Start patch destroys itself: re-ensure it now that
                // the vehicle is complete.
                if (seatruck.gameObject.EnsureComponent<SeatruckToggleLightsController>() is { } controller)
                {
                    controller.SetLightsActive(true, true);
                }
            }
        }
    }
}
#endif
