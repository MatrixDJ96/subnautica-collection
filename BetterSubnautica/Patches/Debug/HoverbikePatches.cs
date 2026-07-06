#if BELOWZERO
using BetterSubnautica.MonoBehaviours.Debug;
using HarmonyLib;

namespace BetterSubnautica.Patches.Debug
{
    [HarmonyPatch(typeof(Hoverbike))]
    [HarmonyPatch(nameof(Hoverbike.Awake))]
    class HoverbikeAwakePatch
    {
        static void Postfix(Hoverbike __instance)
        {
            __instance.gameObject.EnsureComponent<HoverbikeDebuggerController>();
        }
    }
}
#endif
