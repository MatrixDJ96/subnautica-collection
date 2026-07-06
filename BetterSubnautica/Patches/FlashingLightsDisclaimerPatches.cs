using BetterSubnautica.Attributes;
using BetterSubnautica.Extensions;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace BetterSubnautica.Patches
{
    [PrePatch]
    [HarmonyPatch(typeof(FlashingLightsDisclaimer))]
    [HarmonyPatch(nameof(FlashingLightsDisclaimer.SetText))]
    class FlashingLightsDisclaimerSetTextPatch
    {
        static void Postfix(FlashingLightsDisclaimer __instance)
        {
            __instance.text.text +=
                $"\n\n<size=25>" +
                    $"Modded with <color={RandomHexColor()}><size=35><b>BetterSubnautica</b></size>\n" +
                    $"v{Assembly.GetExecutingAssembly().GetName().Version}</color>\n\n" +
                    $"Created by <size=30><color={RandomHexColor()}><b>MatrixDJ96</b></color></size>" +
                $"</size>";
        }

        private static string RandomHexColor()
        {
            return "#" + Random.Range(0, 255).ToHex() + Random.Range(0, 255).ToHex() + Random.Range(0, 255).ToHex() + "FF";
        }
    }
}
