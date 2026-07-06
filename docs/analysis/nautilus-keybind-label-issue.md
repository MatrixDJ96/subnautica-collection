# Nautilus (Below Zero) — keybind row label does not refresh after rebinding

Draft for an upstream Nautilus issue. Not filed. The bug is worked around mod-side in
`BetterSubnautica/Patches/ModKeybindOptionPatches.cs` (`#if BELOWZERO`, verified live
2026-07-07); this document records the upstream root cause so the workaround can be dropped once
Nautilus fixes it.

## Summary

On Below Zero, rebinding a `ModKeybindOption` key in a mod's options page updates the stored
`KeyCode` correctly, but the row keeps displaying the OLD key until the options menu is closed
and reopened. The visible label and the actual binding disagree until the next menu rebuild.

## Environment

- Nautilus, Below Zero build (`#if BELOWZERO` path).
- Any mod page rendering keybind rows through `ModKeybindOption.Create` +
  `ModKeybindOption.AddToPanel`.

## Repro

1. Open a mod options page that contains a `ModKeybindOption` row (e.g. any `[Keybind]`
   `KeyCode` on a Nautilus `ConfigFile`).
2. Click the binding and press a new key.
3. Observe: the row still shows the previous key. The underlying setting is already the new key
   (a save writes the new value; reopening the menu shows the new key on the rebuilt row).

## Root cause

`Nautilus/Options/ModKeybindOption_BelowZero.cs`, `AddToPanel`, the BZ `bindCallback`
(lines 112-121):

```csharp
binding.bindCallback = new Action<GameInput.Device, GameInput.Button, GameInput.BindingSet, string>((_, _1, _2, s) =>
{
    var keyCode = StringToKeyCode(s);
    binding.value = uGUI.GetDisplayTextForBinding(GameInput.GetInputName(binding.value)); // recomputed from the STALE binding.value
    OnChange(Id, keyCode);
    parentOptions.OnChange<KeyCode, KeybindChangedEventArgs>(Id, StringToKeyCode(s));
    binding.RefreshValue();
});
```

The callback receives the newly-pressed key as `s`, and it does propagate the new value to the
option (`OnChange(Id, keyCode)` and `parentOptions.OnChange(..., StringToKeyCode(s))`, both
derived from `s`). But the display label is recomputed from `binding.value` — the field's
PREVIOUS contents — instead of from `s`. `binding.RefreshValue()` then re-renders that stale
label. So the setting changes while the row text does not.

## Suggested upstream fix

Derive the label from the new key `s`, not from the old `binding.value`. For example:

```csharp
binding.value = uGUI.GetDisplayTextForBinding(GameInput.GetInputName(s));
```

so the visible row and the stored binding stay in sync within the same callback.

## Mod-side workaround (current)

`BetterSubnautica/Patches/ModKeybindOptionPatches.cs` postfixes `ModKeybindOption.AddToPanel`,
finds the bound `uGUI_Binding`, and wraps its `bindCallback` so that, after the original
callback runs, it regenerates the label from the new key string (`binding.value = s;
binding.RefreshValue();`). Once upstream derives the label from `s`, the extra assignment is a
no-op and the patch can be removed.
