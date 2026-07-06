using BetterSubnautica.Enums;

namespace BetterSubnautica.MonoBehaviours.Debug
{
    public class SubRootDebuggerController : AbstractDebuggerController<SubRoot>
    {
        public override bool ShowDebugInfo => Core.Settings.SubRootInfo;

        protected override bool ShowLights { get; } = false;

        protected override LightsType LightsType => LightsType.None;

        protected override bool LightsActive => false;
    }
}
