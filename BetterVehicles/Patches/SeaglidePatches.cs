using HarmonyLib;

namespace BetterVehicles.Patches
{
#if SUBNAUTICA_STABLE
    [HarmonyPatch(typeof(PlayerTool))]
    [HarmonyPatch(nameof(PlayerTool.Awake))]
    class SeaglideStartPatch
    {
        static void Prefix(PlayerTool __instance)
        {
            if (__instance is Seaglide seaglide)
            {
                seaglide.toggleLights.lightState = 2;

            }
        }
    }

    [HarmonyPatch(typeof(Seaglide))]
    [HarmonyPatch(nameof(Seaglide.Update))]
    class SeaglideUpdatePatch
    {
        static void Prefix(Seaglide __instance, out int __state)
        {
            __state = __instance.toggleLights.lightState;
        }

        static void Postfix(Seaglide __instance, int __state)
        {
            __instance.toggleLights.lightState = __state;
        }
    }

    [HarmonyPatch(typeof(PlayerTool))]
    [HarmonyPatch(nameof(PlayerTool.OnAltDown))]
    class SeaglideOnAltDownPatch
    {
        static void Prefix(PlayerTool __instance)
        {
            if (__instance is Seaglide seaglide)
            {
                seaglide.toggleLights.lightState = (seaglide.toggleLights.lightState == 2 ? 0 : 2);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerTool))]
    [HarmonyPatch(nameof(PlayerTool.GetCustomUseText))]
    class PlayerToolGetCustomUseTextPatch
    {
        static void Postfix(PlayerTool __instance, ref string __result)
        {
            if (__instance is Seaglide seaglide)
            {
                var lightState = seaglide.toggleLights.lightState;
                var lightsActive = seaglide.toggleLights.lightsActive;

                string mapText = (lightState == 0 ? "Spegni" : "Accendi") + " mappa (<color=#ADF8FFFF>{0}</color>)";
                string lightText = (lightsActive ? "Spegni" : "Accendi") + " torcia (<color=#ADF8FFFF>{0}</color>)";

                __result = LanguageCache.GetButtonFormat(mapText, GameInput.Button.AltTool) + " / " + LanguageCache.GetButtonFormat(lightText, GameInput.Button.RightHand);
            }
        }
    }
#elif BELOWZERO
    [HarmonyPatch(typeof(Seaglide))]
    [HarmonyPatch(nameof(Seaglide.Start))]
    class SeaglideStartPatch
    {
        static void Postfix(Seaglide __instance)
        {
            if (__instance.gameObject.GetComponent<VehicleInterface_MapController>() is VehicleInterface_MapController mapController)
            {
                mapController.mapActive = false;
            }
        }
    }
#endif
}
