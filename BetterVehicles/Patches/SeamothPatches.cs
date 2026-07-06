#if SUBNAUTICA
using BetterVehicles.MonoBehaviours;
using HarmonyLib;

namespace BetterVehicles.Patches
{
    [HarmonyPatch(typeof(SeaMoth))]
    [HarmonyPatch(nameof(SeaMoth.Start))]
    class SeaMothStartPatch
    {
        static void Postfix(SeaMoth __instance)
        {
            __instance.gameObject.EnsureComponent<SeamothStorageController>();
        }
    }
}
#endif
