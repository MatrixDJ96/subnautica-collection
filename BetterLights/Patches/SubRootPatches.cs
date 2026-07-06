#if SUBNAUTICA
using HarmonyLib;

namespace BetterLights.Patches
{
    [HarmonyPatch(typeof(SubRoot))]
    [HarmonyPatch(nameof(SubRoot.UpdateLighting))]
    class SubRootUpdateLightingPatch
    {
        static void Postfix(SubRoot __instance)
        {
            if (__instance.lightControl != null)
            {
                if (__instance.lightingState == 0 && Player.main.currentSub != __instance)
                {
                    __instance.lightControl.LerpToState(1);
                }
            }
        }
    }
}
#endif
