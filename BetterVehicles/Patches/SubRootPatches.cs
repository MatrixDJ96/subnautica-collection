using BetterVehicles.MonoBehaviours;
using HarmonyLib;

namespace BetterVehicles.Patches
{
    [HarmonyPatch(typeof(SubRoot))]
    [HarmonyPatch(nameof(SubRoot.Awake))]
    class SubRootAwakePatch
    {
        static void Postfix(SubRoot __instance)
        {
            SubRootContainer.Instance.Dict[__instance.GetInstanceID()] = __instance;
        }
    }

    [HarmonyPatch(typeof(SubRoot))]
    [HarmonyPatch(nameof(SubRoot.Start))]
    class SubRootStartPatch
    {
        static void Postfix(SubRoot __instance)
        {
            if (!__instance.isCyclops)
            {
                __instance.vehicleRepairUpgrade = Core.GlobalSettings.AutomaticVehicleRepair;
            }
        }
    }

    [HarmonyPatch(typeof(SubRoot))]
    [HarmonyPatch(nameof(SubRoot.SetCyclopsUpgrades))]
    class SubRootSetCyclopsUpgradesPatch
    {
        static void Postfix(SubRoot __instance)
        {
            if (!__instance.isCyclops)
            {
                __instance.vehicleRepairUpgrade = Core.GlobalSettings.AutomaticVehicleRepair;
            }
        }
    }
}
