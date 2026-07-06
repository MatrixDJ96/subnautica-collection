using UnityEngine;

namespace BetterSubnautica.Components
{
    public abstract class AbstractEnergySource<T> : IEnergySource where T : Component
    {
        private T component;

        public T Component
        {
            get => component;
            set
            {
                if (component == null)
                {
                    component = value;
                }
            }
        }

        public abstract bool HasEnergy();
        public abstract void GetValues(out float charge, out float capacity);
        public abstract void ConsumeEnergy(float amount);
    }
}
