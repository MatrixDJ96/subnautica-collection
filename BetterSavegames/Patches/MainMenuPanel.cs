using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
#if SUBNAUTICA_STABLE
using Text = UnityEngine.UI.Text;
#else
using Text = TMPro.TextMeshProUGUI;
#endif

namespace BetterSavegames.Patches
{
    [HarmonyPatch(typeof(MainMenuLoadPanel))]
    [HarmonyPatch(nameof(MainMenuLoadPanel.UpdateLoadButtonState))]
    class MainMenuLoadPanelUpdateLoadButtonStatePatch
    {
        static void Postfix(MainMenuLoadPanel __instance, MainMenuLoadButton lb)
        {
            if (lb.load.FindChild("SaveGameSlot") == null)
            {
                var gameModeObject = lb.load.FindChild("SaveGameMode");
                var gameTimeObject = lb.load.FindChild("SaveGameTime");
                var imageObject = Utils.FindChild(lb.load, "Image");
                var rawImageObject = Utils.FindChild(lb.load, "RawImage");

                if (gameModeObject != null && gameTimeObject != null && imageObject != null)
                {
                    // Create slotImage object
                    var slotImageObject = Object.Instantiate(imageObject, imageObject.transform.parent);

                    // Create slotName object
                    var slotNameObject = Object.Instantiate(gameModeObject, lb.load.transform);
                    slotNameObject.name = "SaveGameSlot";

                    // Apply slotName text
                    var saveGame = lb.saveGame.Replace("slot", "").Trim();
                    slotNameObject.GetComponent<Text>().text = "Slot " + saveGame.Substring(0, 1).ToUpper() + saveGame.Substring(1);

                    // Apply slotName position
                    var positionName = slotNameObject.transform.localPosition;
                    positionName.y = gameTimeObject.transform.localPosition.y;
                    slotNameObject.transform.localPosition = positionName;

                    // Apply slotImage position
                    var positionImage = slotImageObject.transform.localPosition;
                    positionImage.y = -positionImage.y;
                    slotImageObject.transform.localPosition = positionImage;

                    // Set RawImage as an overlay (and "fix" borders)
                    rawImageObject.transform.SetSiblingIndex(slotImageObject.transform.GetSiblingIndex() + 1);
                }
            }
        }
    }
}
