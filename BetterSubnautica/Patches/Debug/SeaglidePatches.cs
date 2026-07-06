using BetterSubnautica.MonoBehaviours.Debug;
using HarmonyLib;

namespace BetterSubnautica.Patches.Debug
{
    [HarmonyPatch(typeof(PlayerTool))]
    [HarmonyPatch(nameof(PlayerTool.Awake))]
    class SeaglideAwakePatch
    {
        static void Postfix(PlayerTool __instance)
        {
            if (__instance is Seaglide)
            {
                __instance.gameObject.EnsureComponent<SeaglideDebuggerController>();
            }
        }
    }
}
