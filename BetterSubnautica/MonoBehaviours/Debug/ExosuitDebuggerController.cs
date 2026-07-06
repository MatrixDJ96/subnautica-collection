namespace BetterSubnautica.MonoBehaviours.Debug
{
    public class ExosuitDebuggerController : AbstractLightsDebuggerController<Exosuit>
    {
        public override bool ShowDebugInfo => Core.Settings.ExosuitInfo;
    }
}
