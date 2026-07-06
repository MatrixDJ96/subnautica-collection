namespace BetterLights.MonoBehaviours.Lights
{
    public class MapRoomCameraLightsController : AbstractLightsController<MapRoomCamera>
    {
        protected override void GetSettings()
        {
            Color = Core.MapRoomCameraSettings.LightsColor;
            IntensityOffset = Core.MapRoomCameraSettings.LightsIntensityOffset;
            RangeOffset = Core.MapRoomCameraSettings.LightsRangeOffset;
        }
    }
}
