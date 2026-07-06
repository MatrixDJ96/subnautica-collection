using UnityEngine;

namespace BetterLights.MonoBehaviours.Lights
{
    public interface ILightsController
    {
        Light[] Lights { get; }

        Color Color { get; }
        float IntensityOffset { get; }
        float RangeOffset { get; }

        void UpdateColor();
        void UpdateIntensity();
        void UpdateRange();
    }
}
