using UnityEngine;

namespace BetterSubnautica.MonoBehaviours
{
    public abstract class AbstractAwakeSingleton<T> : MonoBehaviour where T : AbstractAwakeSingleton<T>
    {
        protected static T instance;
        public static T Instance => instance;

        protected virtual void Awake()
        {
            if (instance != null)
            {
                Destroy(this);
                return;
            }

            instance = this as T;
        }
    }
}
