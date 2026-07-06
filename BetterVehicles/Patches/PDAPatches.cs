using BetterVehicles.MonoBehaviours;
using HarmonyLib;
using System;

namespace BetterVehicles.Patches
{
    [HarmonyPatch(typeof(PDA))]
    [HarmonyPatch(nameof(PDA.Open))]
    class PDAOpenPatch
    {
        static void Prefix(PDA __instance)
        {
            if (Core.GlobalSettings.LinkedStorage && Player.main.GetVehicle() is Vehicle vehicle && Inventory.main.GetUsedStorageCount() == 0)
            {
                IItemsContainer[] storageContainers = Array.Empty<IItemsContainer>();

                if (vehicle.gameObject.GetComponent<AbstractVehicleStorageController>()?.GetVehicleStorage() is IItemsContainer[] vehicleStorage)
                {
                    storageContainers = vehicleStorage;
                }

                foreach (var storageContainer in storageContainers)
                {
                    if (storageContainer != null)
                    {
                        Inventory.main.SetUsedStorage(storageContainer, true);
                    }
                }
            }
        }
    }
}
