#if BELOWZERO
using BetterLights.MonoBehaviours;
using BetterLights.MonoBehaviours.Lights;
using BetterLights.MonoBehaviours.ToggleLights;
using BetterLights.MonoBehaviours.VolumetricLights;
using BetterSubnautica.Extensions;
using HarmonyLib;

namespace BetterLights.Patches
{
    [HarmonyPatch(typeof(SeaTruckSegment))]
    [HarmonyPatch(nameof(SeaTruckSegment.Start))]
    class SeaTruckSegmentStartPatch
    {
        static void Postfix(SeaTruckSegment __instance)
        {
            if (__instance.IsMainSegment())
            {
                __instance.gameObject.EnsureComponent<SeatruckLightsController>();

                __instance.gameObject.EnsureComponent<SeatruckToggleLightsController>();

                __instance.gameObject.EnsureComponent<SeatruckVolumetricLightsController>();
            }
        }
    }

    [HarmonyPatch(typeof(SeaTruckLights))]
    [HarmonyPatch(nameof(SeaTruckLights.Update))]
    class SeaTruckLightsUpdatePatch
    {
        static bool Prefix(SeaTruckLights __instance)
        {
            SeatruckLightsContainer.Instance.Dict.TryGetValue(__instance.GetInstanceID(), out var controller);

            if (controller != null)
            {
                if (__instance.lightingController != null)
                {
                    __instance.lightingController.LerpToState(controller.IsPowered() ? 0 : 2);
                }
            }

            return controller == null;
        }
    }
}
#endif
