using UnityEngine;

namespace BetterLights.MonoBehaviours.ToggleLights
{
    public class ExosuitToggleLightsController : AbstractToggleLightsController<Exosuit>
    {
        protected override bool MandatoryToggleLights { get; } = false;

#if SUBNAUTICA
        protected override bool KeyDown => GameInput.GetButtonDown(Buttons.ExosuitLightsToggle);
#else
        protected override bool KeyDown => Input.GetKeyDown(Core.ExosuitSettings.LightsButtonToggle);
#endif

        protected override float EnergyConsumption => Core.ExosuitSettings.LightsConsumption;

        protected override void Awake()
        {
            base.Awake();

            if (component != null)
            {
                StartCoroutine(CreateToggleLightsAsync());
            }
        }

        public override bool CanToggleLightsActive()
        {
            return base.CanToggleLightsActive() && component.GetPilotingMode();
        }
    }
}
