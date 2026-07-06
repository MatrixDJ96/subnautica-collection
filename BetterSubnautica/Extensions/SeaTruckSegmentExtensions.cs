#if BELOWZERO

namespace BetterSubnautica.Extensions
{
    public static class SeaTruckSegmentExtensions
    {
        public static bool IsMainSegment(this SeaTruckSegment __instance)
        {
            return SeaTruckSegment.GetHead(__instance) is { } head && head == __instance && __instance.isMainCab;
        }
    }
}
#endif
