#if !SUBNAUTICA_STABLE
using UWE;
#endif

namespace BetterSubnautica.Utility
{
    public class FreezeTimeUtility
    {
#if SUBNAUTICA_STABLE
        public static string PDAId { get => "PDAPause"; }
        public static string IngameMenuId { get => "PDAPause"; }

#else
        public static FreezeTime.Id PDAId { get => FreezeTime.Id.PDA; }
        public static FreezeTime.Id IngameMenuId { get => FreezeTime.Id.IngameMenu; }
#endif
    }
}
