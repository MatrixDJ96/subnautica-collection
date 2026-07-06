namespace BetterSubnautica.MonoBehaviours.Debug
{
    public class SeaglideDebuggerController : AbstractLightsDebuggerController<Seaglide>
    {
        public override bool ShowDebugInfo => Core.Settings.SeaglideInfo;
    }
}
