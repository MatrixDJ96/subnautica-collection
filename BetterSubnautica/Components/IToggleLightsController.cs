namespace BetterSubnautica.Components
{
    public interface IToggleLightsController
    {
        bool LightsActive { get; }

        void SetLightsActive(bool active, bool force = false);
        void ToggleLightsActive();
        bool IsPowered();
    }
}
