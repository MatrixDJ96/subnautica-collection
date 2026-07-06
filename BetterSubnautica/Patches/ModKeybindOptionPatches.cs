#if BELOWZERO
using System.Linq;
using HarmonyLib;
using Nautilus.Options;

namespace BetterSubnautica.Patches
{
    // Nautilus (pre.51): the bindCallback assigned in ModKeybindOption.AddToPanel regenerates
    // the row label from the stale binding.value, so the row keeps showing the OLD key until
    // the menu reopens (the setting itself updates). Wrap the callback so the label regenerates
    // from the new key string; once upstream fixes it the extra assignment is a no-op.
    [HarmonyPatch(typeof(ModKeybindOption))]
    [HarmonyPatch(nameof(ModKeybindOption.AddToPanel))]
    class ModKeybindOptionAddToPanelPatch
    {
        static void Postfix(ModKeybindOption __instance)
        {
            var binding = __instance.OptionGameObject != null
                ? __instance.OptionGameObject.GetComponentsInChildren<uGUI_Binding>(true).FirstOrDefault(t => t.bindCallback != null)
                : null;

            if (binding == null)
            {
                Core.Logger.LogWarning("ModKeybindOption.AddToPanel Patch: no bound uGUI_Binding found, keybind-label fix skipped");
                return;
            }

            var bindCallback = binding.bindCallback;
            binding.bindCallback = (device, button, bindingSet, s) =>
            {
                bindCallback(device, button, bindingSet, s);

                binding.value = s;
                binding.RefreshValue();
            };
        }
    }
}
#endif
