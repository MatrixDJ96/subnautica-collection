#if SUBNAUTICA
using UnityEngine;

namespace BetterLights.MonoBehaviours.ToggleLights
{
    public class SeamothToggleLightsController : AbstractToggleLightsController<SeaMoth>
    {
        protected override bool KeyDown => GameInput.GetButtonDown(Buttons.SeamothLightsToggle);

        protected override float EnergyConsumption => Core.SeamothSettings.LightsConsumption;

        public override bool CanToggleLightsActive()
        {
            return base.CanToggleLightsActive() && component.GetPilotingMode();
        }
    }
}
#endif
