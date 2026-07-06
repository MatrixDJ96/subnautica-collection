#if BELOWZERO_MULTI
using BetterSubnautica.Components;
using Subnautica.API.Extensions;
using Subnautica.API.Features;
using Subnautica.Events.EventArgs;
using UnityEngine;

namespace BetterNitrox.Bridges
{
    public static class ToggleLightsBridge
    {
        public static void Initialize()
        {
            ToggleLightsEvents.LightsChanged += OnLightsChanged;
        }

        private static void OnLightsChanged(MonoBehaviour controller, bool active)
        {
            if (!Network.IsMultiplayerActive)
            {
                return;
            }

            if (controller.GetComponent<SeaTruckLights>() is { } seaTruckLights)
            {
                Core.Logger.LogInfo($"[ToggleLightsBridge] SeaTruck lights changed ({active}): raising Vehicle.OnLightChanged");

                Subnautica.Events.Handlers.Vehicle.OnLightChanged(new LightChangedEventArgs(seaTruckLights.gameObject.GetIdentityId(), active, TechType.SeaTruck));
            }
            else if (controller.GetComponent<Hoverbike>() is { } hoverbike)
            {
                Core.Logger.LogInfo($"[ToggleLightsBridge] Hoverbike lights changed ({active}): raising Vehicle.OnLightChanged");

                Subnautica.Events.Handlers.Vehicle.OnLightChanged(new LightChangedEventArgs(hoverbike.gameObject.GetIdentityId(), active, TechType.Hoverbike));
            }
        }
    }
}
#endif
