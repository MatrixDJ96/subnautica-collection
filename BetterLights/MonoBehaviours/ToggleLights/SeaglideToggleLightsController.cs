using UnityEngine;

namespace BetterLights.MonoBehaviours.ToggleLights
{
    public class SeaglideToggleLightsController : AbstractToggleLightsController<Seaglide>
    {
#if SUBNAUTICA
        protected override bool KeyDown => GameInput.GetButtonDown(Buttons.SeaglideLightsToggle);
#else
        protected override bool KeyDown => Input.GetKeyDown(Core.SeaglideSettings.LightsButtonToggle);
#endif

        protected override float EnergyConsumption => Core.SeaglideSettings.LightsConsumption;

        public override bool CanToggleLightsActive()
        {
            return base.CanToggleLightsActive() && Inventory.main.GetHeldTool() == component;
        }
    }
}
