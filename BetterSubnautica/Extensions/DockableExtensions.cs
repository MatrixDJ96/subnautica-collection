#if BELOWZERO
namespace BetterSubnautica.Extensions
{
    public static class DockableExtensions
    {
        public static bool HasEnergySource(this Dockable __instance)
        {
            if (__instance != null)
            {
                return __instance.GetEnergySource() != null;
            }

            return false;
        }
    }
}
#endif
