using BetterLights.MonoBehaviours.Lights;
using BetterLights.MonoBehaviours.ToggleLights;
using BetterLights.MonoBehaviours.VolumetricLights;
using BetterSubnautica.Components;
using HarmonyLib;

namespace BetterLights.Patches
{
    [HarmonyPatch(typeof(Exosuit))]
    [HarmonyPatch(nameof(Exosuit.Awake))]
    class ExosuitAwakePatch
    {
        static void Postfix(Exosuit __instance)
        {
            __instance.gameObject.EnsureComponent<ExosuitLightsController>();

            __instance.gameObject.EnsureComponent<ExosuitToggleLightsController>();

            __instance.gameObject.EnsureComponent<ExosuitVolumetricLightsController>();
        }
    }

#if BELOWZERO
    [HarmonyPatch(typeof(Exosuit))]
    [HarmonyPatch(nameof(Exosuit.SubConstructionComplete))]
#else
    [HarmonyPatch(typeof(Vehicle))]
    [HarmonyPatch(nameof(Vehicle.SubConstructionComplete))]
#endif

    class ExosuitSubConstructionCompletePatch
    {
        static void Postfix(Vehicle __instance)
        {
            if (__instance is Exosuit && __instance.gameObject.GetComponent<IToggleLightsController>() is { } controller)
            {
                controller.SetLightsActive(true, true);
            }
        }
    }

    [HarmonyPatch(typeof(Exosuit))]
    [HarmonyPatch(nameof(Exosuit.OnPilotModeBegin))]
    class ExosuitOnPilotModeBeginPatch
    {
        static void Postfix(Exosuit __instance)
        {
            if (__instance.gameObject.GetComponent<IVolumetricLightsController>() is { } controller)
            {
                controller.DisableVolumes();
            }
        }
    }

    [HarmonyPatch(typeof(Exosuit))]
    [HarmonyPatch(nameof(Exosuit.OnPilotModeEnd))]
    class ExosuitOnPilotModeEndPatch
    {
        static void Postfix(Exosuit __instance)
        {
            if (__instance.gameObject.GetComponent<IVolumetricLightsController>() is { } controller)
            {
                controller.RestoreVolumes();
            }
        }
    }
}
