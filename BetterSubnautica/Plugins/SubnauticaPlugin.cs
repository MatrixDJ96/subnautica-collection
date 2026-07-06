using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BetterSubnautica.Utility;
using HarmonyLib;

namespace BetterSubnautica.Plugins
{
    public abstract class SubnauticaPlugin : BaseUnityPlugin
    {
        public string Name { get; private set; }

        public Version Version { get; private set; }

        public string Location { get; private set; }

        public Harmony Harmony { get; private set; }

        public new ManualLogSource Logger { get; private set; }

        protected SubnauticaPlugin()
        {
            var currentAssembly = GetType().Assembly;
            var callingAssembly = Assembly.GetCallingAssembly();

            Name = currentAssembly.GetName().Name;
            Version = currentAssembly.GetName().Version;

            Location = currentAssembly.Location;

            if (string.IsNullOrWhiteSpace(Location))
            {
                Location = callingAssembly.Location;
            }

            Harmony = new Harmony(Name);

            Logger = base.Logger;
        }

        protected virtual void Awake()
        {
            Logger.LogInfo($"Plugin {Name} v{Version} is Awake!");

            ApplyPatches();
        }

        protected virtual void ApplyPatches()
        {
            HarmonyUtility.PrePatchAll(Harmony, GetType().Assembly, Logger);
            HarmonyUtility.PatchAll(Harmony, GetType().Assembly, Logger);
            HarmonyUtility.PostPatchAll(Harmony, GetType().Assembly, Logger);
        }
    }
}
