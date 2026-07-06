#if BELOWZERO
using HarmonyLib;

namespace BetterSubnautica.Patches
{
    [HarmonyPatch(typeof(WeatherManager))]
    [HarmonyPatch(nameof(WeatherManager.DebugPrintAll))]
    class WeatherManagerDebugPrintAllPatch
    {
        static bool Prefix()
        {
            return false;
        }
    }
}
#endif
