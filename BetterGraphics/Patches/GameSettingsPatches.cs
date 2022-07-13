using BetterSubnautica.Utility;
using HarmonyLib;
using UnityEngine;

namespace BetterGraphics.Patches
{
    [HarmonyPatch(typeof(GameSettings))]
    [HarmonyPatch(nameof(GameSettings.SerializeSettings))]
    class GameSettingsSerializeSettingsPatch
    {
        static void Postfix(GameSettings __instance, GameSettings.ISerializer serializer)
        {
            var resolutionWidth = Screen.width;
            var resolutionHeight = Screen.height;
            var fullScreenMode = Screen.fullScreenMode;

            if (resolutionWidth != Core.Settings.ResolutionWidth || resolutionHeight != Core.Settings.ResolutionHeight || fullScreenMode != Core.Settings.GetFixedFullScreenMode())
            {
                Screen.SetResolution(Core.Settings.ResolutionWidth, Core.Settings.ResolutionHeight, Core.Settings.GetFixedFullScreenMode());
            }

            QualitySettings.vSyncCount = Core.Settings.GetFixedVSyncCount();
            QualitySettings.maxQueuedFrames = Core.Settings.MaximumFrameCount;
            Application.targetFrameRate = Core.Settings.GetFixedFramerateCount();
            QualitySettings.anisotropicFiltering = Core.Settings.AnisotropicFiltering;
            QualitySettings.shadows = Core.Settings.ShadowQuality;
            QualitySettings.shadowDistance = Core.Settings.ShadowDistance;
            QualitySettings.shadowResolution = Core.Settings.ShadowResolution;

            GraphicsUtility.OnQualityLevelChanged();

            if (resolutionWidth != Screen.width || resolutionHeight != Screen.height)
            {
                DebuggerUtility.ShowWarning("Resolution: " + resolutionWidth + "x" + resolutionHeight + " -> " + Screen.width + "x" + Screen.height);
            }

            if (fullScreenMode != Screen.fullScreenMode)
            {
                DebuggerUtility.ShowWarning("FullScreen Mode: " + fullScreenMode + " -> " + Screen.fullScreenMode);
            }
        }
    }
}
