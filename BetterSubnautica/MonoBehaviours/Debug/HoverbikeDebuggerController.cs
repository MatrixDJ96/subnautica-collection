#if BELOWZERO
namespace BetterSubnautica.MonoBehaviours.Debug
{
    public class HoverbikeDebuggerController : AbstractLightsDebuggerController<Hoverbike>
    {
        public override bool ShowDebugInfo => Core.Settings.HoverbikeInfo;
    }
}
#endif
