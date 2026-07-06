using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

#if SUBNAUTICA
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace BetterRemote
{
    /// <summary>
    /// Virtual input driven over the HTTP bridge: key/mouse-button presses with real
    /// down/held/up frame semantics and a per-frame mouse-look delta. Two delivery legs:
    /// Harmony postfixes on the legacy <see cref="Input"/> polling API (what the plugins and
    /// Below Zero's GameInput read), and - on Subnautica - device state events queued into the
    /// new InputSystem (what GameInputSystem reads for game actions). A virtual press is
    /// visible to both stacks, like a physical key.
    /// </summary>
    public static class VirtualInput
    {
        private enum Phase
        {
            Scheduled, // Activates next frame (a mid-frame HTTP call must not half-hit Updates).
            Down,      // The single GetKeyDown frame.
            Held,      // GetKey true until the hold time expires.
            Up,        // The single GetKeyUp frame.
            Expired
        }

        private class KeyState
        {
            public int DownFrame;
            public float UpTime;
            public int UpFrame; // 0 until the expiry is first observed; then the GetKeyUp frame.
        }

        private static readonly Dictionary<KeyCode, KeyState> keys = new Dictionary<KeyCode, KeyState>();

        private static ManualLogSource log;

        private static float lookX;
        private static float lookY;
        private static int lookFrames;

        public static void Press(KeyCode key, int holdMs)
        {
            keys[key] = new KeyState
            {
                DownFrame = Time.frameCount + 1,
                UpTime = Time.unscaledTime + Mathf.Max(holdMs, 1) / 1000f
            };
        }

        public static void Look(float dx, float dy, int frames)
        {
            lookX = dx;
            lookY = dy;
            lookFrames = Mathf.Max(frames, 1);
        }

        public static object Status()
        {
            return new
            {
                keys = keys.Select(p => new { key = p.Key.ToString(), phase = Resolve(p.Value).ToString() }).ToArray(),
                look = lookFrames > 0 ? new { dx = lookX, dy = lookY, frames = lookFrames } : null,
                patchedMethods
            };
        }

        /// <summary>
        /// Pure per-frame phase: every reader inside one frame sees the same answer no matter
        /// where its Update sits in the script execution order. The Up frame latches on the
        /// first observation (query or pump) after the hold expires.
        /// </summary>
        private static Phase Resolve(KeyState state)
        {
            var frame = Time.frameCount;

            if (frame < state.DownFrame)
            {
                return Phase.Scheduled;
            }

            if (frame == state.DownFrame)
            {
                return Phase.Down;
            }

            if (state.UpFrame == 0)
            {
                if (Time.unscaledTime < state.UpTime)
                {
                    return Phase.Held;
                }

                state.UpFrame = frame;
                return Phase.Up;
            }

            return frame == state.UpFrame ? Phase.Up : Phase.Expired;
        }

        /// <summary>Removes expired keys and mirrors state changes; called once per frame.</summary>
        public static void Update()
        {
            foreach (var pair in keys.ToArray())
            {
                if (Resolve(pair.Value) == Phase.Expired)
                {
                    keys.Remove(pair.Key);
                }
            }

#if SUBNAUTICA
            MirrorToInputSystem();
#endif

            if (lookFrames > 0)
            {
                lookFrames--;
            }
        }

        // --- Legacy Input queries (consulted by the Harmony postfixes below) ---

        public static bool GetKey(KeyCode key)
        {
            if (!keys.TryGetValue(key, out var state))
            {
                return false;
            }

            var phase = Resolve(state);
            return phase == Phase.Down || phase == Phase.Held;
        }

        public static bool GetKeyDown(KeyCode key)
        {
            return keys.TryGetValue(key, out var state) && Resolve(state) == Phase.Down;
        }

        public static bool GetKeyUp(KeyCode key)
        {
            return keys.TryGetValue(key, out var state) && Resolve(state) == Phase.Up;
        }

        private static float AxisOffset(string axis)
        {
            if (lookFrames <= 0)
            {
                return 0f;
            }

            switch (axis)
            {
                case "Mouse X": return lookX;
                case "Mouse Y": return lookY;
                default: return 0f;
            }
        }

        // --- Harmony postfixes on the legacy UnityEngine.Input polling API ---

        private static void GetKeyPostfix(KeyCode key, ref bool __result) => __result |= GetKey(key);
        private static void GetKeyDownPostfix(KeyCode key, ref bool __result) => __result |= GetKeyDown(key);
        private static void GetKeyUpPostfix(KeyCode key, ref bool __result) => __result |= GetKeyUp(key);

        private static void GetMouseButtonPostfix(int button, ref bool __result) => __result |= GetKey(KeyCode.Mouse0 + button);
        private static void GetMouseButtonDownPostfix(int button, ref bool __result) => __result |= GetKeyDown(KeyCode.Mouse0 + button);
        private static void GetMouseButtonUpPostfix(int button, ref bool __result) => __result |= GetKeyUp(KeyCode.Mouse0 + button);

        private static void GetAxisPostfix(string axisName, ref float __result) => __result += AxisOffset(axisName);

        private static string[] patchedMethods = new string[0];

        /// <summary>
        /// Patches the legacy Input methods one by one: some are managed wrappers, others are
        /// extern icalls whose detourability depends on the runtime - a failed target is logged
        /// and skipped instead of aborting the whole pass.
        /// </summary>
        public static void ApplyPatches(Harmony harmony, ManualLogSource logger)
        {
            log = logger;

            var targets = new (string name, Type[] signature, string postfix)[]
            {
                ("GetKey", new[] { typeof(KeyCode) }, nameof(GetKeyPostfix)),
                ("GetKeyDown", new[] { typeof(KeyCode) }, nameof(GetKeyDownPostfix)),
                ("GetKeyUp", new[] { typeof(KeyCode) }, nameof(GetKeyUpPostfix)),
                ("GetMouseButton", new[] { typeof(int) }, nameof(GetMouseButtonPostfix)),
                ("GetMouseButtonDown", new[] { typeof(int) }, nameof(GetMouseButtonDownPostfix)),
                ("GetMouseButtonUp", new[] { typeof(int) }, nameof(GetMouseButtonUpPostfix)),
                ("GetAxis", new[] { typeof(string) }, nameof(GetAxisPostfix)),
                ("GetAxisRaw", new[] { typeof(string) }, nameof(GetAxisPostfix)),
            };

            var patched = new List<string>();

            foreach (var (name, signature, postfix) in targets)
            {
                try
                {
                    var original = typeof(Input).GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, signature, null);
                    harmony.Patch(original, postfix: new HarmonyMethod(typeof(VirtualInput), postfix));
                    patched.Add(name);
                }
                catch (Exception e)
                {
                    logger.LogWarning($"VirtualInput: cannot patch Input.{name}: {e.GetType().Name} - {e.Message}");
                }
            }

            patchedMethods = patched.ToArray();
            logger.LogInfo($"VirtualInput: patched Input.{string.Join("/", patchedMethods)}");
        }

#if SUBNAUTICA
        // --- InputSystem mirroring (Subnautica's GameInputSystem reads the new InputSystem) ---

        private static string lastMirrored = "";

        private static void MirrorToInputSystem()
        {
            var pressed = keys
                .Where(p =>
                {
                    var phase = Resolve(p.Value);
                    return phase == Phase.Down || phase == Phase.Held;
                })
                .Select(p => p.Key)
                .OrderBy(k => k)
                .ToArray();

            // A mouse delta is consumed by a single InputSystem update, so an active look
            // re-queues every frame; key/button state only needs an event on change.
            var signature = string.Join("+", pressed) + (lookFrames > 0 ? $"|{lookX},{lookY}" : "");

            if (signature == lastMirrored && lookFrames <= 0)
            {
                return;
            }

            lastMirrored = signature;

            try
            {
                var keyboard = Keyboard.current;

                if (keyboard != null)
                {
                    var mapped = pressed
                        .Select(MapKey)
                        .Where(k => k != Key.None)
                        .ToArray();

                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(mapped));
                }

                var mouse = Mouse.current;

                if (mouse != null)
                {
                    var state = new MouseState
                    {
                        position = mouse.position.ReadValue(),
                        delta = lookFrames > 0 ? new Vector2(lookX, lookY) : Vector2.zero
                    };

                    state = state
                        .WithButton(MouseButton.Left, pressed.Contains(KeyCode.Mouse0))
                        .WithButton(MouseButton.Right, pressed.Contains(KeyCode.Mouse1))
                        .WithButton(MouseButton.Middle, pressed.Contains(KeyCode.Mouse2));

                    InputSystem.QueueStateEvent(mouse, state);
                }
            }
            catch (Exception e)
            {
                log.LogWarning($"VirtualInput: InputSystem mirror failed: {e.GetType().Name} - {e.Message}");
            }
        }

        /// <summary>KeyCode-to-Key mapping: same-name parse plus the divergent names.</summary>
        private static Key MapKey(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.Return: return Key.Enter;
                case KeyCode.KeypadEnter: return Key.NumpadEnter;
                case KeyCode.LeftControl: return Key.LeftCtrl;
                case KeyCode.RightControl: return Key.RightCtrl;
                case KeyCode.Mouse0:
                case KeyCode.Mouse1:
                case KeyCode.Mouse2: return Key.None; // Mouse buttons travel in the MouseState.
            }

            var name = keyCode.ToString();

            if (name.StartsWith("Alpha"))
            {
                name = "Digit" + name.Substring(5);
            }
            else if (name.StartsWith("Keypad"))
            {
                name = "Numpad" + name.Substring(6);
            }

            // Case-insensitive: several KeyCode names differ from the InputSystem Key member
            // only by casing (KeyCode.BackQuote vs Key.Backquote), and a case-sensitive parse
            // silently drops them from the mirror so the new InputSystem never sees the press.
            return Enum.TryParse<Key>(name, ignoreCase: true, out var key) ? key : Key.None;
        }
#endif
    }
}
