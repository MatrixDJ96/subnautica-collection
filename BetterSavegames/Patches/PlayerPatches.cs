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
            if (__instance.gameObject.GetComponent<SavegameController>() == null)
            {
                __instance.gameObject.AddComponent<SavegameController>();
            }
        }
    }
}
