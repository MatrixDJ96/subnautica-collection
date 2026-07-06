#if BELOWZERO
using System.Collections;
using BetterSubnautica.Extensions;
using UnityEngine;

namespace BetterLights.MonoBehaviours.VolumetricLights
{
    public class SeatruckVolumetricLightsController : AbstractVolumetricLightsController<SeaTruckSegment>
    {
        private SeaTruckLights additionalComponent = null;

        protected override void Awake()
        {
            base.Awake();

            if (component != null)
            {
                additionalComponent = component.gameObject.GetComponent<SeaTruckLights>();

                if (!component.IsMainSegment() || additionalComponent == null || additionalComponent.floodLight == null)
                {
                    Destroy(this);
                    return;
                }

                StartCoroutine(CreateSeatruckVolumetricLightsAsync());
            }
        }

        // The vanilla BZ Seatruck ships bare headlights (dimFloodlightsOnEnter is empty):
        // clone cones onto the floodlight Lights, then hand them to SeaTruckLights so the
        // vanilla dim-on-enter path drives them.
        private IEnumerator CreateSeatruckVolumetricLightsAsync()
        {
            yield return CreateVolumetricLightsAsync(additionalComponent.floodLight.GetComponentsInChildren<Light>(true));

            additionalComponent.dimFloodlightsOnEnter = VolumetricLights;
        }

        protected override void UpdateSettings()
        {
            IntensityOffset = Core.SeatruckSettings.VolumetricLightsIntensityOffset;
        }
    }
}
#endif
