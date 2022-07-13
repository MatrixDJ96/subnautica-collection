namespace BetterSubnautica.Components
{
    public class EnergyInterfaceSource : AbstractEnergySource<EnergyInterface>
    {
        public override bool HasEnergy()
        {
            if (Component != null)
            {
                return Component.hasCharge;
            }

            return false;
        }

        public override void GetValues(out float charge, out float capacity)
        {
            charge = 0f;
            capacity = 0f;

            if (Component != null)
            {
                Component.GetValues(out charge, out capacity);
            }
        }

        public override void ConsumeEnergy(float amount)
        {
            if (Component != null)
            {
                Component.ConsumeEnergy(amount);
            }
        }
    }
}
