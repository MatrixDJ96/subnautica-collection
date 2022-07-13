#if SUBNAUTICA_STABLE
using BetterSubnautica.Utility;
using HarmonyLib;
using UWE;

namespace BetterPDA.Patches
{
    [HarmonyPatch(typeof(IngameMenu))]
    [HarmonyPatch(nameof(IngameMenu.QuitGame))]
    class IngameMenuQuitGamePatch
    {
        static void Prefix(IngameMenu __instance)
        {
            // Force resume game
            FreezeTime.End(FreezeTimeUtility.PDAId);
        }
    }
}
#endif
