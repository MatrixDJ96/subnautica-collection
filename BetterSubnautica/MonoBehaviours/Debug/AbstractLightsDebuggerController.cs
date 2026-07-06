using BetterSubnautica.Enums;
using BetterSubnautica.Extensions;
using UnityEngine;

namespace BetterSubnautica.MonoBehaviours.Debug
{
    public abstract class AbstractLightsDebuggerController<T> : AbstractDebuggerController<T> where T : Component
    {
        private GameObject lightsParent;
        protected GameObject LightsParent
        {
            get
            {
                if (lightsParent == null && Component != null)
                {
                    lightsParent = Component.GetLightsParent();
                }
                return lightsParent;
            }
        }

        protected override LightsType LightsType => LightsActive ? LightsType.External : LightsType.None;

        protected override bool LightsActive => LightsParent != null && LightsParent.activeInHierarchy;
    }
}
