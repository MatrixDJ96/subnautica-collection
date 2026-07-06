using BepInEx;
using BetterSubnautica.Plugins;
using Nautilus.Handlers;

namespace BetterGraphics
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(Nautilus.PluginInfo.PLUGIN_GUID)]
    [BepInDependency(BetterSubnautica.MyPluginInfo.PLUGIN_GUID)]
    public class Plugin : SubnauticaPlugin
    {
        public Settings Settings { get; } = OptionsPanelHandler.RegisterModOptions<Settings>();

        public static Plugin Core { get; private set; }

        protected override void Awake()
        {
            Core = this;
            base.Awake();
        }
    }
}
