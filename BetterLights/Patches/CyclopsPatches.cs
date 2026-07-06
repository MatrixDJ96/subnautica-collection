#if SUBNAUTICA
using BetterLights.MonoBehaviours.Lights;
using BetterLights.MonoBehaviours.ToggleLights;
using BetterLights.MonoBehaviours.VolumetricLights;
using BetterSubnautica.Components;
using HarmonyLib;

namespace BetterLights.Patches
{
    [HarmonyPatch(typeof(SubRoot))]
    [HarmonyPatch(nameof(SubRoot.Awake))]
    class CyclopsAwakePatch
    {
        static void Postfix(SubRoot __instance)
        {
            if (__instance.isCyclops)
            {
                __instance.gameObject.EnsureComponent<CyclopsLightsController>();

                __instance.gameObject.EnsureComponent<CyclopsToggleLightsController>();

                __instance.gameObject.EnsureComponent<CyclopsVolumetricLightsController>();
            }
        }
    }

    [HarmonyPatch(typeof(CyclopsLightingPanel))]
    [HarmonyPatch(nameof(CyclopsLightingPanel.SubConstructionComplete))]
    class CyclopsLightingPanelSubConstructionCompletePatch
    {
        static void Postfix(CyclopsLightingPanel __instance)
        {
            if (__instance.gameObject.GetComponentInParent<IToggleLightsController>() is { } controller)
            {
                controller.SetLightsActive(true, true);
            }
        }
    }

    [HarmonyPatch(typeof(CyclopsLightingPanel))]
    [HarmonyPatch(nameof(CyclopsLightingPanel.ToggleFloodlights))]
    class CyclopsLightingPanelToggleFloodlightsPatch
    {
        static void Postfix(CyclopsLightingPanel __instance)
        {
            // Disable volumetric lights if external lights enabled and player inside cyclops
            if (__instance.floodlightsOn && __instance.cyclopsRoot != null && __instance.cyclopsRoot.isCyclops && __instance.cyclopsRoot == Player.main.currentSub && __instance.gameObject.GetComponentInParent<IVolumetricLightsController>() is { } controller)
            {
                controller.DisableVolumes();
            }
        }
    }
}
#endif
