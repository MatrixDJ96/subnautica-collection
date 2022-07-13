using BetterSubnautica.Components;
using BetterSubnautica.Enums;
using BetterSubnautica.Utility;
using HarmonyLib;
using System;
using UnityEngine;

namespace BetterSubnautica.Extensions
{
    public static class ComponentExtensions
    {
        public static void WriteComponents(this Component __instance)
        {
            WriteComponents(__instance.gameObject);
        }

        public static void WriteComponents(this GameObject __instance)
        {
            foreach (var component in __instance.GetComponentsInParent<Component>())
            {
                DebuggerUtility.WriteMessage(__instance.name + ".GetComponentsInParent.Name: " + component.name);
                DebuggerUtility.WriteMessage(__instance.name + ".GetComponentsInParent.Type: " + component.GetType());
                DebuggerUtility.WriteMessage("");
            }

            foreach (var component in __instance.GetComponents<Component>())
            {
                DebuggerUtility.WriteMessage(__instance.name + ".GetComponents.Name: " + component.name);
                DebuggerUtility.WriteMessage(__instance.name + ".GetComponents.Type: " + component.GetType());
                DebuggerUtility.WriteMessage("");
            }

            foreach (var component in __instance.GetComponentsInChildren<Component>())
            {
                DebuggerUtility.WriteMessage(__instance.name + ".GetComponentsInChildren.Name: " + component.name);
                DebuggerUtility.WriteMessage(__instance.name + ".GetComponentsInChildren.Type: " + component.GetType());
                DebuggerUtility.WriteMessage("");
            }
        }

        public static ToggleLights GetToggleLights<T>(this T __instance) where T : Component
        {
            if (__instance is SeaMoth)
            {
                return __instance.gameObject.GetComponentInChildren<ToggleLights>(true);
            }

            return __instance.gameObject.GetComponent<ToggleLights>();
        }

        public static GameObject GetLightsParent<T>(this T __instance) where T : Component
        {
            if (__instance.gameObject.transform.Find("lights_parent") is Transform transform)
            {
                return transform.gameObject;
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
            if (__instance.GetPowerRelay() is PowerRelay powerRelay)
            {
                return new PowerRelaySource() { Component = powerRelay };
            }

            if (__instance.GetEnergyInterface() is EnergyInterface energyInterface)
            {
                return new EnergyInterfaceSource() { Component = energyInterface };
            }

            if (__instance.GetEnergyMixin() is EnergyMixin energyMixin)
            {
                return new EnergyMixinSource() { Component = energyMixin };
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
                    catch (Exception) { }
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
                    catch (Exception) { }
                }
            }
        }
    }
}
