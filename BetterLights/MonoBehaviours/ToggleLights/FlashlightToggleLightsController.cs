using UnityEngine;

namespace BetterLights.MonoBehaviours.ToggleLights
{
    public class FlashlightToggleLightsController : AbstractToggleLightsController<FlashLight>
    {
#if SUBNAUTICA
        protected override bool KeyDown => GameInput.GetButtonDown(Buttons.FlashlightLightsToggle);
#else
        protected override bool KeyDown => Input.GetKeyDown(Core.FlashlightSettings.LightsButtonToggle);
#endif

        protected override float EnergyConsumption => Core.FlashlightSettings.LightsConsumption;

        public override bool CanToggleLightsActive()
        {
            return base.CanToggleLightsActive() && Inventory.main.GetHeldTool() == component;
        }
    }
}
