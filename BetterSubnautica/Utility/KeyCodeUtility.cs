#if SUBNAUTICA
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetterSubnautica.Utility
{
    public static class KeyCodeUtility
    {
        public static string GetName(KeyCode keyCode, bool withColor = true)
        {
            if (GetBindingPath(keyCode) is { } path)
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

        public static void SetKeyCode(GameInput.Button button, KeyCode keyCode, GameInput.Device device = GameInput.Device.Keyboard, GameInput.BindingSet bindingSet = GameInput.BindingSet.Primary)
        {
            if (GameInput.IsBindable(device, button) && GetBindingPath(keyCode) is { } path)
            {
                GameInput.SetBinding(device, button, bindingSet, path);
            }
        }

        // Builds the canonical InputSystem binding path without touching Keyboard.current /
        // Mouse.current, so it also works at plugin Awake when no device exists yet.
        public static string GetBindingPath(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.Mouse0: return "<Mouse>/leftButton";
                case KeyCode.Mouse1: return "<Mouse>/rightButton";
                case KeyCode.Mouse2: return "<Mouse>/middleButton";
                case KeyCode.Mouse3: return "<Mouse>/backButton";
                case KeyCode.Mouse4: return "<Mouse>/forwardButton";
                default:
                    if (TryGetKey(keyCode, out var key))
                    {
                        var name = key.ToString();

                        if (name.StartsWith("Digit"))
                        {
                            name = name.Substring("Digit".Length);
                        }
                        else if (!name.StartsWith("OEM"))
                        {
                            name = char.ToLowerInvariant(name[0]) + name.Substring(1);
                        }

                        return $"<Keyboard>/{name}";
                    }

                    return null;
            }
        }

        private static bool TryGetKey(KeyCode keyCode, out Key key)
        {
            switch (keyCode)
            {
                case KeyCode.None:
                    key = Key.None;
                    return false;
                case KeyCode.Alpha0: key = Key.Digit0; return true;
                case KeyCode.Alpha1: key = Key.Digit1; return true;
                case KeyCode.Alpha2: key = Key.Digit2; return true;
                case KeyCode.Alpha3: key = Key.Digit3; return true;
                case KeyCode.Alpha4: key = Key.Digit4; return true;
                case KeyCode.Alpha5: key = Key.Digit5; return true;
                case KeyCode.Alpha6: key = Key.Digit6; return true;
                case KeyCode.Alpha7: key = Key.Digit7; return true;
                case KeyCode.Alpha8: key = Key.Digit8; return true;
                case KeyCode.Alpha9: key = Key.Digit9; return true;
                case KeyCode.Keypad0: key = Key.Numpad0; return true;
                case KeyCode.Keypad1: key = Key.Numpad1; return true;
                case KeyCode.Keypad2: key = Key.Numpad2; return true;
                case KeyCode.Keypad3: key = Key.Numpad3; return true;
                case KeyCode.Keypad4: key = Key.Numpad4; return true;
                case KeyCode.Keypad5: key = Key.Numpad5; return true;
                case KeyCode.Keypad6: key = Key.Numpad6; return true;
                case KeyCode.Keypad7: key = Key.Numpad7; return true;
                case KeyCode.Keypad8: key = Key.Numpad8; return true;
                case KeyCode.Keypad9: key = Key.Numpad9; return true;
                case KeyCode.KeypadPeriod: key = Key.NumpadPeriod; return true;
                case KeyCode.KeypadDivide: key = Key.NumpadDivide; return true;
                case KeyCode.KeypadMultiply: key = Key.NumpadMultiply; return true;
                case KeyCode.KeypadMinus: key = Key.NumpadMinus; return true;
                case KeyCode.KeypadPlus: key = Key.NumpadPlus; return true;
                case KeyCode.KeypadEnter: key = Key.NumpadEnter; return true;
                case KeyCode.KeypadEquals: key = Key.NumpadEquals; return true;
                case KeyCode.Return: key = Key.Enter; return true;
                case KeyCode.LeftControl: key = Key.LeftCtrl; return true;
                case KeyCode.RightControl: key = Key.RightCtrl; return true;
                case KeyCode.LeftCommand: key = Key.LeftMeta; return true;
                case KeyCode.LeftWindows: key = Key.LeftMeta; return true;
                case KeyCode.RightCommand: key = Key.RightMeta; return true;
                case KeyCode.RightWindows: key = Key.RightMeta; return true;
                case KeyCode.Numlock: key = Key.NumLock; return true;
                case KeyCode.BackQuote: key = Key.Backquote; return true;
                case KeyCode.AltGr: key = Key.AltGr; return true;
                default:
                    return Enum.TryParse(keyCode.ToString(), ignoreCase: true, out key);
            }
        }
    }
}
#elif BELOWZERO
using System.Text;
using UnityEngine;

namespace BetterSubnautica.Utility
{
    public static class KeyCodeUtility
    {
        public static string GetName(KeyCode keyCode, bool withColor = true)
        {
            StringBuilder sb = new();

            foreach (var input in GameInput.inputs)
            {
                if (input.keyCode == keyCode)
                {
                    string text = GameInput.GetInputName(input.name);
                    if (text != null)
                    {
                        text = uGUI.GetDisplayTextForBinding(text);
                        sb.Append(withColor ? $"<color=#ADF8FFFF>{text}</color>" : text);
                    }
                }
            }

            if (sb.Length == 0)
            {
                var text = Language.isNotQuitting ? Language.main.Get("NoInputAssigned") : "No Input Assigned";
                sb.Append(withColor ? $"<color=#ADF8FFFF>{text}</color>" : text);
            }

            return sb.ToString();
        }

        public static void SetKeyCode(GameInput.Button button, KeyCode keyCode, GameInput.Device device = GameInput.Device.Keyboard, GameInput.BindingSet bindingSet = GameInput.BindingSet.Primary)
        {
            if (GameInput.IsBindable(device, button))
            {
                var inputName = GameInput.GetKeyCodeAsInputName(keyCode);

                GameInput.SetBindingInternal(device, button, bindingSet, inputName);
            }
        }
    }
}
#endif
