using BetterSubnautica.Extensions;
using System;
using System.Linq;
using UnityEngine;

namespace BetterLights.MonoBehaviours.VolumetricLights
{
    public abstract class AbstractVolumetricLightsController<T> : MonoBehaviour, IVolumetricLightsController where T : Component
    {
        protected T component = null;

        protected VFXVolumetricLight[] volumetricLights = null;
        public virtual VFXVolumetricLight[] VolumetricLights
        {
            get
            {
                if (volumetricLights == null)
                {
                    volumetricLights = Array.Empty<VFXVolumetricLight>();
                }
                return volumetricLights;
            }
        }

        protected float intensityOffset = 0f;
        public float IntensityOffset
        {
            get => intensityOffset;
            set
            {
                if (intensityOffset != value)
                {
                    intensityOffset = value;
                }
            }
        }

        public void OnDestroy()
        {
            //DebuggerUtility.ShowMessage($"Component: {component != null} | VolumetricLights: {VolumetricLights.Length}", $"({GetInstanceID()}) {GetType().Name}.Destroy");

            foreach (var item in VolumetricLights)
            {
                VolumetricLightsContainer.Instance.Dict.Remove(item.GetInstanceID());
            }
        }

        protected virtual void Awake()
        {
            component = gameObject.GetComponent<T>();

            if (component == null)
            {
                Destroy(this);
                return;
            }
        }

        protected virtual void Start()
        {
            foreach (var volumetricLight in VolumetricLights)
            {
                VolumetricLightsContainer.Instance.Dict[volumetricLight.GetInstanceID()] = this;
            }
        }

        protected void LateUpdate()
        {
            UpdateSettings();
        }

        public void UpdateMaterial(VFXVolumetricLight volumetricLight, bool forceUpdate)
        {
            if (VolumetricLights.Contains(volumetricLight))
            {
                volumetricLight.UpdateMaterial(IntensityOffset, forceUpdate);
            }
        }

        protected abstract void UpdateSettings();
    }
}
