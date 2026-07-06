using UnityEngine;

namespace BetterLights.MonoBehaviours.ToggleLights
{
    public class MapRoomCameraToggleLightsController : AbstractToggleLightsController<MapRoomCamera>
    {
        protected override bool MandatoryToggleLights { get; } = false;

#if SUBNAUTICA
        protected override bool KeyDown => GameInput.GetButtonDown(Buttons.MapRoomCameraLightsToggle);
#else
        protected override bool KeyDown => Input.GetKeyDown(Core.MapRoomCameraSettings.LightsButtonToggle);
#endif

        protected override float EnergyConsumption => Core.MapRoomCameraSettings.LightsConsumption;

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
#if SUBNAUTICA
            return base.CanToggleLightsActive() && component.active;
#elif BELOWZERO
            return base.CanToggleLightsActive() && component.controllingPlayer != null;
#endif
        }
    }
}
