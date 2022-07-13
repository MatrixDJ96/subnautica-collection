using BetterSubnautica.Extensions;
using UnityEngine;

namespace BetterLights.MonoBehaviours.Lights
{
    public abstract class AbstractLightsController<T> : MonoBehaviour, ILightsController where T : Component
    {
        protected T component = null;

        protected float lastColorUpdate = 0f;
        protected float lastIntensityUpdate = 0f;
        protected float lastRangeUpdate = 0f;

        protected float[] startIntensities = null;
        protected float[] startRanges = null;

        protected Light[] lights = null;
        public virtual Light[] Lights => lights;

        protected Color color = Color.white;
        public Color Color
        {
            get => color;
            set
            {
                if (color != value)
                {
                    color = value;
                    lastColorUpdate = 0;
                }
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
                    lastIntensityUpdate = 0f;
                }
            }
        }

        protected float rangeOffset = 0f;
        public float RangeOffset
        {
            get => rangeOffset;
            set
            {
                if (rangeOffset != value)
                {
                    rangeOffset = value;
                    lastRangeUpdate = 0;
                }
            }
        }

        public float UpdateInterval { get; set; } = 60f;

        protected virtual void Awake()
        {
            component = gameObject.GetComponent<T>();

            if (component == null)
            {
                Destroy(this);
                return;
            }

            var lightsParent = component.GetLightsParent();
            var toggleLights = component.GetToggleLights();

            if (lightsParent == null && toggleLights != null)
            {
                lightsParent = toggleLights.lightsParent;
            }

            if (lightsParent != null)
            {
                lights = lightsParent.GetComponentsInChildren<Light>(true);
            }
        }

        protected virtual void Start()
        {
            if (lights != null && lights.Length > 0)
            {
                startIntensities = new float[lights.Length];
                startRanges = new float[lights.Length];

                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i] != null)
                    {
                        startIntensities[i] = lights[i].intensity;
                        startRanges[i] = lights[i].range;
                    }
                }
            }
        }

        protected virtual void Update()
        {
            if (gameObject.activeInHierarchy)
            {
                UpdateColor();
                UpdateIntensity();
                UpdateRange();
            }
        }

        protected virtual void LateUpdate()
        {
            UpdateSettings();
        }

        protected virtual void OnDestroy()
        {
            //DebuggerUtility.ShowMessage($"Component: {component != null} | Lights: {lights != null}", $"({GetInstanceID()}) {GetType().Name}.Destroy");
        }

        public virtual void UpdateColor()
        {
            if (lastColorUpdate == 0 || lastColorUpdate + UpdateInterval < Time.time)
            {
                if (lights != null && lights.Length > 0)
                {
                    for (int i = 0; i < lights.Length; i++)
                    {
                        if (lights[i] != null && lights[i].color != Color)
                        {
                            lights[i].color = Color;
                        }
                    }

                    lastColorUpdate = Time.time;
                }
            }
        }

        public virtual void UpdateIntensity()
        {
            if (lastIntensityUpdate == 0 || lastIntensityUpdate + UpdateInterval < Time.time)
            {
                if (lights != null && lights.Length > 0)
                {
                    for (int i = 0; i < lights.Length; i++)
                    {
                        if (lights[i] != null && !Mathf.Approximately(lights[i].intensity, startIntensities[i] + IntensityOffset))
                        {
                            lights[i].intensity = startIntensities[i] + IntensityOffset;
                        }
                    }

                    lastIntensityUpdate = Time.time;
                }
            }
        }

        public virtual void UpdateRange()
        {
            if (lastRangeUpdate == 0 || lastRangeUpdate + UpdateInterval < Time.time)
            {
                if (lights != null && lights.Length > 0)
                {
                    for (int i = 0; i < lights.Length; i++)
                    {
                        if (lights[i] != null && !Mathf.Approximately(lights[i].range, startRanges[i] + RangeOffset))
                        {
                            lights[i].range = startRanges[i] + RangeOffset;

                            if (lights[i].gameObject.GetComponent<VFXVolumetricLight>() is VFXVolumetricLight volumetricLight)
                            {
                                volumetricLight.UpdateScale();
                            }
                        }
                    }

                    lastRangeUpdate = Time.time;
                }
            }
        }

        protected abstract void UpdateSettings();
    }
}
