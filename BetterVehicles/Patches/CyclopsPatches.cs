#if SUBNAUTICA
using HarmonyLib;

namespace BetterVehicles.Patches
{
    [HarmonyPatch(typeof(CyclopsCameraInput))]
    [HarmonyPatch(nameof(CyclopsCameraInput.HandleInput))]
    class CyclopsCameraInputHandleInputPatch
    {
        static void Prefix(CyclopsCameraInput __instance)
        {
            if (__instance.rotationSpeedDamper != Core.CyclopsSettings.CameraRotationSpeedDamper)
            {
                __instance.rotationSpeedDamper = Core.CyclopsSettings.CameraRotationSpeedDamper;
            }
        }
    }
}
#endif
