#if SUBNAUTICA
using UnityEngine;

namespace BetterLights.MonoBehaviours.Lights
{
    public class CyclopsCameraLightsController : AbstractLightsController<CyclopsExternalCams>
    {
        protected override void GetLights()
        {
            if (Component.cameraLight != null)
            {
                Lights = new Light[] { Component.cameraLight };
            }
        }

        public override void UpdateColor() { }

        protected override void GetSettings()
        {
            Color = UnityEngine.Color.white;
            IntensityOffset = Core.CyclopsSettings.CameraLightsIntensityOffset;
            RangeOffset = Core.CyclopsSettings.CameraLightsRangeOffset;
        }
    }
}
#endif
