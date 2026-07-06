using BetterHUD.MonoBehaviours;
using HarmonyLib;

namespace BetterHUD.Patches
{
    [HarmonyPatch(typeof(Player))]
    [HarmonyPatch(nameof(Player.Awake))]
    class PlayerAwakePatch
    {
        static void Postfix(Player __instance)
        {
            __instance.gameObject.EnsureComponent<TimeDisplayController>();
        }
    }
}
