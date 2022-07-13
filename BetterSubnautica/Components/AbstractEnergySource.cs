using UnityEngine;

namespace BetterSubnautica.Components
{
    public abstract class AbstractEnergySource<T> : IEnergySource where T : Component
    {
        protected T component = null;
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
