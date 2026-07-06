using System;
using UnityEngine;

namespace BetterSubnautica.Components
{
    public static class ToggleLightsEvents
    {
        public static event Action<MonoBehaviour, bool> LightsChanged = delegate { };

        public static void NotifyLightsChanged(MonoBehaviour controller, bool active)
        {
            LightsChanged(controller, active);
        }
    }
}
