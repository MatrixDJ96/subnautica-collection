using BetterSubnautica.MonoBehaviours.Debug;
using HarmonyLib;

namespace BetterSubnautica.Patches.Debug
{
    [HarmonyPatch(typeof(PlayerTool))]
    [HarmonyPatch(nameof(PlayerTool.Awake))]
    class FlashlightAwakePatch
    {
        static void Postfix(PlayerTool __instance)
        {
            if (__instance is FlashLight)
            {
                __instance.gameObject.EnsureComponent<FlashlightDebuggerController>();
            }
        }
    }

#if BELOWZERO
    [HarmonyPatch(typeof(FlashlightHelmet))]
    [HarmonyPatch(nameof(FlashlightHelmet.Awake))]
    class FlashlightHelmetAwakePatch
    {
        static void Postfix(FlashlightHelmet __instance)
        {
            __instance.gameObject.EnsureComponent<FlashlightHelmetDebuggerController>();
        }
    }
#endif
}
