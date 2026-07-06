#if BELOWZERO
using HarmonyLib;

namespace BetterVehicles.Patches
{
    [HarmonyPatch(typeof(VehicleDockingBay))]
    [HarmonyPatch(nameof(VehicleDockingBay.Dock))]
    class VehicleDockingBayDockPatch
    {
        static void Postfix(VehicleDockingBay __instance)
        {
            __instance.CancelInvoke("RepairVehicle");
            __instance.InvokeRepeating("RepairVehicle", 0f, 5f);
        }
    }
}
#endif
