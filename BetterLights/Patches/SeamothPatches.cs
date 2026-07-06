#if SUBNAUTICA
using BetterLights.MonoBehaviours.Lights;
using BetterLights.MonoBehaviours.ToggleLights;
using BetterLights.MonoBehaviours.VolumetricLights;
using BetterSubnautica.Components;
using HarmonyLib;

namespace BetterLights.Patches
{
    [HarmonyPatch(typeof(SeaMoth))]
    [HarmonyPatch(nameof(SeaMoth.Start))]
    class SeamothStartPatch
    {
        static void Postfix(SeaMoth __instance)
        {
            __instance.gameObject.EnsureComponent<SeamothLightsController>();

            __instance.gameObject.EnsureComponent<SeamothToggleLightsController>();

            __instance.gameObject.EnsureComponent<SeamothVolumetricLightsController>();
        }
    }

    [HarmonyPatch(typeof(SeaMoth))]
    [HarmonyPatch(nameof(SeaMoth.SubConstructionComplete))]
    class SeamothSubConstructionCompletePatch
    {
        static void Postfix(SeaMoth __instance)
        {
            // During the construction animation the light children are inactive, so the
            // controller added by the Start patch destroys itself: re-ensure it now that
            // the vehicle is complete.
            if (__instance.gameObject.EnsureComponent<SeamothToggleLightsController>() is { } toggleLightsController)
            {
                toggleLightsController.SetLightsActive(true, true);
            }
        }
    }
}
#endif
