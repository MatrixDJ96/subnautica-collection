using BetterSavegames.MonoBehaviours;
using HarmonyLib;

namespace BetterSavegames.Patches
{
    [HarmonyPatch(typeof(Player))]
    [HarmonyPatch(nameof(Player.Awake))]
    class PlayerAwakePatch
    {
        static void Postfix(Player __instance)
        {
            __instance.gameObject.EnsureComponent<SavegameController>();
        }
    }
}
