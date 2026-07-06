using BepInEx;
using BetterSubnautica.Plugins;

namespace BetterRemote
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(BetterSubnautica.MyPluginInfo.PLUGIN_GUID)]
    public class Plugin : SubnauticaPlugin
    {
        public static Plugin Core { get; private set; }

        public RemoteServer Server { get; private set; }

        protected override void Awake()
        {
            Core = this;
            base.Awake();

            var port = Config.Bind("Server", "Port", 2600, "Local HTTP port for the remote debug bridge").Value;
            var iface = Config.Bind("Server", "Interface", Interface.Local,
                "Interfaces the bridge listens on. Local = loopback only; All = every interface "
                + "(on HTTP.SYS, needs the game elevated or a 'netsh http add urlacl' "
                + "reservation).").Value;

            Server = new RemoteServer(port, iface);
            Server.Start();

            var bind = iface == Interface.All
                ? $"http://+:{port}/ (all interfaces)"
                : $"http://127.0.0.1:{port}/ (loopback; + localhost, [::1])";

            Logger.LogInfo($"Remote debug bridge listening on {bind}");
        }

        protected override void ApplyPatches()
        {
            base.ApplyPatches();

            VirtualInput.ApplyPatches(Harmony, Logger);
        }

        protected void Update()
        {
            VirtualInput.Update();
            Server?.Pump();
        }

        protected void OnDestroy()
        {
            Server?.Stop();
        }
    }
}
