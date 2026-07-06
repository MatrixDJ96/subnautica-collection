#if SUBNAUTICA
namespace BetterSubnautica.MonoBehaviours.Debug
{
    public class SeamothDebuggerController : AbstractLightsDebuggerController<SeaMoth>
    {
        public override bool ShowDebugInfo => Core.Settings.SeamothInfo;
    }
}
#endif
