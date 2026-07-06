#if BELOWZERO_MULTI
using BetterSubnautica.Attributes;
using HarmonyLib;
using Subnautica.API.Features;

namespace BetterNitrox.Patches
{
    [PrePatch]
    [HarmonyPatch(typeof(Tools))]
    [HarmonyPatch(nameof(Tools.IsBepinexInstalled))]
    class ToolsIsBepinexInstalledPatch
    {
        static bool Prefix(ref bool __result)
        {
            Core.Logger.LogInfo("Tools.IsBepinexInstalled Patch called");

            __result = false;

            return false;
        }
    }
}
#endif
