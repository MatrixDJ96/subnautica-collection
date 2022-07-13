using System.Collections;
using UnityEngine;

namespace BetterLights.MonoBehaviours.ToggleLights
{
    public class MapRoomCameraToggleLightsController : AbstractToggleLightsController<MapRoomCamera>
    {
        protected override bool MandatoryToggleLights { get; } = false;

        protected override bool KeyDown => Input.GetKeyDown(Core.MapRoomCameraSettings.LightsButtonToggle);

        protected override float EnergyConsumption => Core.MapRoomCameraSettings.LightsConsumption;

        protected override void Awake()
        {
            base.Awake();

            if (component != null)
            {
                StartCoroutine(CreateToggleLightsAsync());
            }
        }

        private IEnumerator CreateToggleLightsAsync()
        {
            var request = CraftData.GetPrefabForTechTypeAsync(TechType.Seamoth);
            yield return request;
            SeaMoth seamoth = request.GetResult().GetComponent<SeaMoth>();

            if (seamoth == null || !InitializeToggleLights(seamoth))
            {
                Destroy(this);
                yield break;
            }

            Start();
        }

        public override bool CanToggleLightsActive()
        {
            return base.CanToggleLightsActive() && component.controllingPlayer != null;
        }
    }
}
