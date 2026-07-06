using Nautilus.Json;
using Nautilus.Options.Attributes;

namespace BetterLights.Settings
{
    [Menu("Better Lights - Vehicles")]
    public class VehiclesSettings : ConfigFile
    {
        public VehiclesSettings() : base("config_vehicles") { }

        [Toggle("Enable Lights On Undocking")]
        public bool EnableLightsOnUndocking { get; set; } = true;
    }
}
