#if BELOWZERO
using BetterSubnautica.Extensions;
using BetterVehicles.MonoBehaviours;
using HarmonyLib;

namespace BetterVehicles.Patches
{
    [HarmonyPatch(typeof(SeaTruckSegment))]
    [HarmonyPatch(nameof(SeaTruckSegment.Start))]
    class SeaTruckSegmentStartPatch
    {
        static void Postfix(SeaTruckSegment __instance)
        {
            if (__instance.IsMainSegment())
            {
                __instance.gameObject.EnsureComponent<SeatruckController>();
            }
        }
    }

    [HarmonyPatch(typeof(SeaTruckSegment))]
    [HarmonyPatch(nameof(SeaTruckSegment.EnterHatch))]
    class SeaTruckSegmentEnterHatchPatch
    {
        static void Prefix(SeaTruckSegment __instance)
        {
            if (__instance.gameObject.GetComponent<SeatruckController>() is SeatruckController controller)
            {
                controller.EnterHatch = true;
                controller.DirectEnter = controller.ForceAction;
            }
        }

        static void Postfix(SeaTruckSegment __instance)
        {
            if (__instance.gameObject.GetComponent<SeatruckController>() is SeatruckController controller)
            {
                controller.EnterHatch = false;
            }
        }
    }

    [HarmonyPatch(typeof(SeaTruckMotor))]
    [HarmonyPatch(nameof(SeaTruckMotor.StopPiloting))]
    class SeaTruckMotorStopPilotingPatch
    {
        static void Prefix(SeaTruckMotor __instance)
        {
            if (__instance.gameObject.GetComponentInParent<SeatruckController>() is SeatruckController controller)
            {
                controller.DirectExit = controller.ForceAction;
            }
        }
    }

    [HarmonyPatch(typeof(SeaTruckSegment))]
    [HarmonyPatch(nameof(SeaTruckSegment.IsWalkable))]
    class SeaTruckSegmentIsWalkablePatch
    {
        static bool Prefix(SeaTruckSegment __instance, ref bool __result)
        {
            if (__instance.gameObject.GetComponent<SeatruckController>() is SeatruckController controller)
            {
                if (controller.DirectExit || (controller.DirectEnter && controller.EnterHatch))
                {
                    controller.Reset();
                    __result = false;
                    return false;
                }
            }

            return true;
        }
    }
}
#endif
