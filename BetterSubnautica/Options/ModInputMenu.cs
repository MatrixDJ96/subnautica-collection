#if BELOWZERO
using System.Collections.Generic;
using System.Linq;
using BetterSubnautica.Utility;
using Nautilus.Options;

namespace BetterSubnautica.Options
{
    // One consolidated, category-grouped keybind page under Options > Mods that replaces the
    // per-plugin keybind rows. Rows reuse Nautilus' ModKeybindOption and each plugin's ConfigFile
    // JSON pipeline. The live registry (ModInputUtility) is read every time the panel is built, so
    // every plugin has fed its keybinds by then regardless of plugin load order.
    public class ModInputMenu : ModOptions
    {
        private readonly Dictionary<string, ModInputUtility.KeybindEntry> entriesById = new Dictionary<string, ModInputUtility.KeybindEntry>();

        public ModInputMenu() : base("Better Subnautica - Input")
        {
            OnChanged += OnKeybindChanged;
        }

        public override void BuildModOptions(uGUI_TabbedControlsPanel panel, int modsTabIndex, IReadOnlyCollection<OptionItem> options)
        {
            // The panel is torn down and rebuilt each time it opens, so start from a clean set.
            foreach (var id in entriesById.Keys.ToList())
            {
                RemoveItem(id);
            }
            entriesById.Clear();

            panel.AddHeading(modsTabIndex, Name);

            foreach (var category in ModInputUtility.GetCategories())
            {
                panel.AddHeading(modsTabIndex, category.Key);

                foreach (var entry in category.Value)
                {
                    var option = ModKeybindOption.Create(entry.Id, entry.DisplayName, GameInput.GetPrimaryDevice(), entry.Get(), entry.Tooltip);
                    entriesById[entry.Id] = entry;
                    AddItem(option);
                    option.AddToPanel(panel, modsTabIndex);
                }
            }
        }

        private void OnKeybindChanged(object sender, OptionEventArgs e)
        {
            if (e is KeybindChangedEventArgs keybind && entriesById.TryGetValue(keybind.Id, out var entry))
            {
                entry.Set(keybind.Value);
                entry.OnChanged?.Invoke();
                entry.Config.Save();
            }
        }
    }
}
#endif
