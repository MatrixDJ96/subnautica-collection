#if SUBNAUTICA
using HarmonyLib;

namespace BetterSubnautica.Extensions
{
    public static class CyclopsExternalCamsExtensions
    {
        public static int GetLightState(this CyclopsExternalCams __instance)
        {
            return (int)Traverse.Create(__instance).Field(nameof(CyclopsExternalCams.lightState)).GetValue();
        }

        public static void SetLightState(this CyclopsExternalCams __instance, int lightState)
        {
            var traverse = Traverse.Create(__instance);

            traverse.Field(nameof(CyclopsExternalCams.lightState)).SetValue(lightState);
            traverse.Method(nameof(CyclopsExternalCams.SetLight)).GetValue();
        }
    }
}
#endif
