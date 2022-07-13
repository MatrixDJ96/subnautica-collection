using BetterSubnautica.Extensions;
using HarmonyLib;
using System.Collections.Generic;

namespace BetterVehicles.Patches
{
    [HarmonyPatch(typeof(BaseUpgradeConsoleGeometry))]
#if SUBNAUTICA
    [HarmonyPatch(nameof(BaseUpgradeConsoleGeometry.GetVehicleInfo))]
    class BaseUpgradeConsoleGeometryGetDockedInfoPatch
    {
        static void Postfix(BaseUpgradeConsoleGeometry __instance, ref string __result, Vehicle vehicle)
        {
            var dockable = vehicle;
#elif BELOWZERO
    [HarmonyPatch(nameof(BaseUpgradeConsoleGeometry.GetDockedInfo))]
    class BaseUpgradeConsoleGeometryGetDockedInfoPatch
    {
        static void Postfix(BaseUpgradeConsoleGeometry __instance, ref string __result, Dockable dockable)
        {
#endif
            if (dockable != null && dockable.HasEnergySource() && dockable.gameObject.GetComponent<LiveMixin>() is LiveMixin liveMixin)
            {
                var result = new List<string>();
                var chunks = __result.Split('\n');

                var energyScalar = dockable.GetEnergyScalar();
                var healthFraction = liveMixin.GetHealthFraction();

                foreach (var chunk in chunks)
                {
                    if (!chunk.Contains(Language.main.Get("SubmersibleFullyCharged")) && !chunk.Contains(Language.main.Get("SubmersibleCharging")))
                    {
                        result.Add(chunk.Trim());

                        if (chunk.Contains(Language.main.Get("SubmersibleDocked")))
                        {
                            if (energyScalar < 1f)
                            {
                                result.Add("<size=30>" + Language.main.GetFormat("VehicleStatusFormat", healthFraction, energyScalar) + "</size>");
                            }
                            else
                            {
                                result.Add("<size=30>" + Language.main.GetFormat("VehicleStatusChargedFormat", healthFraction) + "</size>");
                            }
                        }
                    }
                }

                __result = result.ToArray().Join(null, "\n").Trim().ToUpper();
            }
        }
    }
}
