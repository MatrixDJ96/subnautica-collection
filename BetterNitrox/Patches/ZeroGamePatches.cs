#if BELOWZERO_MULTI
using BetterSubnautica.Components;
using HarmonyLib;
using Subnautica.API.Features;

namespace BetterNitrox.Patches
{
    [HarmonyPatch(typeof(ZeroGame))]
    [HarmonyPatch(nameof(ZeroGame.SetLightsActive), typeof(ToggleLights), typeof(bool), typeof(bool))]
    class ZeroGameSetLightsActivePatch
    {
        static bool Prefix(ToggleLights toggleLights, bool isActive, bool infinityEnergy)
        {
            if (toggleLights.gameObject.GetComponentInParent<IToggleLightsController>() is { } controller)
            {
                Core.Logger.LogInfo($"[ZeroGame.SetLightsActivePatch] IToggleLightsController called ({isActive})");

                controller.SetLightsActive(isActive);

                return false;
            }

            Core.Logger.LogWarning($"[ZeroGame.SetLightsActivePatch] active: {isActive}, infinityEnergy: {infinityEnergy}");

            return true;
        }
    }

    [HarmonyPatch(typeof(ZeroGame))]
    [HarmonyPatch(nameof(ZeroGame.SetLightsActive), typeof(SeaTruckLights), typeof(bool))]
    class ZeroGameSetSeaTruckLightsActivePatch
    {
        static bool Prefix(SeaTruckLights seaTruckLights, bool isActive)
        {
            var controller = seaTruckLights.gameObject.GetComponentInParent<IToggleLightsController>();

            // A module's SeaTruckLights carries no controller: bridge through the main cab at the head of the segment chain.
            if (controller == null && seaTruckLights.gameObject.GetComponent<SeaTruckSegment>() is { } segment && SeaTruckSegment.GetHead(segment) is { } head)
            {
                controller = head.gameObject.GetComponentInParent<IToggleLightsController>();
            }

            if (controller != null)
            {
                Core.Logger.LogInfo($"[ZeroGame.SetSeaTruckLightsActivePatch] IToggleLightsController called ({isActive})");

                controller.SetLightsActive(isActive);

                return false;
            }

            Core.Logger.LogWarning($"[ZeroGame.SetSeaTruckLightsActivePatch] active: {isActive}");

            return true;
        }
    }
}
#endif
