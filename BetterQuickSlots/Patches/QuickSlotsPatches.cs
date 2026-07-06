using BetterQuickSlots.Utility;
using HarmonyLib;
using System.Collections.Generic;

namespace BetterQuickSlots.Patches
{
    [HarmonyPatch(typeof(QuickSlots))]
    [HarmonyPatch(nameof(QuickSlots.Update))]
    class QuickSlotsUpdatePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return SlotsUtility.Transpiler(instructions);
        }
    }

    [HarmonyPatch(typeof(QuickSlots))]
    [HarmonyPatch(nameof(QuickSlots.SelectInternal))]
    class QuickSlotsSelectInternalPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return SlotsUtility.Transpiler(instructions);
        }
    }

    [HarmonyPatch(typeof(QuickSlots))]
    [HarmonyPatch(nameof(QuickSlots.DeselectInternal))]
    class QuickSlotsDeselectInternalPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return SlotsUtility.Transpiler(instructions);
        }
    }
}
