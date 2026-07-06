#if BELOWZERO_MULTI
using BetterMap.MonoBehaviours;
using HarmonyLib;
using Subnautica.Server.Core;

namespace BetterMap.Patches
{
    [HarmonyPatch(typeof(Server))]
    [HarmonyPatch(nameof(Server.Dispose))]
    class ServerDisposePatch
    {
        static void Prefix(Server __instance, bool isEndGame)
        {
            if (SaveController.Instance != null)
            {
                Core.Logger.LogInfo("Server is disposing, performing save...");

                SaveController.Instance.PerformSave();
            }
        }
    }
}
#endif
