using HarmonyLib;

namespace BetterSavegames.Patches
{
    [HarmonyPatch(typeof(uGUI_MainMenu))]
    [HarmonyPatch(nameof(uGUI_MainMenu.Start))]
    class uGUIMainMenuStartPatch
    {
        public static bool FirstRun { get; set; } = true;
        public static bool ForceAutoload { get; set; } = false;

        static void Prefix(uGUI_MainMenu __instance)
        {
            if ((Core.Settings.AutoloadLatestSavegame && FirstRun) || ForceAutoload)
            {
                FirstRun = false;
                ForceAutoload = false;

                if (__instance.HasSavedGames())
                {
                    __instance.LoadMostRecentSavedGame();
                }
            }
        }
    }
}
