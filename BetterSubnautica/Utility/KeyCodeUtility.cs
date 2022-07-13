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
#if SUBNAUTICA
                var text = Language.main ? Language.main.Get("NoInputAssigned") : "No Input Assigned";
#elif BELOWZERO
                var text = Language.isNotQuitting ? Language.main.Get("NoInputAssigned") : "No Input Assigned";
#endif
                sb.Append(withColor ? $"<color=#ADF8FFFF>{text}</color>" : text);
            }

            return sb.ToString();
        }

        public static KeyCode GetKeyCode(GameInput.Button button, GameInput.Device device = GameInput.Device.Keyboard, GameInput.BindingSet bindingSet = GameInput.BindingSet.Primary)
        {
            var bindingInternal = GameInput.GetBindingInternal(device, button, bindingSet);

            if (bindingInternal != -1)
            {
                return GameInput.inputs[bindingInternal].keyCode;
            }

            return KeyCode.None;
        }

        public static void SetKeyCode(GameInput.Button button, KeyCode keyCode, GameInput.Device device = GameInput.Device.Keyboard, GameInput.BindingSet bindingSet = GameInput.BindingSet.Primary)
        {
            //DebuggerUtility.ShowMessage($"False -> {keyCode} | {device} | {bindingSet}", $"{button}");

            if (GameInput.IsBindable(device, button))
            {
                var inputName = GameInput.GetKeyCodeAsInputName(keyCode);

                GameInput.SetBindingInternal(device, button, bindingSet, inputName);

                //DebuggerUtility.ShowMessage($"True -> {keyCode} ({inputName}) | {device} | {bindingSet}", $"{button}");
            }
        }
    }
}
