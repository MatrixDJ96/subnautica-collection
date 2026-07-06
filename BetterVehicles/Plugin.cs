using BepInEx;
using BetterSubnautica.Plugins;
using BetterVehicles.Settings;
using Nautilus.Handlers;

namespace BetterVehicles
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(Nautilus.PluginInfo.PLUGIN_GUID)]
    [BepInDependency(BetterSubnautica.MyPluginInfo.PLUGIN_GUID)]
    public class Plugin : SubnauticaPlugin
    {
        public GlobalSettings GlobalSettings { get; } = OptionsPanelHandler.RegisterModOptions<GlobalSettings>();
#if SUBNAUTICA
        public CyclopsSettings CyclopsSettings { get; } = OptionsPanelHandler.RegisterModOptions<CyclopsSettings>();
#elif BELOWZERO
        // Keybind-only config: both keys live on the consolidated "Better Subnautica - Input" page,
        // so load it for persistence without registering an empty per-plugin options page.
        public SeatruckSettings SeatruckSettings { get; } = LoadSeatruckSettings();
#endif

        public static Plugin Core { get; private set; }

#if BELOWZERO
        private static SeatruckSettings LoadSeatruckSettings()
        {
            var settings = new SeatruckSettings();
            settings.Load();
            return settings;
        }
#endif

        protected override void Awake()
        {
            Core = this;
            Buttons.Register();
            base.Awake();
        }
    }
}
