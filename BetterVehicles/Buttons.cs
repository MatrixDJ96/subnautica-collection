#if SUBNAUTICA
using BetterSubnautica.Utility;

namespace BetterVehicles
{
    public static class Buttons
    {
        public const string Category = "Better Vehicles";

        public static GameInput.Button UpgradeModules { get; private set; }

        public static GameInput.Button TorpedoStorage { get; private set; }

        public static GameInput.Button VehicleStorage { get; private set; }

        internal static void Register()
        {
            UpgradeModules = ModInputUtility.RegisterButton("BetterVehiclesUpgradeModules", "Upgrade Modules Button", "Opens the upgrade modules of the piloted vehicle.", Category, Core.GlobalSettings.UpgradeModules);
            TorpedoStorage = ModInputUtility.RegisterButton("BetterVehiclesTorpedoStorage", "Torpedo Storage Button", "Opens the torpedo storage of the piloted vehicle.", Category, Core.GlobalSettings.TorpedoStorage);
            VehicleStorage = ModInputUtility.RegisterButton("BetterVehiclesVehicleStorage", "Vehicle Storage Button", "Opens the storage of the piloted vehicle.", Category, Core.GlobalSettings.VehicleStorage);
        }
    }
}
#elif BELOWZERO
using BetterSubnautica.Utility;

namespace BetterVehicles
{
    public static class Buttons
    {
        public const string Category = "Better Vehicles";

        internal static void Register()
        {
            var core = Plugin.Core;

            ModInputUtility.RegisterKeybind(Category, "Upgrade Modules Button", "Opens the upgrade modules of the piloted vehicle.", core.GlobalSettings, () => core.GlobalSettings.UpgradeModules, value => core.GlobalSettings.UpgradeModules = value);
            ModInputUtility.RegisterKeybind(Category, "Torpedo Storage Button", "Opens the torpedo storage of the piloted vehicle.", core.GlobalSettings, () => core.GlobalSettings.TorpedoStorage, value => core.GlobalSettings.TorpedoStorage = value);
            ModInputUtility.RegisterKeybind(Category, "Vehicle Storage Button", "Opens the storage of the piloted vehicle.", core.GlobalSettings, () => core.GlobalSettings.VehicleStorage, value => core.GlobalSettings.VehicleStorage = value);
            ModInputUtility.RegisterKeybind(Category, "Direct Enter/Exit Button", "Extra button to press with default enter/exit action.", core.SeatruckSettings, () => core.SeatruckSettings.ForceAction, value => core.SeatruckSettings.ForceAction = value);
            ModInputUtility.RegisterKeybind(Category, "Detach Segments Button", "Detaches the Seatruck segments.", core.SeatruckSettings, () => core.SeatruckSettings.DetachSegments, value => core.SeatruckSettings.DetachSegments = value);
        }
    }
}
#endif
