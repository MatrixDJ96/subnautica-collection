using BetterSavegames.Patches;
using BetterSubnautica.Extensions;
using BetterSubnautica.MonoBehaviours;
using BetterSubnautica.Utility;
using System.Collections;
using System.IO;
using UnityEngine;
using UWE;

namespace BetterSavegames.MonoBehaviours
{
    public class SavegameController : AbstractAwakeSingleton<SavegameController>
    {
        public bool Started { get; private set; } = false;

        public bool Saving { get; private set; } = false;
        public bool Copying { get; private set; } = false;
        public float AutosaveTimer { get; private set; } = 0f;

        public string OriginalSlot { get; private set; } = null;
        public string PreviousSlot { get; private set; } = null;
        public string LatestSlot { get; private set; } = null;

        public UserStoragePC UserStorage { get; private set; } = null;

        public static string SlotAuto { get; } = "slotautosave";
        public static string SlotQuick { get; } = "slotquicksave";

        protected void Start()
        {
            StartCoroutine(AsyncStart());
        }

        protected IEnumerator AsyncStart()
        {
            while (
                LightmappedPrefabs.main == null || LightmappedPrefabs.main.IsWaitingOnLoads() ||
                PAXTerrainController.main == null || PAXTerrainController.main.isWorking ||
                uGUI.main == null || uGUI.isLoading || HandReticle.main == null
                )
            {
                yield return null;
            }

            Saving = false;
            Copying = false;
            AutosaveTimer = 0f;

            OriginalSlot = SaveLoadManager.main.currentSlot;
            PreviousSlot = SaveLoadManager.main.currentSlot;
            LatestSlot = SaveLoadManager.main.currentSlot;

            UserStorage = PlatformUtils.main.GetUserStorage() as UserStoragePC;

            if (GetOriginalSlotName(OriginalSlot) is string originalSlot)
            {
                OriginalSlot = originalSlot;
            }

            SaveOriginalSlotName(LatestSlot, OriginalSlot);

            Started = true;
        }

        protected void Update()
        {
            if (Started)
            {
                //DebuggerUtility.ShowMessage($"{OriginalSlot}", $"1 ({GetInstanceID()}) {GetType().Name}.OriginalSlot");
                //DebuggerUtility.ShowMessage($"{PreviousSlot}", $"2 ({GetInstanceID()}) {GetType().Name}.PreviousSlot");
                //DebuggerUtility.ShowMessage($"{LatestSlot}", $"3 ({GetInstanceID()}) {GetType().Name}.LatestSlot");
                //DebuggerUtility.ShowMessage($"{Saving}", $"4 ({GetInstanceID()}) {GetType().Name}.Saving");
                //DebuggerUtility.ShowMessage($"{Copying}", $"5 ({GetInstanceID()}) {GetType().Name}.Copying");
                //DebuggerUtility.ShowMessage($"{CanSaveGame()}", $"6 ({GetInstanceID()}) {GetType().Name}.CanSaveGame");

                if (FreezeTime.freezers.Count == 0)
                {
                    AutosaveTimer += Time.deltaTime;
                }

                if (Core.Settings.EnableAutosave && (AutosaveTimer / 60) >= Core.Settings.AutosaveInterval)
                {
                    StartCoroutine(SaveToSlot(SlotAuto, true));
                }

                if (Input.GetKeyDown(Core.Settings.Quicksave))
                {
                    StartCoroutine(SaveToSlot(SlotQuick));
                }

                if (Input.GetKeyDown(Core.Settings.Quickload))
                {
                    StartCoroutine(LoadLatestSlot());
                }
            }
        }

        public bool CanSaveGame()
        {
            return Started && !Saving && !Copying && IngameMenu.main.GetAllowSaving() && !GameModeUtils.IsPermadeath();
        }

        public string GetSlotPath(string slotName)
        {
            return Path.Combine(UserStorage.savePath, slotName);
        }

        public string GetOriginalSlotName(string slotName)
        {
            var slotNamePath = Path.Combine(GetSlotPath(slotName), "slotname.bin");

            if (File.Exists(slotNamePath))
            {
                var originalSlot = File.ReadAllText(slotNamePath).Base64Decode();

                foreach (var slot in SaveLoadManager.main.GetActiveSlotNames())
                {
                    if (slot == originalSlot)
                    {
                        return originalSlot;
                    }
                }
            }

            return null;
        }

        public void SyncLatestSlotName()
        {
            LatestSlot = SaveLoadManager.main.currentSlot;
        }

        public void SaveOriginalSlotName(string slotName, string originalSlot)
        {
            var slotnamePath = Path.Combine(GetSlotPath(slotName), "slotname.bin");
            File.WriteAllText(slotnamePath, originalSlot.Base64Encode());
        }

        public IEnumerator ClearSlot(string slotName)
        {
            UserStorageUtils.AsyncOperation deleteOperation;
            yield return deleteOperation = UserStorage.DeleteContainerAsync(slotName);

            if (!deleteOperation.GetSuccessful())
            {
                DebuggerUtility.ShowWarning($"Unable to clear slot \"{slotName}\"!", null, false);
                yield break;
            }
        }

        public IEnumerator CreateSlot(string slotName)
        {
            UserStorageUtils.AsyncOperation createOperation;
            yield return createOperation = UserStorage.CreateContainerAsync(slotName);

            if (!createOperation.GetSuccessful())
            {
                DebuggerUtility.ShowWarning($"Unable to create slot \"{slotName}\"!", null, false);
                yield break;
            }
        }

        public IEnumerator CopySlot(string srcSlot, string destSlot)
        {
            if (!Copying)
            {
                Copying = true;

                var srcSlotPath = GetSlotPath(srcSlot);
                var destSlotPath = GetSlotPath(destSlot);

                // Copy files to slot
                UserStorageUtils.CopyOperation finalOperation;
                yield return finalOperation = UserStorage.CopyFilesFromContainerAsync(srcSlot, destSlotPath);

                if (!finalOperation.GetSuccessful())
                {
                    DebuggerUtility.ShowWarning($"Unable to copy files to slot \"{destSlot}\"!", null, false);
                    yield break;
                }

                Copying = false;
            }
        }

        public IEnumerator SaveToSlot(string slotName, bool resetTimer = false)
        {
            if (CanSaveGame())
            {
                Saving = true;

                // Save savegame slot names
                PreviousSlot = LatestSlot;
                LatestSlot = slotName;

                IngameMenu.main.Open();
                IngameMenu.main.mainPanel.SetActive(false);
                IngameMenu.main.SetPleaseWaitVisible(true);

                // Copy current savegame to requested slot
                if (PreviousSlot != LatestSlot)
                {
                    yield return ClearSlot(LatestSlot);
                    yield return CreateSlot(LatestSlot);
                    yield return CopySlot(PreviousSlot, LatestSlot);
                }

                // Execute real savegame
                SaveLoadManager.main.currentSlot = LatestSlot;
                yield return IngameMenu.main.SaveGameAsync();
                SaveLoadManager.main.currentSlot = OriginalSlot;

                IngameMenu.main.SetPleaseWaitVisible(false);
                IngameMenu.main.Close();

                if (resetTimer)
                {
                    AutosaveTimer = 0;
                }

                Saving = false;
            }
        }

        public IEnumerator LoadLatestSlot()
        {
            // Reload available slots
            yield return SaveLoadManager.main.LoadSlotsAsync();
            // Enable force autoload latest savegame
            uGUIMainMenuStartPatch.ForceAutoload = true;
            // Exit to main menu to execute autoload
            IngameMenu.main.QuitGame(false);
        }
    }
}
