using UnityEngine;

namespace BetterSubnautica.MonoBehaviours
{
    public abstract class AbstractAwakeSingleton<T> : MonoBehaviour where T : AbstractAwakeSingleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this as T;
        }
        
        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
