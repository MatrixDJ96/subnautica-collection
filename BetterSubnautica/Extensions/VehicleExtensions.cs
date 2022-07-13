namespace BetterSubnautica.Extensions
{
    public static class VehicleExtensions
    {
        public static bool HasStorage(this Vehicle __instance, TechType techType)
        {
            for (int i = 0; i < __instance.GetSlotCount(); i++)
            {
                if (__instance.GetStorageInSlot(i, techType) is ItemsContainer)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasEnergySource(this Vehicle __instance)
        {
            if (__instance != null)
            {
                return __instance.GetEnergySource() != null;
            }

            return false;
        }
        public static float GetEnergyScalar(this Vehicle __instance)
        {
            if (__instance != null)
            {
                var energySource = __instance.GetEnergySource();

                if (energySource != null)
                {
                    energySource.GetValues(out var charge, out var capacity);

                    if (capacity == 0f)
                    {
                        return 0f;
                    }

                    return charge / capacity;
                }
            }

            return 0f;
        }
    }
}
