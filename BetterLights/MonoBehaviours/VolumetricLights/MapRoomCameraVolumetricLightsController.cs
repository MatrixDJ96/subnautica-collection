namespace BetterLights.MonoBehaviours.VolumetricLights
{
    public class MapRoomCameraVolumetricLightsController : AbstractVolumetricLightsController<MapRoomCamera>
    {
        protected override void Awake()
        {
            base.Awake();

            if (component != null)
            {
                StartCoroutine(CreateVolumetricLightsAsync());
            }
        }

        protected override void UpdateSettings()
        {
            IntensityOffset = Core.MapRoomCameraSettings.VolumetricLightsIntensityOffset;
        }
    }
}
