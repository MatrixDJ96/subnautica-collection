#if SUBNAUTICA
using BetterSubnautica.Extensions;

namespace BetterLights.MonoBehaviours.ToggleLights
{
    public class CyclopsToggleLightsController : AbstractToggleLightsController<SubRoot>
    {
        protected override bool MandatoryToggleLights { get; } = false;
        protected override bool MandatoryLightsParent { get; } = false;

        private CyclopsLightingPanel lightingPanel = null;
        private CyclopsExternalCams externalCams = null;

        protected override bool KeyDown => false;

        protected override float EnergyConsumption
        {
            get
            {
                var energyPerSecond = 0f;
                if (lightingPanel.lightingOn)
                {
                    if (Player.main.currentSub == component)
                    {
                        energyPerSecond += Core.CyclopsSettings.InternalLightsConsumption;
                    }
                    else
                    {
                        energyPerSecond += Core.CyclopsSettings.InternalLightsConsumption / 2;
                    }
                }
                if (externalCams.GetActive())
                {
                    switch (externalCams.GetLightState())
                    {
                        case 1:
                            energyPerSecond += Core.CyclopsSettings.CameraLightsConsumption;
                            break;
                        case 2:
                            energyPerSecond += Core.CyclopsSettings.CameraLightsConsumption / 2;
                            break;
                    }
                }
                else
                {
                    if (lightingPanel.floodlightsOn)
                    {
                        energyPerSecond += Core.CyclopsSettings.ExternalLightsConsumption;
                    }
                }
                return energyPerSecond;
            }
        }

        public override bool LightsActive => lightsActive = externalCams.GetActive() || lightingPanel.lightingOn || lightingPanel.floodlightsOn;

        protected override void Awake()
        {
            base.Awake();

            if (component != null)
            {
                lightingPanel = component.gameObject.GetComponentInChildren<CyclopsLightingPanel>();
                externalCams = component.gameObject.GetComponentInChildren<CyclopsExternalCams>();

                if (!component.isCyclops || lightingPanel == null || externalCams == null)
                {
                    Destroy(this);
                    return;
                }
            }
        }

        public override bool CanToggleLightsActive()
        {
            return false;
        }

        public override void SetLightsActive(bool active, bool force = false)
        {
            if (!IsPowered())
            {
                active = false;
            }

            if (force)
            {
                // Internal lights
                lightingPanel.lightingOn = active;
                component.ForceLightingState(active);
            }

            // External lights
            lightingPanel.floodlightsOn = active;
            lightingPanel.SetExternalLighting(active);

            // Buttons status
            lightingPanel.UpdateLightingButtons();
        }
    }
}
#endif
