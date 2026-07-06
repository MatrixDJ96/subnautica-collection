#if BELOWZERO_MULTI
using BetterSubnautica.Components;
using HarmonyLib;
using Subnautica.API.Features;
using Subnautica.API.Features.Helper;
using Subnautica.Client.Synchronizations.Processors.Vehicle;

namespace BetterNitrox.Patches
{
    // botbenson applies Hoverbike receives through ZeroGame.SetLightsActive(ToggleLights, ...)
    // (routed to the controllers by ZeroGamePatches), but MapRoomCamera goes straight to
    // lightsParent.SetActive and the SeaTruck hop - ZeroGame.SetLightsActive(SeaTruckLights, ...),
    // a single field write - is small enough that the Mono JIT inlines it into
    // OnProcessCompleted, so a Harmony patch on ZeroGame never sees this call site. Route both
    // cases through the mod controller at the caller, where Harmony reliably intercepts.
    [HarmonyPatch(typeof(LightProcessor))]
    [HarmonyPatch(nameof(LightProcessor.OnProcessCompleted))]
    class LightProcessorOnProcessCompletedPatch
    {
        static bool Prefix(ItemQueueProcess item)
        {
            var techType = item.Action.GetProperty<TechType>("TechType");
            var uniqueId = item.Action.GetProperty<string>("UniqueId");
            var isActive = item.Action.GetProperty<bool>("IsActive");

            if (techType == TechType.MapRoomCamera)
            {
                if (Network.Identifier.GetComponentByGameObject<MapRoomCamera>(uniqueId) is { } mapRoomCamera && mapRoomCamera.gameObject.GetComponent<IToggleLightsController>() is { } controller)
                {
                    Core.Logger.LogInfo($"[LightProcessor.OnProcessCompletedPatch] MapRoomCamera IToggleLightsController called ({isActive})");

                    controller.SetLightsActive(isActive);

                    return false;
                }

                Core.Logger.LogWarning($"[LightProcessor.OnProcessCompletedPatch] MapRoomCamera controller not found ({uniqueId})");
            }
            else if (techType == TechType.SeaTruck)
            {
                if (Network.Identifier.GetComponentByGameObject<SeaTruckLights>(uniqueId) is { } seaTruckLights)
                {
                    var controller = seaTruckLights.gameObject.GetComponentInParent<IToggleLightsController>();

                    // A module's SeaTruckLights carries no controller: bridge through the main cab at the head of the segment chain.
                    if (controller == null && seaTruckLights.gameObject.GetComponent<SeaTruckSegment>() is { } segment && SeaTruckSegment.GetHead(segment) is { } head)
                    {
                        controller = head.gameObject.GetComponentInParent<IToggleLightsController>();
                    }

                    if (controller != null)
                    {
                        Core.Logger.LogInfo($"[LightProcessor.OnProcessCompletedPatch] SeaTruck IToggleLightsController called ({isActive})");

                        controller.SetLightsActive(isActive);

                        return false;
                    }
                }

                Core.Logger.LogWarning($"[LightProcessor.OnProcessCompletedPatch] SeaTruck controller not found ({uniqueId})");
            }

            return true;
        }
    }
}
#endif
