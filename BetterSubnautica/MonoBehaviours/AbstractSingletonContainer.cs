using System.Collections.Generic;
using UnityEngine;

namespace BetterSubnautica.MonoBehaviours
{
    public abstract class AbstractSingletonContainer<Type, Key, Value> : AbstractGenericSingleton<Type> where Type : AbstractGenericSingleton<Type>
    {
        public virtual IDictionary<Key, Value> Dict { get; } = new Dictionary<Key, Value>();

        private float lastUpdate = 0f;
        protected float updateInterval = 60f;

        protected virtual void Update()
        {
            if (lastUpdate == 0 || lastUpdate + updateInterval < Time.time)
            {
                foreach (var key in new List<Key>(Dict.Keys))
                {
                    var value = Dict[key];

                    if (value is Object unityObject ? unityObject == null : value == null)
                    {
                        Dict.Remove(key);
                    }
                }

                lastUpdate = Time.time;
            }
        }
    }
}
