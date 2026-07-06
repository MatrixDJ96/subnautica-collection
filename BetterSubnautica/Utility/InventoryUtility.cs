namespace BetterSubnautica.Utility
{
    public static class InventoryUtility
    {
#if SUBNAUTICA
        public static ItemAction GetEatUseItemAction(InventoryItem item)
        {
            if (item != null && Inventory.main is Inventory inventory)
            {
                return inventory.GetAllItemActions(item) & (ItemAction.Eat | ItemAction.Use);
            }

            return ItemAction.None;
        }
#elif BELOWZERO
        public static ItemAction GetEatUseItemAction(InventoryItem item)
        {
            var result = ItemAction.None;

            if (item != null && item.item is Pickupable pickupable)
            {
                var techType = pickupable.GetTechType();

                var hunger = GameModeManager.GetOption<bool>(GameOption.Hunger);
                var thirst = GameModeManager.GetOption<bool>(GameOption.Thirst);
                var oxygen = GameModeManager.GetOption<bool>(GameOption.OxygenDepletes) && GameModeManager.GetOption<bool>(GameOption.OrganicOxygenSources);
                var cold = GameModeManager.GetOption<bool>(GameOption.BodyTemperatureDecreases);
                var vegetarian = GameModeManager.GetOption<bool>(GameOption.VegetarianDiet);

                if (pickupable.GetComponentInParent<Planter>() == null && pickupable.GetComponent<Eatable>() is Eatable eatable)
                {
                    if (!vegetarian || !TechTypeGroups.IsTechTypeInGroup(techType, TechTypeGroup.NonVegetarian))
                    {
                        if ((oxygen && techType == TechType.Bladderfish)
                            || (hunger && eatable.GetFoodValue() != 0f)
                            || (thirst && eatable.GetWaterValue() != 0f)
                            || (cold && eatable.coldMeterValue < 0f))
                        {
                            result |= ItemAction.Eat;
                        }
                    }
                }

                if (techType == TechType.FirstAidKit || techType == TechType.WaterPurificationTablet)
                {
                    result |= ItemAction.Use;
                }
            }

            return result;
        }
#endif
    }
}
