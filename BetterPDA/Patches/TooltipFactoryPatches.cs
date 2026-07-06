using BetterSubnautica.Utility;
using HarmonyLib;
using System.Text;

namespace BetterPDA.Patches
{
    [HarmonyPatch(typeof(TooltipFactory))]
    [HarmonyPatch(nameof(TooltipFactory.ItemActions))]
    class TooltipFactoryItemActionsPatch
    {
        static void Postfix(StringBuilder sb, InventoryItem item)
        {
            if (Inventory.main is Inventory && item != null)
            {
                var itemAction = InventoryUtility.GetEatUseItemAction(item);

                if (itemAction == ItemAction.None)
                {
                    return;
                }

#if SUBNAUTICA
                var keyName = ModInputUtility.GetButtonName(Buttons.EatUse);
#elif BELOWZERO
                var keyName = KeyCodeUtility.GetName(Core.Settings.EatUse);
#endif

                var lines = sb.ToString().Split('\n');
                sb.Clear();

                var merged = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(TooltipFactory.stringButton0) && (lines[i].Contains(TooltipFactory.stringEat) || lines[i].Contains(TooltipFactory.stringUse)))
                    {
                        lines[i] = lines[i].Replace(TooltipFactory.stringButton0, TooltipFactory.stringButton0 + "/" + keyName);
                        merged = true;
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append('\n');
                    }
                    sb.Append(lines[i]);
                }

                if (!merged)
                {
                    if ((itemAction & ItemAction.Eat) != ItemAction.None)
                    {
                        TooltipFactory.WriteAction(sb, keyName, TooltipFactory.GetUseActionString(ItemAction.Eat));
                    }

                    if ((itemAction & ItemAction.Use) != ItemAction.None)
                    {
                        TooltipFactory.WriteAction(sb, keyName, TooltipFactory.GetUseActionString(ItemAction.Use));
                    }
                }
            }
        }
    }
}
