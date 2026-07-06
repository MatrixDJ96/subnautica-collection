namespace BetterSubnautica.MonoBehaviours.Debug
{
    public class MapRoomCameraDebuggerController : AbstractLightsDebuggerController<MapRoomCamera>
    {
        public override bool ShowDebugInfo => Core.Settings.MapRoomCameraInfo;
    }
}
