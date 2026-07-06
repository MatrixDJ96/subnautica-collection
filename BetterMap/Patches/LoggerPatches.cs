using HarmonyLib;
using SubnauticaMap;

namespace BetterMap.Patches
{
    [HarmonyPatch(typeof(Logger))]
    [HarmonyPatch(nameof(Logger.Print))]
    class LoggerPrintPatch
    {
        static bool Prefix(string text)
        {
            Logger.Write(text);

            return false;
        }
    }
}
