using BetterVehicles.MonoBehaviours;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options.Attributes;
using System;
using UnityEngine;

namespace BetterVehicles.Settings
{
    [Menu("Better Vehicles - Global")]
    public class GlobalSettings : ConfigFile
    {
        public GlobalSettings() : base("config_global") { }

        [Toggle("Automatic Vehicle Repair", Tooltip = "Enable automatic vehicle repair on docking bay"), OnChange(nameof(AutomaticVehicleRepairEvent))]
        public bool AutomaticVehicleRepair { get; set; } = true;

        [Toggle("Linked Storage", Tooltip = "Link personal inventory with vehicle storage")]
        public bool LinkedStorage { get; set; } = false;

        [Keybind("Upgrade Modules Button")]
        public KeyCode UpgradeModules { get; set; } = KeyCode.U;

        [Keybind("Torpedo Storage Button")]
        public KeyCode TorpedoStorage { get; set; } = KeyCode.T;

        [Keybind("Vehicle Storage Button")]
        public KeyCode VehicleStorage { get; set; } = KeyCode.V;

        private void AutomaticVehicleRepairEvent()
        {
            foreach (var item in SubRootContainer.Instance.Dict)
            {
                if (item.Value != null)
                {
                    item.Value.SetCyclopsUpgrades();
                }
            }
        }
    }
}
