using BetterSubnautica.Utility;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options.Attributes;
using System;
using UnityEngine;
using FullScreenModeEnum = UnityEngine.FullScreenMode;

namespace BetterGraphics
{
    [Menu("Better Graphics")]
    public class Settings : ConfigFile
    {
        private bool Started { get; set; } = false;

        public Settings() : base()
        {
            OnFinishedLoading += (sender, e) =>
            {
                FullScreenMode = GetRealFullScreenMode();
                Started = true;
            };

            OnStartedSaving += (sender, e) => FullScreenMode = Started ? GetFixedFullScreenMode() : FullScreenMode;

            OnFinishedSaving += (sender, e) => FullScreenMode = Started ? GetRealFullScreenMode() : FullScreenMode;
        }

        [IgnoreMember]
        public int ResolutionWidth { get; set; } = Screen.width;
        [IgnoreMember]
        public int ResolutionHeight { get; set; } = Screen.height;

        [Choice("FullScreen Mode", new[] { "Exclusive FullScreen", "FullScreen Window", "Windowed" }), OnChange(nameof(FullScreenModeEvent))]
        public FullScreenModeEnum FullScreenMode { get; set; } = Screen.fullScreenMode;

        [Toggle("Enable VSync"), OnChange(nameof(VSyncFramerateEvent))]
        public bool EnableVSync { get; set; } = true;

        [Slider("Maximum Frames Count", 1, 4, Tooltip = "Maximum number of frames queued up by graphics driver", DefaultValue = 2), OnChange(nameof(VSyncFramerateEvent))]
        public int MaximumFrameCount { get; set; } = QualitySettings.maxQueuedFrames;

        [Toggle("Enable Framerate Limit"), OnChange(nameof(VSyncFramerateEvent))]
        public bool EnableFramerateLimit { get; set; } = false;

        [Slider("Framerate Limit", 1, 500, DefaultValue = 60), OnChange(nameof(VSyncFramerateEvent))]
        public int FramerateLimit { get; set; } = 60;

        [Choice("Anisotropic Filtering", new[] { "Disabled", "Enabled", "Force Enabled" }), OnChange(nameof(AnisotropicFilteringEvent))]
        public AnisotropicFiltering AnisotropicFiltering { get; set; } = AnisotropicFiltering.ForceEnable;

        [Choice("Shadow Quality", new[] { "Disable", "Hard Only", "All" }), OnChange(nameof(ShadowQualityEvent))]
        public ShadowQuality ShadowQuality { get; set; } = QualitySettings.shadows;

        [Slider("Shadow Distance", 0, 200, DefaultValue = 50, Step = 1), OnChange(nameof(ShadowDistanceEvent))]
        public float ShadowDistance { get; set; } = QualitySettings.shadowDistance;

        [Choice("Shadow Resolution", new[] { "Low", "Medium", "High", "Very High" }), OnChange(nameof(ShadowResolutionEvent))]
        public ShadowResolution ShadowResolution { get; set; } = QualitySettings.shadowResolution;

        public FullScreenModeEnum GetFixedFullScreenMode()
        {
            return FullScreenMode == FullScreenModeEnum.MaximizedWindow ? FullScreenModeEnum.Windowed : FullScreenMode;
        }

        public FullScreenModeEnum GetRealFullScreenMode()
        {
            return FullScreenMode == FullScreenModeEnum.Windowed ? FullScreenModeEnum.MaximizedWindow : FullScreenMode;
        }

        public int GetFixedVSyncCount()
        {
            return EnableVSync ? 1 : 0;
        }

        public int GetFixedFramerateCount()
        {
            return EnableFramerateLimit ? FramerateLimit : -1;
        }

        private void FullScreenModeEvent()
        {
            Screen.fullScreenMode = GetFixedFullScreenMode();
            GraphicsUtility.OnQualityLevelChanged();
        }

        private void VSyncFramerateEvent()
        {
            QualitySettings.vSyncCount = GetFixedVSyncCount();
            QualitySettings.maxQueuedFrames = MaximumFrameCount;
            Application.targetFrameRate = GetFixedFramerateCount();
        }

        private void AnisotropicFilteringEvent()
        {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering;
            GraphicsUtility.OnQualityLevelChanged();
        }

        private void ShadowQualityEvent()
        {
            QualitySettings.shadows = ShadowQuality;
            GraphicsUtility.OnQualityLevelChanged();
        }

        private void ShadowDistanceEvent()
        {
            QualitySettings.shadowDistance = ShadowDistance;
            GraphicsUtility.OnQualityLevelChanged();
        }

        private void ShadowResolutionEvent()
        {
            QualitySettings.shadowResolution = ShadowResolution;
            GraphicsUtility.OnQualityLevelChanged();
        }
    }
}
