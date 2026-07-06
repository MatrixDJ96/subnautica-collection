namespace BetterSubnautica.MonoBehaviours.Debug
{
    public class FlashlightDebuggerController : AbstractLightsDebuggerController<FlashLight>
    {
        public override bool ShowDebugInfo => Core.Settings.FlashlightInfo;
    }
}
