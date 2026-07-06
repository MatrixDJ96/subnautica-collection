using BetterLights.MonoBehaviours.ToggleLights;
using HarmonyLib;

namespace BetterLights.Patches
{
    // Ownership comes from ToggleLightsRegistry (exact instance match): a hierarchy lookup
    // from the vanilla child object misses the controller on some spawn paths.
    [HarmonyPatch(typeof(ToggleLights))]
    [HarmonyPatch(nameof(ToggleLights.SetLightsActive))]
    class ToggleLightsSetLightsActivePatch
    {
        static bool Prefix(ToggleLights __instance, bool isActive)
        {
            ToggleLightsRegistry.TryGetOwner(__instance, out var owner);
            var block = owner != null;
#if DEBUG_LOGS
            BetterLights.Plugin.Core.Logger.LogInfo($"[Lights] VANILLA ToggleLights.SetLightsActive(isActive={isActive}) owner={owner?.GetType().Name ?? "none"} block={block}");
#endif
            return !block;
        }
    }

    // The vanilla per-frame toggle check (RightHand button) stays alive even with
    // SetLightsActive blocked (timers, lightState); skip it entirely when a controller owns
    // the lights so the vanilla key leaves no residue.
    [HarmonyPatch(typeof(ToggleLights))]
    [HarmonyPatch(nameof(ToggleLights.CheckLightToggle))]
    class ToggleLightsCheckLightTogglePatch
    {
        static bool Prefix(ToggleLights __instance)
        {
            return !ToggleLightsRegistry.IsOwned(__instance);
        }
    }
}
