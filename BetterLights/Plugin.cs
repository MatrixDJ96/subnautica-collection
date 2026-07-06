using BepInEx;
using BetterLights.Settings;
using BetterSubnautica.Plugins;
using Nautilus.Handlers;

namespace BetterLights
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(Nautilus.PluginInfo.PLUGIN_GUID)]
    [BepInDependency(BetterSubnautica.MyPluginInfo.PLUGIN_GUID)]
#if MULTI
    [BepInDependency(BetterNitrox.MyPluginInfo.PLUGIN_GUID)]
#endif
    public class Plugin : SubnauticaPlugin
    {
        public FlashlightSettings FlashlightSettings { get; } = OptionsPanelHandler.RegisterModOptions<FlashlightSettings>();
#if BELOWZERO
        public FlashlightHelmetSettings FlashlightHelmetSettings { get; } = OptionsPanelHandler.RegisterModOptions<FlashlightHelmetSettings>();
#endif
        public SeaglideSettings SeaglideSettings { get; } = OptionsPanelHandler.RegisterModOptions<SeaglideSettings>();
#if SUBNAUTICA
        public SeamothSettings SeamothSettings { get; } = OptionsPanelHandler.RegisterModOptions<SeamothSettings>();
#elif BELOWZERO
        public SeatruckSettings SeatruckSettings { get; } = OptionsPanelHandler.RegisterModOptions<SeatruckSettings>();
#endif
#if BELOWZERO
        public HoverbikeSettings HoverbikeSettings { get; } = OptionsPanelHandler.RegisterModOptions<HoverbikeSettings>();
#endif
        public ExosuitSettings ExosuitSettings { get; } = OptionsPanelHandler.RegisterModOptions<ExosuitSettings>();
#if SUBNAUTICA
        public CyclopsSettings CyclopsSettings { get; } = OptionsPanelHandler.RegisterModOptions<CyclopsSettings>();
#endif
        public MapRoomCameraSettings MapRoomCameraSettings { get; } = OptionsPanelHandler.RegisterModOptions<MapRoomCameraSettings>();
        public VehiclesSettings VehiclesSettings { get; } = OptionsPanelHandler.RegisterModOptions<VehiclesSettings>();

        public static Plugin Core { get; private set; }

        protected override void Awake()
        {
            Core = this;
            Buttons.Register();
            base.Awake();
        }
    }
}
