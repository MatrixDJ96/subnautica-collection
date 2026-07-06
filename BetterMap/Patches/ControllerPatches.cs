#if MULTI
using BetterMap.MonoBehaviours;
using HarmonyLib;
using SubnauticaMap;

namespace BetterMap.Patches
{
    [HarmonyPatch(typeof(Controller))]
    [HarmonyPatch(nameof(Controller.Run))]
    class ControllerRunPatch
    {
        static void Postfix(Controller __instance)
        {
            __instance.gameObject.EnsureComponent<SaveController>();

            __instance.gameObject.EnsureComponent<PingController>();
        }
    }

    [HarmonyPatch(typeof(Controller))]
    [HarmonyPatch(nameof(Controller.ReloadMaps))]
    class ControllerReloadMapsPatch
    {
        static void Postfix(Controller __instance)
        {
            if (PingController.Instance != null)
            {
                PingController.Instance.ReloadPings();
            }
        }
    }
}
#endif
