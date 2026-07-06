#if BELOWZERO
namespace BetterSubnautica.MonoBehaviours.Debug
{
    public class SeatruckDebuggerController : AbstractLightsDebuggerController<SeaTruckSegment>
    {
        public override bool ShowDebugInfo => Core.Settings.SeatruckInfo;
    }
}
#endif
