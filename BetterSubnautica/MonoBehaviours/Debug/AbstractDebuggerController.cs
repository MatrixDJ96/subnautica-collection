using BetterSubnautica.Components;
using BetterSubnautica.Enums;
using BetterSubnautica.Extensions;
using BetterSubnautica.Utility;
using UnityEngine;

namespace BetterSubnautica.MonoBehaviours.Debug
{
    public abstract class AbstractDebuggerController<T> : MonoBehaviour where T : Component
    {
        private bool lastEnabled;
        public abstract bool ShowDebugInfo { get; }

        private T component;
        protected T Component
        {
            get
            {
                if (component == null)
                {
                    component = gameObject.GetComponent<T>();
                }
                return component;
            }
        }

        protected virtual bool ShowLights { get; } = true;

        protected abstract LightsType LightsType { get; }
        protected abstract bool LightsActive { get; }

        private float energyPerSecond;
        protected float EnergyPerSecond
        {
            get
            {
                if (energyPerSecond is > -0.0001f and < 0.0001f)
                {
                    energyPerSecond = 0f;
                }
                return energyPerSecond;
            }
            set => energyPerSecond = value;
        }

        private IEnergySource energySource;
        protected IEnergySource EnergySource
        {
            get
            {
                if (energySource == null && Component != null)
                {
                    energySource = Component.GetEnergySource();
                }
                return energySource;
            }
        }

        protected virtual float Capacity
        {
            get
            {
                if (EnergySource == null)
                {
                    return 0f;
                }

                EnergySource.GetValues(out _, out var capacity);
                return capacity;
            }
        }

        protected float LastCharge { get; set; }
        protected virtual float Charge
        {
            get
            {
                if (EnergySource == null)
                {
                    return 0f;
                }

                EnergySource.GetValues(out var charge, out _);
                return charge;
            }
        }

        protected float LastUpdate { get; set; }

        protected float PercentCharge => Capacity > 0f ? (Charge * 100) / Capacity : 0f;

        protected virtual void OnDisable()
        {
            DeleteMessages();
        }

        protected virtual void OnDestroy()
        {
            DeleteMessages();
        }

        protected virtual void Awake()
        {
            if (Component == null)
            {
                Destroy(this);
                return;
            }

            UpdateInfo(false);
        }

        protected virtual void Update()
        {
            var showDebugInfo = Core.Settings.ShowDebugInfo && ShowDebugInfo;

            if (showDebugInfo)
            {
                ShowMessages();

                if (LastUpdate + 1f < Time.time)
                {
                    UpdateInfo();
                }
            }
            else
            {
                if (lastEnabled)
                {
                    DeleteMessages();
                }
            }

            lastEnabled = showDebugInfo;
        }

        protected virtual void UpdateInfo(bool withEnergy = true)
        {
            if (withEnergy)
            {
                EnergyPerSecond = Charge - LastCharge;
            }

            LastCharge = Charge;
            LastUpdate = Time.time;
        }

        protected virtual void ShowMessages()
        {
            if (ShowLights)
            {
                DebuggerUtility.ShowMessage($"{LightsActive:0.##} ({LightsType})", $"({GetInstanceID()}) {GetType().Name}.LightsStatus");
            }

            DebuggerUtility.ShowMessage($"{EnergyPerSecond:+0.####;-0.####;0.####}", $"({GetInstanceID()}) {GetType().Name}.EnergyPerSecond");
            DebuggerUtility.ShowMessage($"{Charge:0.##}/{Capacity:0.##} ({PercentCharge:0.##}%)", $"({GetInstanceID()}) {GetType().Name}.AvailableEnergy");
            DebuggerUtility.ShowMessage("", $"({GetInstanceID()}) {GetType().Name}.ZZZ");
        }

        protected virtual void DeleteMessages()
        {
            if (ShowLights)
            {
                DebuggerUtility.RemoveMessage($"({GetInstanceID()}) {GetType().Name}.LightsStatus");
            }

            DebuggerUtility.RemoveMessage($"({GetInstanceID()}) {GetType().Name}.EnergyPerSecond");
            DebuggerUtility.RemoveMessage($"({GetInstanceID()}) {GetType().Name}.AvailableEnergy");
            DebuggerUtility.RemoveMessage($"({GetInstanceID()}) {GetType().Name}.ZZZ");
        }
    }
}
