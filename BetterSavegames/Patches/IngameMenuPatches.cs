using BetterSavegames.MonoBehaviours;
using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace BetterSavegames.Patches
{
    [HarmonyPatch(typeof(IngameMenu))]
    [HarmonyPatch(nameof(IngameMenu.SaveGameAsync))]
    class IngameMenuPatches
    {
        static IEnumerator UpdateSlotCoroutine()
        {
            //DebuggerUtility.ShowMessage($"{DateTime.Now}", $"0 ({ __instance.GetInstanceID()}) {__instance.GetType().Name}.UpdateSlotCoroutine");

            IngameMenu.main.SetPleaseWaitVisible(true);

            yield return new WaitUntil(() => SavegameController.Instance.CanSaveGame());

            yield return SavegameController.Instance.ClearSlot(SaveLoadManager.main.currentSlot);
            yield return SavegameController.Instance.CreateSlot(SaveLoadManager.main.currentSlot);

            // Copy current savegame to requested slot
            yield return SavegameController.Instance.CopySlot(
                SavegameController.Instance.LatestSlot,
                SaveLoadManager.main.currentSlot
            );

            SavegameController.Instance.SyncLatestSlotName();

            // Re-execute SaveGameAsync
            yield return IngameMenu.main.SaveGameAsync();
        }

        static bool Prefix(IngameMenu __instance)
        {
            if (SavegameController.Instance != null)
            {
                // Check if latest slot is autosave or quicksave
                if (SaveLoadManager.main.currentSlot != SavegameController.Instance.LatestSlot && SavegameController.Instance.CanSaveGame())
                {
                    // Fix slot and re-execute SaveGameAsync
                    SavegameController.Instance.StartCoroutine(UpdateSlotCoroutine());
                    return false;
                }
            }

            return true;
        }
    }
}
