#if BELOWZERO
using BetterLights.MonoBehaviours.Lights;
using BetterLights.MonoBehaviours.ToggleLights;
using HarmonyLib;

namespace BetterLights.Patches
{
    [HarmonyPatch(typeof(FlashlightHelmet))]
    [HarmonyPatch(nameof(FlashlightHelmet.Awake))]
    class FlashlightHelmetAwakePatch
    {
        static void Postfix(FlashlightHelmet __instance)
        {
            __instance.gameObject.EnsureComponent<FlashlightHelmetLightsController>();

            __instance.gameObject.EnsureComponent<FlashlightHelmetToggleLightsController>();
        }
    }
}
#endif
