using BetterVehicles.MonoBehaviours;
using HarmonyLib;

namespace BetterVehicles.Patches
{
    [HarmonyPatch(typeof(Exosuit))]
    [HarmonyPatch(nameof(Exosuit.Start))]
    class ExosuitStartPatch
    {
        static void Postfix(Exosuit __instance)
        {
            __instance.gameObject.EnsureComponent<ExosuitStorageController>();
        }
    }
}
