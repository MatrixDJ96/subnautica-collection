using System;
using System.Collections;
using System.Linq;
using BetterSubnautica.Enums;
using BetterSubnautica.Extensions;
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

        protected virtual void OnDestroy()
        {
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

        protected IEnumerator CreateVolumetricLightsAsync(Light[] lights = null)
        {
            var request = CraftData.GetPrefabForTechTypeAsync(TechType.Seamoth);
            yield return request;
            GameObject gameObject = request.GetResult();

            if (gameObject.GetComponent<SeaMoth>() is { } seamoth)
            {
                var seamothVolumetricLights = seamoth.volumeticLights;

                if (lights == null)
                {
                    lights = component.gameObject.GetComponentsInChildren<Light>(true);
                    var seamothLights = seamoth.gameObject.GetComponentsInChildren<Light>(true);

                    if (seamothLights.Length != lights.Length || seamothVolumetricLights.Length != lights.Length)
                    {
                        yield break;
                    }
                }

                if (lights.Length == 0 || seamothVolumetricLights.Length == 0)
                {
                    yield break;
                }

                var volumetricLights = new VFXVolumetricLight[lights.Length];

                for (int i = 0; i < lights.Length; i++)
                {
                    volumetricLights[i] = lights[i].gameObject.GetComponent<VFXVolumetricLight>();

                    if (volumetricLights[i] == null)
                    {
                        var template = seamothVolumetricLights[i % seamothVolumetricLights.Length];

                        volumetricLights[i] = lights[i].gameObject.AddComponent<VFXVolumetricLight>();
                        volumetricLights[i].CopyValues(template, CopyType.Fields);
                        volumetricLights[i].lightSource = lights[i];
                        volumetricLights[i].block = null;

                        volumetricLights[i].volumGO = Instantiate(template.volumGO, lights[i].transform);

                        volumetricLights[i].Init();
                        volumetricLights[i].InitMaterialBlock();
                        volumetricLights[i].UpdateMaterial();
                        volumetricLights[i].UpdateScale();
                    }
                }

                this.volumetricLights = volumetricLights;
                Start();
            }
        }

        public void DisableVolumes()
        {
            foreach (var volumetricLight in VolumetricLights)
            {
                volumetricLight.DisableVolume();
            }
        }

        public void RestoreVolumes()
        {
            foreach (var volumetricLight in VolumetricLights)
            {
                volumetricLight.RestoreVolume();
            }
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
