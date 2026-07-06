using BetterLights.MonoBehaviours.Lights;
using BetterLights.MonoBehaviours.ToggleLights;
using HarmonyLib;

namespace BetterLights.Patches
{
    [HarmonyPatch(typeof(PlayerTool))]
    [HarmonyPatch(nameof(PlayerTool.Awake))]
    class FlashlightAwakePatch
    {
        static void Postfix(PlayerTool __instance)
        {
            if (__instance is FlashLight)
            {
                __instance.gameObject.EnsureComponent<FlashlightLightsController>();

                __instance.gameObject.EnsureComponent<FlashlightToggleLightsController>();
            }
        }
    }
}
