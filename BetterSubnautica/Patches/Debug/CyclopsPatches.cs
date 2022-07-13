#if SUBNAUTICA
using BetterSubnautica.MonoBehaviours.Debug;
using HarmonyLib;

namespace BetterSubnautica.Patches.Debug
{
    [HarmonyPatch(typeof(SubRoot))]
    [HarmonyPatch(nameof(SubRoot.Awake))]
    class CyclopsAwakePatch
    {
        static void Postfix(SubRoot __instance)
        {
            if (__instance.isCyclops)
            {
                if (__instance.gameObject.GetComponent<CyclopsDebuggerController>() == null)
                {
                    __instance.gameObject.AddComponent<CyclopsDebuggerController>();
                }
            }
        }
    }
}
#endif
