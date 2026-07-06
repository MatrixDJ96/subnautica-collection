using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterVehicles.MonoBehaviours
{
    public abstract class AbstractVehicleStorageController : MonoBehaviour
    {
        protected Vehicle component;

        protected virtual void Awake()
        {
            component = gameObject.GetComponent<Vehicle>();
        }

        protected virtual void Update()
        {
            if (AvatarInputHandler.main.IsEnabled() && Player.main.GetVehicle() is Vehicle vehicle && vehicle == component)
            {
#if SUBNAUTICA
                var upgrade = GameInput.GetButtonDown(Buttons.UpgradeModules);
                var torpedo = GameInput.GetButtonDown(Buttons.TorpedoStorage);
                var storage = GameInput.GetButtonDown(Buttons.VehicleStorage);
#else
                var upgrade = Input.GetKeyDown(Core.GlobalSettings.UpgradeModules);
                var torpedo = Input.GetKeyDown(Core.GlobalSettings.TorpedoStorage);
                var storage = Input.GetKeyDown(Core.GlobalSettings.VehicleStorage);
#endif

                if (upgrade || torpedo || storage)
                {
                    Inventory.main.ClearUsedStorage();

                    IItemsContainer[] storageContainers = Array.Empty<IItemsContainer>();

                    if (upgrade)
                    {
                        storageContainers = GetUpgradeModules();
                    }
                    else if (torpedo)
                    {
                        storageContainers = GetTorpedoStorage();
                    }
                    else if (storage)
                    {
                        storageContainers = GetVehicleStorage();
                    }

                    foreach (var storageContainer in storageContainers)
                    {
                        if (storageContainer != null)
                        {
                            Inventory.main.SetUsedStorage(storageContainer, true);
                        }
                    }
                }

                if (Inventory.main.GetUsedStorageCount() != 0)
                {
                    Player.main.GetPDA().Open(PDATab.Inventory);
                }
            }
        }

        protected virtual IItemsContainer[] GetStorageContainers(TechType techType)
        {
            List<IItemsContainer> storages = new();

            for (int i = 0; i < component.GetSlotCount(); i++)
            {
                if (component.GetStorageInSlot(i, techType) is IItemsContainer container)
                {
                    storages.Add(container);
                }
            }

            return storages.ToArray();
        }

        public abstract IItemsContainer[] GetDefaultStorage();

        public virtual IItemsContainer[] GetUpgradeModules()
        {
            return new[] { component.upgradesInput.equipment };
        }

        public abstract IItemsContainer[] GetTorpedoStorage();

        public virtual IItemsContainer[] GetVehicleStorage()
        {
            return GetStorageContainers(TechType.VehicleStorageModule);
        }
    }
}
