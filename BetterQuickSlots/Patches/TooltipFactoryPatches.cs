using BetterQuickSlots.Utility;
using BetterSubnautica.Utility;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BetterQuickSlots.Patches
{
    [HarmonyPatch(typeof(TooltipFactory))]
    [HarmonyPatch(nameof(TooltipFactory.RefreshActionStrings))]
    class TooltipFactoryRefreshActionStringsPatch
    {
        static void Postfix()
        {
            if (uGUI.main != null && uGUI.main.quickSlots is { } __instance)
            {
                __instance.StartCoroutine(UpdateKeyRangeCoroutine(__instance));
            }
        }

        public static IEnumerator UpdateKeyRangeCoroutine(uGUI_QuickSlots __instance)
        {
            yield return CoroutineUtility.WaitUntil(() => __instance.icons != null);

            var inputSlotNames = new List<string>();

            for (int i = 0; i < __instance.icons.Length; i++)
            {
                inputSlotNames.Add(SlotsUtility.GetInputSlotName(i, false));
            }

            var result = string.Empty;
            var slotInts = new List<int>();
            var slotStrings = new List<string>();

            for (int i = 0; i < inputSlotNames.Count; i++)
            {
                if (int.TryParse(inputSlotNames[i], out var parsedInt))
                {
                    slotInts.Add(parsedInt);
                }
                else
                {
                    slotStrings.Add(inputSlotNames[i]);
                }
            }

            slotInts = slotInts.OrderBy(x => x).ToList();

            var startRange = -1;
            var endRange = -1;

            for (int i = 0; i < slotInts.Count; i++)
            {
                if (i + 1 < slotInts.Count)
                {
                    if (slotInts[i] + 1 == slotInts[i + 1])
                    {
                        if (startRange == -1)
                        {
                            startRange = slotInts[i];
                        }

                        endRange = slotInts[i + 1];
                    }
                    else
                    {
                        if (startRange != -1 && endRange != -1)
                        {
                            result = (result + ',' + $"{startRange}-{endRange}").Trim(',');
                            startRange = -1;
                            endRange = -1;
                        }
                    }
                }
            }

            if (startRange != -1 && endRange != -1)
            {
                result = (result + ',' + $"{startRange}-{endRange}").Trim(',');
            }

            TooltipFactory.stringKeyRange15 = "<color=#ADF8FFFF>" + (result + ',' + slotStrings.Join(null, ",")).Trim(',') + "</color>";
        }
    }
}
