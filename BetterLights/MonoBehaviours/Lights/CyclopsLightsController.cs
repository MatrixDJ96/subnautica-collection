#if SUBNAUTICA
using System.Collections.Generic;
using UnityEngine;

namespace BetterLights.MonoBehaviours.Lights
{
    public class CyclopsLightsController : AbstractLightsController<SubRoot>
    {
        protected override void GetLights()
        {
            if (Component.isCyclops && Component.gameObject.GetComponentInChildren<CyclopsLightingPanel>() is { } lightingPanel && lightingPanel.floodlightsHolder != null)
            {
                var cyclopsLights = new List<Light>();

                foreach (Transform item in lightingPanel.floodlightsHolder.transform)
                {
                    if (item.gameObject.GetComponent<Light>() is { } light)
                    {
                        cyclopsLights.Add(light);
                    }
                }

                Lights = cyclopsLights.ToArray();
            }
        }

        protected override void GetSettings()
        {
            Color = UnityEngine.Color.white;
            IntensityOffset = Core.CyclopsSettings.ExternalLightsIntensityOffset;
            RangeOffset = Core.CyclopsSettings.ExternalLightsRangeOffset;
        }
    }
}
#endif
