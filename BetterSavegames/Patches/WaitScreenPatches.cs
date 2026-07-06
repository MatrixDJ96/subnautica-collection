using BetterSavegames.MonoBehaviours;
using HarmonyLib;

namespace BetterSavegames.Patches
{
    [HarmonyPatch(typeof(WaitScreen))]
    [HarmonyPatch(nameof(WaitScreen.Awake))]
    class WaitScreenAwakePatch
    {
        static void Postfix(WaitScreen __instance)
        {
            __instance.gameObject.EnsureComponent<WaitScreenController>();
        }
    }

    [HarmonyPatch(typeof(WaitScreen))]
    [HarmonyPatch(nameof(WaitScreen.Update))]
    class WaitScreenUpdatePatch
    {
        static void Postfix(WaitScreen __instance)
        {
            if (__instance.gameObject.GetComponent<WaitScreenController>() is WaitScreenController controller)
            {
                controller.OnWaitingChanged(__instance.isWaiting);
            }
        }
    }
}
