using BetterSubnautica.Extensions;
using BetterSubnautica.Helpers;
using UnityEngine;

namespace BetterLights.MonoBehaviours.Lights
{
    public abstract class AbstractLightsController<T> : MonoBehaviour, ILightsController where T : Component
    {
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

        public virtual Light[] Lights { get; protected set; } = [];

        public virtual Color Color { get; protected set; }

        public virtual float IntensityOffset { get; protected set; }

        public virtual float RangeOffset { get; protected set; }

        protected Color[] DefaultColors { get; set; } = [];

        protected float[] DefaultIntensities { get; set; } = [];

        protected float[] DefaultRanges { get; set; } = [];

        protected Timerwatch Timer { get; } = new(1);

        protected virtual void Awake()
        {
            if (Component == null)
            {
                Destroy(this);
                return;
            }

            GetLights();
        }

        protected virtual void Start()
        {
            if (Lights.Length == 0)
            {
                Destroy(this);
                return;
            }

            SetDefaults();
        }

        protected virtual void Update()
        {
            if (!gameObject.activeInHierarchy) return;

            if (Timer.IsFinished())
            {
                GetSettings();

                UpdateColor();
                UpdateIntensity();
                UpdateRange();

                Timer.Restart();
            }
        }

        protected virtual void GetLights()
        {
            if (Component.GetLightsParent() is { } lightsParent)
            {
                Lights = lightsParent.transform.GetLightsInChildren();
            }
        }

        protected abstract void GetSettings();

        private void SetDefaults()
        {
            DefaultColors = new Color[Lights.Length];
            DefaultIntensities = new float[Lights.Length];
            DefaultRanges = new float[Lights.Length];

            for (var i = 0; i < Lights.Length; i++)
            {
                DefaultColors[i] = Lights[i].color;
                DefaultIntensities[i] = Lights[i].intensity;
                DefaultRanges[i] = Lights[i].range;
            }
        }

        public virtual void UpdateColor()
        {
            foreach (var light in Lights)
            {
                if (light.color != Color)
                {
                    light.color = Color;
                }
            }
        }

        public void UpdateIntensity()
        {
            for (var i = 0; i < Lights.Length; i++)
            {
                if (!Mathf.Approximately(Lights[i].intensity, DefaultIntensities[i] + IntensityOffset))
                {
                    Lights[i].intensity = DefaultIntensities[i] + IntensityOffset;
                }
            }
        }

        public void UpdateRange()
        {
            for (var i = 0; i < Lights.Length; i++)
            {
                if (!Mathf.Approximately(Lights[i].range, DefaultRanges[i] + RangeOffset))
                {
                    Lights[i].range = DefaultRanges[i] + RangeOffset;

                    if (Lights[i].gameObject.GetComponent<VFXVolumetricLight>() is { } volumetricLight)
                    {
                        volumetricLight.UpdateScale();
                    }
                }
            }
        }
    }
}
