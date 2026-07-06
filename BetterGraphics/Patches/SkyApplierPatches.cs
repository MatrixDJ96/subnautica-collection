#if BELOWZERO
using HarmonyLib;
using UnityEngine;

namespace BetterGraphics.Patches
{
    [HarmonyPatch(typeof(SkyApplier))]
    [HarmonyPatch(nameof(SkyApplier.OnEnvironmentChanged))]
    class SkyApplierOnEnvironmentChangedPatch
    {
        static void Postfix(SkyApplier __instance, GameObject environment)
        {
            // This will generate exception on savegame loading but it's ok, it MUST NOT be fixed
            if (environment != null && environment.GetComponent<Base>() is Base component && component.GetCellLightingFor(__instance.transform.position) is BaseCellLighting baseCellLighting)
            {
                baseCellLighting.RegisterSkyApplier(__instance, true);
            }
        }
    }
}
#endif
