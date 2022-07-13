using SMLHelper.V2.Json;
using SMLHelper.V2.Options.Attributes;

namespace BetterSubnautica
{
    [Menu("Better Subnautica - Debug")]
    public class Settings : ConfigFile
    {
        [Toggle("Show Debug Info", Tooltip = "Enable debug on screen to show information about entities flagged below")]
        public bool ShowDebugInfo { get; set; } = false;

#if SUBNAUTICA
        [Toggle("Enable Cyclops Info")]
        public bool CyclopsInfo { get; set; } = true;
#endif

        [Toggle("Enable Map room camera Info")]
        public bool MapRoomCameraInfo { get; set; } = true;

        [Toggle("Enable SubRoot Info")]
        public bool SubRootInfo { get; set; } = true;

        [Toggle("Enable Exosuit Info")]
        public bool ExosuitInfo { get; set; } = true;

#if SUBNAUTICA
        [Toggle("Enable Seamoth Info")]
        public bool SeamothInfo { get; set; } = true;
#elif BELOWZERO
        [Toggle("Enable Seatruck Info")]
        public bool SeatruckInfo { get; set; } = true;
#endif

#if BELOWZERO
        [Toggle("Enable Hoverbike Info")]
        public bool HoverbikeInfo { get; set; } = true;
#endif

        [Toggle("Enable Flashlight Info")]
        public bool FlashlightInfo { get; set; } = true;

#if BELOWZERO
        [Toggle("Enable Flashlight Helmet Info")]
        public bool FlashlightHelmetInfo { get; set; } = true;
#endif

        [Toggle("Enable Seaglide Info")]
        public bool SeaglideInfo { get; set; } = true;
    }
}
