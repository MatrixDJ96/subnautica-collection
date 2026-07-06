#if BELOWZERO
namespace BetterSubnautica.MonoBehaviours.Debug
{
    public class FlashlightHelmetDebuggerController : AbstractLightsDebuggerController<FlashlightHelmet>
    {
        public override bool ShowDebugInfo => Core.Settings.FlashlightHelmetInfo;
    }
}
#endif
