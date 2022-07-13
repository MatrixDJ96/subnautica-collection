using BetterSubnautica.Components;
using BetterSubnautica.Extensions;
using UnityEngine;
using UWE;

namespace BetterLights.MonoBehaviours.ToggleLights
{
    public abstract class AbstractToggleLightsController<T> : MonoBehaviour, IToggleLightsController where T : Component
    {
        protected virtual bool MandatoryToggleLights { get; } = true;
        protected virtual bool MandatoryLightsParent { get; } = true;
        protected virtual bool MandatoryEnergySource { get; } = true;

        protected abstract bool KeyDown { get; }
        protected abstract float EnergyConsumption { get; }

        protected T component = null;
        protected global::ToggleLights toggleLights = null;
        protected GameObject lightsParent = null;
        protected IEnergySource energySource = null;

        protected FMOD_StudioEventEmitter lightsOnSound = null;
        protected FMOD_StudioEventEmitter lightsOffSound = null;
        protected FMODAsset onSound = null;
        protected FMODAsset offSound = null;

        protected bool lightsActive = false;
        public virtual bool LightsActive => lightsActive;

        protected virtual void Awake()
        {
            component = gameObject.GetComponent<T>();

            if (component == null)
            {
                Destroy(this);
                return;
            }

            InitializeToggleLights(component);
            InitializeLightsParent(component);
            InitializeEnergySource(component);
        }

        protected virtual void Start()
        {
            SetLightsActive(LightsActive, true);
        }

        protected virtual void Update()
        {
            if (gameObject.activeInHierarchy)
            {
                UpdateLightsEnergy();

                if (CanToggleLightsActive())
                {
                    ToggleLightsActive();
                }

                FixToggleLights();
                FixLightsParent();
            }
        }

        protected virtual void OnDestroy()
        {
            //DebuggerUtility.ShowMessage($"Component: {component != null} | ToggleLights: {toggleLights != null} | LightsParent: {lightsParent != null} | EnergySource: {energySource != null}", $"({GetInstanceID()}) {GetType().Name}.Destroy");
        }

        protected virtual bool InitializeToggleLights(Component component = null)
        {
            if (component != null)
            {
                toggleLights = component.GetToggleLights();

                if (toggleLights != null)
                {
                    lightsOnSound = toggleLights.lightsOnSound;
                    onSound = toggleLights.onSound;
                    lightsOffSound = toggleLights.lightsOffSound;
                    offSound = toggleLights.offSound;

                    toggleLights.energyPerSecond = 0f;

                    return true;
                }
            }

            if (MandatoryToggleLights)
            {
                Destroy(this);
            }

            return false;
        }

        protected virtual bool InitializeLightsParent(Component component = null)
        {
            if (component != null)
            {
                lightsParent = component.GetLightsParent();

                if (lightsParent == null && toggleLights != null)
                {
                    lightsParent = toggleLights.lightsParent;
                }

                if (lightsParent != null)
                {
                    return true;
                }
            }

            if (MandatoryLightsParent)
            {
                Destroy(this);
            }

            return false;
        }

        protected virtual bool InitializeEnergySource(Component component = null)
        {
            if (component != null)
            {
                energySource = component.GetEnergySource();

                if (energySource != null)
                {
                    return true;
                }
            }

            if (MandatoryEnergySource)
            {
                Destroy(this);
            }

            return false;
        }

        protected virtual void FixLightsParent()
        {
            if (lightsParent != null)
            {
                lightsParent.SetActive(LightsActive);
            }
        }

        protected virtual void FixToggleLights()
        {
            if (toggleLights != null)
            {
                toggleLights.lightsActive = LightsActive;
            }
        }

        protected virtual void UpdateLightsEnergy()
        {
            if (!IsPowered())
            {
                SetLightsActive(false);
            }
            else if (LightsActive && EnergyConsumption > 0f)
            {
                ConsumeEnergy(DayNightCycle.main.deltaTime * EnergyConsumption);
            }
        }

        public virtual bool CanToggleLightsActive()
        {
            return KeyDown && !Player.main.GetPDA().isInUse && FreezeTime.freezers.Count == 0;
        }

        public virtual void SetLightsActive(bool active, bool force = false)
        {
            if (!IsPowered())
            {
                active = false;
            }

            if (LightsActive != active || force)
            {
                if (LightsActive != active)
                {
                    if (active)
                    {
                        if ((bool)lightsOnSound)
                        {
                            Utils.PlayEnvSound(lightsOnSound, lightsOnSound.gameObject.transform.position);
                        }
                        if ((bool)onSound)
                        {
                            Utils.PlayFMODAsset(onSound, transform);
                        }
                    }
                    else
                    {
                        if ((bool)lightsOffSound)
                        {
                            Utils.PlayEnvSound(lightsOffSound, lightsOffSound.gameObject.transform.position);
                        }
                        if ((bool)offSound)
                        {
                            Utils.PlayFMODAsset(offSound, transform);
                        }
                    }
                }

                lightsActive = active;
            }

            FixToggleLights();
            FixLightsParent();
        }

        public virtual void ToggleLightsActive()
        {
            SetLightsActive(!LightsActive);
        }

        public virtual bool IsPowered()
        {
            if (energySource != null)
            {
                return energySource.HasEnergy();
            }

            return false;
        }

        protected virtual void ConsumeEnergy(float amount)
        {
            if (energySource != null)
            {
                energySource.ConsumeEnergy(amount);
            }
        }
    }
}
