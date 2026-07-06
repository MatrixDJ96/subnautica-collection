#if SUBNAUTICA
using BetterSubnautica.MonoBehaviours.Debug;
using HarmonyLib;

namespace BetterSubnautica.Patches.Debug
{
    [HarmonyPatch(typeof(SeaMoth))]
    [HarmonyPatch(nameof(SeaMoth.Start))]
    class SeamothStartPatch
    {
        static void Postfix(SeaMoth __instance)
        {
            __instance.gameObject.EnsureComponent<SeamothDebuggerController>();
        }
    }
}
#endif
