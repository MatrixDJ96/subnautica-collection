namespace BetterSubnautica.Components
{
    public interface IEnergySource
    {
        bool HasEnergy();

        void GetValues(out float charge, out float capacity);

        void ConsumeEnergy(float amount);
    }
}