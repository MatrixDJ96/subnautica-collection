using UnityEngine;

namespace BetterSubnautica.MonoBehaviours
{
    public abstract class AbstractGenericSingleton<T> : MonoBehaviour where T : AbstractGenericSingleton<T>
    {
        protected static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject(typeof(T).Name).AddComponent<T>();
                }
                return instance;
            }
        }
    }
}
