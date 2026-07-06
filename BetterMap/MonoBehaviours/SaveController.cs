using System.Collections;
using BetterMap.Components;
using BetterSubnautica.Helpers;
using BetterSubnautica.MonoBehaviours;
using SubnauticaMap;

#if BELOWZERO_MULTI
using System.IO;
using Subnautica.API.Features;
#endif

namespace BetterMap.MonoBehaviours
{

    public class SaveController : AbstractAwakeSingleton<SaveController>
    {
        private object ProcessLock { get; } = new();

        public Timerwatch Timer { get; } = new(10);

        public Controller Controller { get; private set; }

        public string SavePath { get; private set; }
        public string MapPath { get; private set; }

        public UserStorage RealUserStorage { get; private set; }
        public UserStorage FakeUserStorage { get; private set; }

        public bool IsStarted { get; private set; }
        public bool IsLoaded { get; private set; }

        protected override void Awake()
        {
            IsStarted = false;
            IsLoaded = false;

            Controller = gameObject.GetComponent<Controller>();

    #if BELOWZERO_MULTI
            var serverId = Network.Session.Current?.ServerId;

            if (string.IsNullOrWhiteSpace(serverId))
            {
                Controller = null;
            }
    #else
            // TODO: Implement for Subnautica
            Controller = null;
    #endif

            if (Controller == null)
            {
                Destroy(this);
                return;
            }

    #if BELOWZERO_MULTI
            SavePath = Paths.GetMultiplayerClientSavePath();
            MapPath = Path.Combine(serverId!, "SubnauticaMap");
    #else
            // TODO: Implement for Subnautica
    #endif

            RealUserStorage = new UserStoragePC(SavePath);
            RealUserStorage.CreateContainerAsync(MapPath);
            FakeUserStorage = new FakeStorage();

            base.Awake();
        }

        protected void Start()
        {
            StartCoroutine(StartAsync());
        }

        private IEnumerator StartAsync()
        {
            while (!Controller.isStarted)
            {
                yield return null;
            }

            PerformLoad();
        }

        protected void Update()
        {
            if (!IsLoaded) return;

            if (Timer.IsFinished())
            {
                PerformSave();
            }
        }

        private void PerformLoad()
        {
            Core.Logger.LogInfo("Loading map...");

            lock (ProcessLock)
            {
                IsStarted = true;

                Map.All().Clear();
                Fogmap.Release();
                Fogmap.All().Clear();

                Controller.LoadMapIcons();
                Controller.LoadNotes();
                Controller.ReloadMaps();

                IsLoaded = true;
                Timer.Restart();
            }

            Core.Logger.LogInfo("Map loaded!");
        }

        public void PerformSave()
        {
            if (!IsLoaded) return;

            Core.Logger.LogInfo("Saving map...");

            lock (ProcessLock)
            {
                Fogmap.SaveAll();
                Controller.SaveMapIcons();
                Controller.SaveNotes();

                Timer.Restart();
            }

            Core.Logger.LogInfo("Map saved!");
        }
    }
}
