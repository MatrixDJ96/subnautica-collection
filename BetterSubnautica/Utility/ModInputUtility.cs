#if SUBNAUTICA
using Nautilus.Handlers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetterSubnautica.Utility
{
    public static class ModInputUtility
    {
        // The config KeyCode only seeds the default binding: once the player rebinds the
        // button in the Mod Input tab, the game's own input settings own the key.
        // AvoidConflicts keeps mouse defaults (RMB/MMB) usable alongside the vanilla actions
        // that share them, matching the raw-input behavior the buttons replace.
        public static GameInput.Button RegisterButton(string name, string displayName, string tooltip, string category, KeyCode defaultKeyCode)
        {
            var builder = EnumHandler.AddEntry<GameInput.Button>(name)
                .CreateInput(displayName, tooltip)
                .WithCategory(category)
                .AvoidConflicts();

            if (KeyCodeUtility.GetBindingPath(defaultKeyCode) is { } path)
            {
                builder.WithKeyboardBinding(path);
            }
            else
            {
                builder.SetBindable(GameInput.Device.Keyboard);
            }

            return builder;
        }

        public static string GetButtonName(GameInput.Button button, bool withColor = true)
        {
            var path = GameInput.GetBinding(GameInput.Device.Keyboard, button, GameInput.BindingSet.Primary);

            if (!string.IsNullOrEmpty(path))
            {
                // Without color the callers parse the name (tooltip key ranges), so return the
                // plain human-readable control name instead of the game's key-glyph sprite markup.
                var text = withColor
                    ? GameInput.GetDisplayText(path, "#ADF8FFFF")
                    : InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);

                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }
            }

            var fallback = Language.isNotQuitting ? Language.main.Get("NoInputAssigned") : "No Input Assigned";

            return withColor ? $"<color=#ADF8FFFF>{fallback}</color>" : fallback;
        }
    }
}
#endif

#if BELOWZERO
using System;
using System.Collections.Generic;
using System.Linq;
using Nautilus.Json;
using UnityEngine;

namespace BetterSubnautica.Utility
{
    // Below Zero sizes its GameInput button array once at startup, so a mod action cannot become
    // a native Keyboard-tab row the way it can on Subnautica. Mod keybinds stay KeyCode fields on
    // each plugin's ConfigFile. This registry lets every plugin feed those keybinds to one
    // consolidated "Better Subnautica - Input" options page, mirroring the single categorized Mod
    // Input tab Subnautica gets natively.
    public static class ModInputUtility
    {
        public class KeybindEntry
        {
            public string Id { get; }
            public string DisplayName { get; }
            public string Tooltip { get; }
            public ConfigFile Config { get; }
            public Func<KeyCode> Get { get; }
            public Action<KeyCode> Set { get; }
            public Action OnChanged { get; }

            public KeybindEntry(string id, string displayName, string tooltip, ConfigFile config, Func<KeyCode> get, Action<KeyCode> set, Action onChanged)
            {
                Id = id;
                DisplayName = displayName;
                Tooltip = tooltip;
                Config = config;
                Get = get;
                Set = set;
                OnChanged = onChanged;
            }
        }

        // Category display order matches the Subnautica Mod Input tab.
        private static readonly List<string> CategoryOrder = new List<string>
        {
            "Better Lights",
            "Better Quick Slots",
            "Better Savegames",
            "Better Vehicles",
            "Better PDA"
        };

        private static readonly Dictionary<string, List<KeybindEntry>> EntriesByCategory = new Dictionary<string, List<KeybindEntry>>();

        public static void RegisterKeybind(string category, string displayName, string tooltip, ConfigFile config, Func<KeyCode> get, Action<KeyCode> set, Action onChanged = null)
        {
            if (!EntriesByCategory.TryGetValue(category, out var entries))
            {
                entries = new List<KeybindEntry>();
                EntriesByCategory[category] = entries;
            }

            var id = "BetterInput_" + new string((category + "_" + displayName).Where(char.IsLetterOrDigit).ToArray());
            entries.Add(new KeybindEntry(id, displayName, tooltip, config, get, set, onChanged));
        }

        // Plugin load order is dependency-driven, so emit the known categories in CategoryOrder
        // first and append any extra category in first-seen order.
        public static IEnumerable<KeyValuePair<string, List<KeybindEntry>>> GetCategories()
        {
            foreach (var category in CategoryOrder)
            {
                if (EntriesByCategory.TryGetValue(category, out var entries))
                {
                    yield return new KeyValuePair<string, List<KeybindEntry>>(category, entries);
                }
            }

            foreach (var pair in EntriesByCategory)
            {
                if (!CategoryOrder.Contains(pair.Key))
                {
                    yield return pair;
                }
            }
        }
    }
}
#endif
