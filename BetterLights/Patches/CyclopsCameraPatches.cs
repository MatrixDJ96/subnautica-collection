#if SUBNAUTICA
using BetterLights.MonoBehaviours.Lights;
using BetterSubnautica.Extensions;
using HarmonyLib;

namespace BetterLights.Patches
{
    [HarmonyPatch(typeof(CyclopsExternalCams))]
    [HarmonyPatch(nameof(CyclopsExternalCams.Start))]
    class CyclopsExternalCamsStartPatch
    {
        static void Postfix(CyclopsExternalCams __instance)
        {
            __instance.gameObject.EnsureComponent<CyclopsCameraLightsController>();
        }
    }

    [HarmonyPatch(typeof(CyclopsExternalCams))]
    [HarmonyPatch(nameof(CyclopsExternalCams.SetLight))]
    class CyclopsExternalCamsSetLightPatch
    {
        static void Postfix(CyclopsExternalCams __instance)
        {
            if (__instance.GetLightState() > 0 && __instance.gameObject.GetComponent<CyclopsCameraLightsController>() is { } lightsController)
            {
                var color = lightsController.Color;

                if (__instance.GetLightState() == 2)
                {
                    color /= 2;
                    color.a = 1;
                }

                __instance.cameraLight.color = color;
            }
        }
    }

    [HarmonyPatch(typeof(CyclopsExternalCams))]
    [HarmonyPatch(nameof(CyclopsExternalCams.ChangeCamera))]
    class CyclopsExternalCamsChangeCameraPatch
    {
        static void Prefix(CyclopsExternalCams __instance, out int __state, int iterate)
        {
            __state = __instance.GetLightState();
        }

        static void Postfix(CyclopsExternalCams __instance, int __state, int iterate)
        {
            __instance.SetLightState(__state);
        }
    }
}
#endif
