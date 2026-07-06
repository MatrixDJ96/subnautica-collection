using BetterSubnautica.Utility;
using HarmonyLib;
#if BELOWZERO
using UnityEngine;
#endif

namespace BetterPDA.Patches
{
    [HarmonyPatch(typeof(uGUI_InventoryTab))]
    [HarmonyPatch(nameof(uGUI_InventoryTab.OnUpdate))]
    class uGUIInventoryTabOnUpdatePatch
    {
#if SUBNAUTICA
        static void Postfix(uGUI_InventoryTab __instance, bool isOpen)
        {
            if (isOpen && ItemDragManager.hoveredItem is InventoryItem item && GameInput.GetButtonDown(Buttons.EatUse))
            {
                if (InventoryUtility.GetEatUseItemAction(item) is ItemAction itemAction && itemAction != ItemAction.None)
                {
                    var inventory = Inventory.main;

                    if (inventory != null)
                    {
                        // Use wins over Eat, matching the vanilla left-click priority.
                        inventory.ExecuteItemAction((itemAction & ItemAction.Use) != ItemAction.None ? ItemAction.Use : ItemAction.Eat, item);
                    }
                }
            }
        }
#elif BELOWZERO
        static void Postfix(uGUI_InventoryTab __instance, bool isOpen)
        {
            if (isOpen && ItemDragManager.hoveredItem is InventoryItem item && Input.GetKeyDown(Core.Settings.EatUse))
            {
                if (InventoryUtility.GetEatUseItemAction(item) is ItemAction itemAction && itemAction != ItemAction.None)
                {
                    var pickupable = item.item;
                    var inventory = Inventory.main;
                    var survival = Player.main.GetComponent<Survival>();

                    if (pickupable != null && inventory != null && survival != null)
                    {
                        switch (itemAction)
                        {
                            case ItemAction.Eat:
                                if (survival.Eat(pickupable.gameObject))
                                {
                                    inventory.TryRemoveItem(pickupable);
                                    Object.Destroy(pickupable.gameObject);
                                }
                                break;
                            case ItemAction.Use:
                                if (survival.Use(pickupable.gameObject, inventory))
                                {
                                    inventory.TryRemoveItem(pickupable);
                                    Object.Destroy(pickupable.gameObject);
                                }
                                break;
                        }
                    }
                }
            }
        }
#endif
    }
}
