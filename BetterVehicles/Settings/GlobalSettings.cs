using BetterVehicles.MonoBehaviours;
using Nautilus.Json;
using Nautilus.Options.Attributes;
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

#if SUBNAUTICA
        [Keybind("Upgrade Modules Button")]
#endif
        public KeyCode UpgradeModules { get; set; } = KeyCode.U;

#if SUBNAUTICA
        [Keybind("Torpedo Storage Button")]
#endif
        public KeyCode TorpedoStorage { get; set; } = KeyCode.T;

#if SUBNAUTICA
        [Keybind("Vehicle Storage Button")]
#endif
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
