namespace BetterLights.MonoBehaviours.VolumetricLights
{
    public interface IVolumetricLightsController
    {
        VFXVolumetricLight[] VolumetricLights { get; }
        float IntensityOffset { get; set; }

        void DisableVolumes();
        void RestoreVolumes();
        void UpdateMaterial(VFXVolumetricLight volumetricLight, bool forceUpdate);
    }
}
