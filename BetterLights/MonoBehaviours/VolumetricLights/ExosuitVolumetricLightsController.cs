namespace BetterLights.MonoBehaviours.VolumetricLights
{
    public class ExosuitVolumetricLightsController : AbstractVolumetricLightsController<Exosuit>
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
            IntensityOffset = Core.ExosuitSettings.VolumetricLightsIntensityOffset;
        }
    }
}
