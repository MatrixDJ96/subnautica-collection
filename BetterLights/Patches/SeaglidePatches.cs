using BetterLights.MonoBehaviours.Lights;
using BetterLights.MonoBehaviours.ToggleLights;
using HarmonyLib;

namespace BetterLights.Patches
{
    [HarmonyPatch(typeof(PlayerTool))]
    [HarmonyPatch(nameof(PlayerTool.Awake))]
    class SeaglideAwakePatch
    {
        static void Postfix(PlayerTool __instance)
        {
            if (__instance is Seaglide seaglide)
            {
                __instance.gameObject.EnsureComponent<SeaglideLightsController>();

                __instance.gameObject.EnsureComponent<SeaglideToggleLightsController>();
            }
        }
    }
}
