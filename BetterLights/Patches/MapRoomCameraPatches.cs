using BetterLights.MonoBehaviours.Lights;
using BetterLights.MonoBehaviours.ToggleLights;
using BetterLights.MonoBehaviours.VolumetricLights;
using BetterSubnautica.Components;
using HarmonyLib;

namespace BetterLights.Patches
{
    [HarmonyPatch(typeof(MapRoomCamera))]
    [HarmonyPatch(nameof(MapRoomCamera.Start))]
    class MapRoomCameraStartPatch
    {
        static void Postfix(MapRoomCamera __instance)
        {
            __instance.gameObject.EnsureComponent<MapRoomCameraLightsController>();

            __instance.gameObject.EnsureComponent<MapRoomCameraToggleLightsController>();

            __instance.gameObject.EnsureComponent<MapRoomCameraVolumetricLightsController>();
        }
    }

    [HarmonyPatch(typeof(MapRoomCamera))]
    [HarmonyPatch(nameof(MapRoomCamera.ControlCamera))]
    class MapRoomCameraControlCameraPatch
    {
#if BELOWZERO
        static void Postfix(MapRoomCamera __instance, Player player, MapRoomScreen screen)
#else
        static void Postfix(MapRoomCamera __instance, MapRoomScreen screen)
#endif
        {
            if (__instance.gameObject.GetComponent<IToggleLightsController>() is { } toggleLightsController)
            {
                toggleLightsController.SetLightsActive(toggleLightsController.LightsActive);
            }

            if (__instance.gameObject.GetComponent<IVolumetricLightsController>() is { } volumetricLightsController)
            {
                volumetricLightsController.DisableVolumes();
            }
        }
    }

    [HarmonyPatch(typeof(MapRoomCamera))]
    [HarmonyPatch(nameof(MapRoomCamera.FreeCamera))]
    class MapRoomCameraFreeCameraPatch
    {
        static void Postfix(MapRoomCamera __instance)
        {
            if (__instance.gameObject.GetComponent<IToggleLightsController>() is { } toggleLightsController)
            {
                toggleLightsController.SetLightsActive(__instance.dockingPoint == null && toggleLightsController.LightsActive);
            }

            if (__instance.gameObject.GetComponent<IVolumetricLightsController>() is { } volumetricLightsController)
            {
                volumetricLightsController.RestoreVolumes();
            }
        }
    }
}
