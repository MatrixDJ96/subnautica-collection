using BetterSubnautica.MonoBehaviours.Debug;
using HarmonyLib;

namespace BetterSubnautica.Patches.Debug
{
    [HarmonyPatch(typeof(MapRoomCamera))]
    [HarmonyPatch(nameof(MapRoomCamera.Start))]
    class MapRoomCameraStartPatch
    {
        static void Postfix(MapRoomCamera __instance)
        {
            __instance.gameObject.EnsureComponent<MapRoomCameraDebuggerController>();
        }
    }
}
