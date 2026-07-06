#if BELOWZERO
using Nautilus.Json;
using Nautilus.Options.Attributes;
using UnityEngine;

namespace BetterVehicles.Settings
{
    [Menu("Better Vehicles - Seatruck")]
    public class SeatruckSettings : ConfigFile
    {
        public SeatruckSettings() : base("config_seatruck") { }

        public KeyCode ForceAction { get; set; } = KeyCode.LeftControl;

        public KeyCode DetachSegments { get; set; } = KeyCode.V;
    }
}
#endif
