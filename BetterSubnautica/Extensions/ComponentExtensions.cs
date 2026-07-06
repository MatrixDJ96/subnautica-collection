using System;
using BetterSubnautica.Components;
using BetterSubnautica.Enums;
using HarmonyLib;
using UnityEngine;

namespace BetterSubnautica.Extensions
{
    public static class ComponentExtensions
    {
        public static Light[] GetLightsInChildren<T>(this T __instance, bool includeInactive = true) where T : Component
        {
            return __instance.gameObject.GetComponentsInChildren<Light>(includeInactive);
        }

        public static ToggleLights GetToggleLights<T>(this T __instance) where T : Component
        {
            if (__instance.gameObject.GetComponent<ToggleLights>() is { } toggleLights)
            {
                return toggleLights;
            }

            if (__instance.gameObject.GetComponentInChildren<ToggleLights>() is { } toggleLightsInChildren)
            {
                return toggleLightsInChildren;
            }

            return null;
        }

        public static GameObject GetLightsParent<T>(this T __instance) where T : Component
        {
            if (__instance.transform.Find("lights_parent") is { } transform)
            {
                return transform.gameObject;
            }

#if BELOWZERO
            if (__instance is SeaTruckSegment seaTruckSegment)
            {
                if (seaTruckSegment.seatruckLights is { } seaTruckLights)
                {
                    return seaTruckLights.floodLight;
                }
            }
#endif

            if (__instance.GetToggleLights() is { } toggleLights)
            {
                return toggleLights.lightsParent;
            }

            return null;
        }

        public static EnergyMixin GetEnergyMixin<T>(this T __instance) where T : Component
        {
            return __instance.gameObject.GetComponent<EnergyMixin>();
        }

        public static EnergyInterface GetEnergyInterface<T>(this T __instance) where T : Component
        {
            return __instance.gameObject.GetComponent<EnergyInterface>();
        }

        public static PowerRelay GetPowerRelay<T>(this T __instance) where T : Component
        {
            return __instance.gameObject.GetComponent<PowerRelay>();
        }

        public static IEnergySource GetEnergySource<T>(this T __instance) where T : Component
        {
            if (__instance.GetPowerRelay() is { } powerRelay)
            {
                return new PowerRelaySource { Component = powerRelay };
            }

            if (__instance.GetEnergyInterface() is { } energyInterface)
            {
                return new EnergyInterfaceSource { Component = energyInterface };
            }

            if (__instance.GetEnergyMixin() is { } energyMixin)
            {
                return new EnergyMixinSource { Component = energyMixin };
            }

            return null;
        }

        public static void CopyValues<T>(this T __instance, T component, CopyType copyType = CopyType.All) where T : Component
        {
            var instanceTraverse = Traverse.Create(__instance);
            var componentTraverse = Traverse.Create(component);

            if (copyType.HasFlag(CopyType.Fields))
            {
                foreach (var field in instanceTraverse.Fields())
                {
                    try
                    {
                        instanceTraverse.Field(field).SetValue(componentTraverse.Field(field).GetValue());
                    }
                    catch (Exception)
                    {
                        // Ignored
                    }
                }
            }

            if (copyType.HasFlag(CopyType.Properties))
            {
                foreach (var property in instanceTraverse.Properties())
                {
                    try
                    {
                        instanceTraverse.Property(property).SetValue(componentTraverse.Property(property).GetValue());
                    }
                    catch (Exception)
                    {
                        // Ignored
                    }
                }
            }
        }
    }
}
