#if BELOWZERO
using BetterSubnautica.Extensions;
using HarmonyLib;
using UnityEngine;

namespace BetterGraphics.Patches
{
    [HarmonyPatch(typeof(Base))]
    [HarmonyPatch(nameof(Base.UpdateSkyAppliers))]
    class BaseUpdateSkyAppliersPatch
    {
        static bool Prefix(Base __instance)
        {
            foreach (var component in __instance.gameObject.GetComponentsInChildren<SkyApplier>())
            {
                // Send environment only to objects inside base
                if (component != null && __instance.GetCellIndex(component.transform.position) > -1)
                {
                    SkyEnvironmentChanged.Send(component.gameObject, __instance);
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Base))]
    [HarmonyPatch(nameof(Base.RebuildGeometry))]
    class BaseRebuildGeometryPatch
    {
        static void Postfix(Base __instance)
        {
            foreach (var component in __instance.gameObject.GetComponentsInChildren<Transform>())
            {
                if (component != null && component.gameObject.name == "Large_Aquarium_02_glass")
                {
                    if (component.gameObject.GetComponent<MeshRenderer>() is MeshRenderer renderer)
                    {
                        // Decrease alpha value (default 0.314f)
                        renderer.material.SetColor("_Color", renderer.material.GetColor("_Color").WithAlpha(0.2f));
                        // Restore value from Subnautica
                        renderer.material.SetFloat("_SpecInt", 3f);
                        renderer.material.SetFloat("_Shininess", 7f);
                    }
                }
            }
        }
    }
}
#endif
