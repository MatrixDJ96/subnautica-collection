using HarmonyLib;
using UnityEngine;

namespace BetterGraphics.Patches
{
    [HarmonyPatch(typeof(MainMenuController))]
    [HarmonyPatch(nameof(MainMenuController.Start))]
    class MainMenuControllerStartPatch
    {
        static void Postfix()
        {
            Application.targetFrameRate = Core.Settings.GetFixedFramerateCount();
        }
    }

    [HarmonyPatch(typeof(MainMenuVsync))]
    [HarmonyPatch(nameof(MainMenuVsync.ToggleVsync))]
    class MainMenuVsyncToggleVsyncPatch
    {
        static bool Prefix()
        {
            return false;
        }
    }
}
