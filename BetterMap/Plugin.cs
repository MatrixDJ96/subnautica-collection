using BepInEx;
using BetterSubnautica.Plugins;

#if MULTI
using NitroxPlugin = BetterNitrox.Plugin;
#endif

// TODO: Handle Subnautica Nitrox

namespace BetterMap
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(BetterSubnautica.MyPluginInfo.PLUGIN_GUID)]
#if MULTI
    [BepInDependency(BetterNitrox.MyPluginInfo.PLUGIN_GUID)]
#endif
    public class Plugin : SubnauticaPlugin
    {
        public static Plugin Core { get; private set; }

        protected override void Awake()
        {
            Core = this;
            base.Awake();
        }

#if BELOWZERO_MULTI
        protected override void ApplyPatches()
        {
            if (!NitroxPlugin.Core.IsNitroxLoaded)
            {
                NitroxPlugin.Core.NitroxLoaded += () =>
                {
                    base.ApplyPatches();
                };
            }
            else
            {
                base.ApplyPatches();
            }
        }
#endif
    }
}
