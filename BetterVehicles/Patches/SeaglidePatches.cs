using HarmonyLib;

namespace BetterVehicles.Patches
{
#if BELOWZERO
    [HarmonyPatch(typeof(Seaglide))]
    [HarmonyPatch(nameof(Seaglide.Start))]
    class SeaglideStartPatch
    {
        static void Postfix(Seaglide __instance)
        {
            if (__instance.gameObject.GetComponent<VehicleInterface_MapController>() is VehicleInterface_MapController mapController)
            {
                mapController.mapActive = false;
            }
        }
    }
#endif
}
