#if SUBNAUTICA
using BetterSubnautica.Utility;

namespace BetterLights
{
    public static class Buttons
    {
        public const string Category = "Better Lights";

        public static GameInput.Button FlashlightLightsToggle { get; private set; }

        public static GameInput.Button SeaglideLightsToggle { get; private set; }

        public static GameInput.Button SeamothLightsToggle { get; private set; }

        public static GameInput.Button ExosuitLightsToggle { get; private set; }

        public static GameInput.Button MapRoomCameraLightsToggle { get; private set; }

        internal static void Register()
        {
            FlashlightLightsToggle = ModInputUtility.RegisterButton("BetterLightsFlashlightToggle", "Flashlight Lights Toggle", "Toggles the flashlight lights.", Category, Core.FlashlightSettings.LightsButtonToggle);
            SeaglideLightsToggle = ModInputUtility.RegisterButton("BetterLightsSeaglideToggle", "Seaglide Lights Toggle", "Toggles the Seaglide lights.", Category, Core.SeaglideSettings.LightsButtonToggle);
            SeamothLightsToggle = ModInputUtility.RegisterButton("BetterLightsSeamothToggle", "Seamoth Lights Toggle", "Toggles the Seamoth lights while piloting.", Category, Core.SeamothSettings.LightsButtonToggle);
            ExosuitLightsToggle = ModInputUtility.RegisterButton("BetterLightsExosuitToggle", "Exosuit Lights Toggle", "Toggles the Exosuit lights while piloting.", Category, Core.ExosuitSettings.LightsButtonToggle);
            MapRoomCameraLightsToggle = ModInputUtility.RegisterButton("BetterLightsMapRoomCameraToggle", "Map Room Camera Lights Toggle", "Toggles the map room camera lights while driving it.", Category, Core.MapRoomCameraSettings.LightsButtonToggle);
        }
    }
}
#elif BELOWZERO
using BetterSubnautica.Utility;

namespace BetterLights
{
    public static class Buttons
    {
        public const string Category = "Better Lights";

        internal static void Register()
        {
            var core = Plugin.Core;

            ModInputUtility.RegisterKeybind(Category, "Flashlight Lights Toggle", "Toggles the flashlight lights.", core.FlashlightSettings, () => core.FlashlightSettings.LightsButtonToggle, value => core.FlashlightSettings.LightsButtonToggle = value);
            ModInputUtility.RegisterKeybind(Category, "Helmet Light Toggle", "Toggles the helmet flashlight lights.", core.FlashlightHelmetSettings, () => core.FlashlightHelmetSettings.LightsButtonToggle, value => core.FlashlightHelmetSettings.LightsButtonToggle = value);
            ModInputUtility.RegisterKeybind(Category, "Seaglide Lights Toggle", "Toggles the Seaglide lights.", core.SeaglideSettings, () => core.SeaglideSettings.LightsButtonToggle, value => core.SeaglideSettings.LightsButtonToggle = value);
            ModInputUtility.RegisterKeybind(Category, "Seatruck Lights Toggle", "Toggles the Seatruck lights while piloting.", core.SeatruckSettings, () => core.SeatruckSettings.LightsButtonToggle, value => core.SeatruckSettings.LightsButtonToggle = value);
            ModInputUtility.RegisterKeybind(Category, "Hoverbike Lights Toggle", "Toggles the Hoverbike lights while piloting.", core.HoverbikeSettings, () => core.HoverbikeSettings.LightsButtonToggle, value => core.HoverbikeSettings.LightsButtonToggle = value);
            ModInputUtility.RegisterKeybind(Category, "Exosuit Lights Toggle", "Toggles the Exosuit lights while piloting.", core.ExosuitSettings, () => core.ExosuitSettings.LightsButtonToggle, value => core.ExosuitSettings.LightsButtonToggle = value);
            ModInputUtility.RegisterKeybind(Category, "Map Room Camera Lights Toggle", "Toggles the map room camera lights while driving it.", core.MapRoomCameraSettings, () => core.MapRoomCameraSettings.LightsButtonToggle, value => core.MapRoomCameraSettings.LightsButtonToggle = value);
        }
    }
}
#endif
