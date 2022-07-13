namespace BetterSubnautica.Components
{
    public class EnergyMixinSource : AbstractEnergySource<EnergyMixin>
    {
        public override bool HasEnergy()
        {
            if (Component != null)
            {
                return Component.charge > 0f;
            }

            return false;
        }

        public override void GetValues(out float charge, out float capacity)
        {
            charge = 0f;
            capacity = 0f;

            if (Component != null && Component.battery != null)
            {
                charge = Component.charge;
                capacity = Component.capacity;
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
