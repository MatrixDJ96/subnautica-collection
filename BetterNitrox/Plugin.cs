using System;
using BepInEx;
using BetterSubnautica.Plugins;
using BetterSubnautica.Utility;

#if BELOWZERO
using System.IO;
using System.Reflection;
#endif

// TODO: Handle Subnautica Nitrox

namespace BetterNitrox
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(BetterSubnautica.MyPluginInfo.PLUGIN_GUID)]
    public class Plugin : SubnauticaPlugin
    {
        public static Plugin Core { get; private set; }

        public event Action NitroxLoaded = delegate { };

        public bool IsNitroxLoaded { get; internal set; }

        protected override void Awake()
        {
            Core = this;
            base.Awake();
        }

        protected override void ApplyPatches()
        {
            NitroxLoaded += () =>
            {
                HarmonyUtility.PatchAll(Harmony, GetType().Assembly, Logger);
                HarmonyUtility.PostPatchAll(Harmony, GetType().Assembly, Logger);
#if BELOWZERO_MULTI
                Bridges.ToggleLightsBridge.Initialize();
#endif
            };

            LoadNitrox();
        }

        private void LoadNitrox()
        {
#if BELOWZERO
            try
            {
                if (!SubnauticaBootstrap.IsLoaded)
                {
                    Logger.LogInfo("Loading Subnautica Bootstrap...");

                    var loaderPath = SubnauticaBootstrap.GetLoaderPath();
                    var apiFilePath = SubnauticaBootstrap.GetAPIFilePath();

                    if (File.Exists(loaderPath) && File.Exists(apiFilePath))
                    {
                        Logger.LogInfo("Patching Subnautica Bootstrap...");

                        var loaderAssembly = Assembly.Load(File.ReadAllBytes(loaderPath!));
                        var apiAssembly = Assembly.Load(File.ReadAllBytes(apiFilePath!));

                        var loader = loaderAssembly.GetType("Subnautica.Loader.Loader");

                        if (loader != null)
                        {
                            loader.GetMethod("LoadDependencies", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null);

                            try
                            {
                                var apiPaths = apiAssembly.GetType("Subnautica.API.Features.Paths", true);
                                var pluginsPath = apiPaths!.GetMethod("GetGamePluginsPath")!.Invoke(null, [null]);

                                foreach (string assemblyPath in Directory.GetFiles((pluginsPath as string)!, "*.dll"))
                                {
                                    Assembly.Load(File.ReadAllBytes(assemblyPath));
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.LogError($"Unable to get Nitrox plugins path: {e}");
                            }

                            HarmonyUtility.PrePatchAll(Harmony, GetType().Assembly, Logger);

                            loader.GetMethod("Run")!.Invoke(null, null);

                            Logger.LogInfo("Subnautica Bootstrap Patched!");

                            SubnauticaBootstrap.IsLoaded = true;
                            IsNitroxLoaded = true;
                            OnNitroxLoaded();
                        }
                        else
                        {
                            Logger.LogWarning("Unable to patch Subnautica Bootstrap: assembly error!");
                        }
                    }
                    else
                    {
                        Logger.LogWarning("Unable to patch Subnautica Bootstrap: files not found!");
                    }
                }
                else
                {
                    Logger.LogWarning("Unable to patch Subnautica Bootstrap: already loaded!");
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Unable to patch Subnautica Bootstrap: {e}");
            }
#else
            HarmonyUtility.PrePatchAll(Harmony, GetType().Assembly, Logger);
            IsNitroxLoaded = true;
            OnNitroxLoaded();
#endif
        }

        public void OnNitroxLoaded()
        {
            if (IsNitroxLoaded)
            {
                NitroxLoaded?.Invoke();
            }
        }
    }
}
