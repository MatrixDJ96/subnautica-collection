using BetterSubnautica.MonoBehaviours;
using SubnauticaMap;

namespace BetterMap.MonoBehaviours
{

    public class PingController : AbstractAwakeSingleton<PingController>
    {
        public Controller Controller { get; private set; }

        protected override void Awake()
        {
            Controller = gameObject.GetComponent<Controller>();

            if (Controller == null)
            {
                Destroy(this);
                return;
            }

            base.Awake();
        }

        public void ReloadPings()
        {
            Core.Logger.LogInfo("Reloading pings...");

            Controller.pingMapIconList.Clear();

            using (var enumerator = PingManager.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var key = enumerator.Current.Key;
                    var ping = enumerator.Current.Value;

                    if ((bool)(UnityEngine.Object)ping.origin)
                    {
                        Controller.pingMapIconList.Add(key, Controller.CreatePingMapIcon(ping));
                    }
                }
            }

            Core.Logger.LogInfo("Pings reloaded!");
        }
    }
}
