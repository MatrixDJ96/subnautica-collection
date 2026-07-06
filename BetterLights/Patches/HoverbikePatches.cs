#if BELOWZERO
using BetterLights.MonoBehaviours.Lights;
using BetterLights.MonoBehaviours.ToggleLights;
using BetterLights.MonoBehaviours.VolumetricLights;
using HarmonyLib;

namespace BetterLights.Patches
{
    [HarmonyPatch(typeof(Hoverbike))]
    [HarmonyPatch(nameof(Hoverbike.Awake))]
    class HoverbikeAwakePatch
    {
        static void Postfix(Hoverbike __instance)
        {
            __instance.gameObject.EnsureComponent<HoverbikeLightsController>();

            __instance.gameObject.EnsureComponent<HoverbikeToggleLightsController>();

            __instance.gameObject.EnsureComponent<HoverbikeVolumetricLightsController>();
        }
    }


    [HarmonyPatch(typeof(Hoverbike))]
    [HarmonyPatch(nameof(Hoverbike.EnterVehicle))]
    class HoverbikeEnterVehiclePatch
    {
        static void Postfix(Hoverbike __instance)
        {
            if (__instance.gameObject.GetComponent<IVolumetricLightsController>() is { } volumetricLightsController)
            {
                volumetricLightsController.DisableVolumes();
            }
        }
    }

    [HarmonyPatch(typeof(Hoverbike))]
    [HarmonyPatch(nameof(Hoverbike.ExitVehicle))]
    class HoverbikeExitVehiclePatch
    {
        static void Postfix(Hoverbike __instance)
        {
            if (__instance.gameObject.GetComponent<IVolumetricLightsController>() is { } volumetricLightsController)
            {
                volumetricLightsController.RestoreVolumes();
            }
        }
    }
}
#endif
