using BetterSubnautica.MonoBehaviours.Debug;
using HarmonyLib;

namespace BetterSubnautica.Patches.Debug
{
    [HarmonyPatch(typeof(SubRoot))]
    [HarmonyPatch(nameof(SubRoot.Awake))]
    class SubRootAwakePatch
    {
        static void Postfix(SubRoot __instance)
        {
            if (!__instance.isCyclops)
            {
                __instance.gameObject.EnsureComponent<SubRootDebuggerController>();
            }
        }
    }
}
