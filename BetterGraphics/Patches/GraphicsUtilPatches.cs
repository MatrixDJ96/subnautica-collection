using HarmonyLib;

namespace BetterGraphics.Patches
{
    [HarmonyPatch(typeof(GraphicsUtil))]
    [HarmonyPatch(nameof(GraphicsUtil.SetVSyncEnabled))]
    class GraphicsUtilSetVSyncEnabledPatch
    {
        static bool Prefix()
        {
            return false;
        }
    }
}
