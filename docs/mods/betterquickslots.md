# BetterQuickSlots — feature verification

Extends the quick-slot bar of both games from the vanilla 5 slots up to 10, with per-slot key
remapping, a key label rendered above each icon (plus, on BZ, hidden redundant vanilla
binding rows) and a rewritten quick-slot tooltip range. The solution builds the project in
all four configurations (`.Build.0` for `SN.*` and `BZ.*`).

## Two input stacks, one feature set

The released SN1 (2.0 codebase) and BZ diverge on input:

- **BZ** keeps the legacy `GameInput` stack: a static `inputs` list of KeyCode-based entries,
  `SetBindingInternal`, `GetKeyCodeAsInputName`, and `Player.quickSlotButtonsCount`. The mod
  polls extra keys with legacy `Input.GetKeyDown` and writes slot bindings through the
  `SetBindingInternal` string overload.
- **SN1** rewrote input on the Unity InputSystem: `GameInput` is a static facade over an
  `IGameInput` implementation (`GameInputSystem` at runtime, created in `PlatformUtils`), which
  builds an `InputActionAsset` in code — one `InputAction` per `GameInput.Button` enum value,
  keyboard bindings as control paths (`"<Keyboard>/1"`). `GameInput.Button` stops at `Slot5`;
  the mod extends the enum with five custom buttons through Nautilus, and the game's own
  binding persistence covers them (the Nautilus `SerializeSettings` transpiler).

The SN1 branch of the mod rides that new stack end to end:

- **Slots 1-5**: the game's own `Slot1..Slot5` buttons and their native Input-tab rows own
  the keys — the mod neither seeds nor hides them; the config `Slot1..Slot5` values are
  BZ-only.
- **Slots 6-10**: custom `GameInput` buttons ("Better Quick Slots" category in the Mod Input
  tab, registered in `Buttons.cs` with defaults seeded from the config `KeyCode`s), read via
  `GameInput.GetButtonDown`/`GetButtonHeld`/`GetButtonUp` in a `uGUI_QuickSlots.HandleInput`
  postfix that mirrors the vanilla loop (`SlotKeyDown`/`SlotKeyHeld`/`SlotKeyUp`) and its
  gating (`Player.main.GetCanItemBeUsed()`, `!uGUI.isIntro`, `!IntroLifepodDirector.IsActive`).
- **Key labels and names**: each slot's button resolves its current primary keyboard binding
  through `ModInputUtility.GetButtonName` (vanilla buttons for 1-5, custom for 6-10), so the
  labels follow rebinds made in the game's panels — `QuickSlotsController` refreshes on
  `GameInput.OnBindingsChanged`. The colored form returns the game's TMP key-glyph sprites
  (the labels above the icons render as key caps), the plain form returns
  `InputControlPath.ToHumanReadableString` text that the tooltip range parser collapses.

## Plugin bootstrap

`Plugin.cs` (`[BepInPlugin]`, `Plugin : SubnauticaPlugin`) with
`[BepInDependency(BetterSubnautica.MyPluginInfo.PLUGIN_GUID)]`; `Using.cs` exposes the static
`Core` instance unqualified in the mod's files. On both games the log shows `Plugin
BetterQuickSlots v0.0.3.7 is Awake!` and every patch applied — 7 on SN, 8 on BZ: the
binding-write hook is `GameInputSystem::SetBinding` on SN and `GameInput::SetBindingInternal`
on BZ, the `AddBindingOption` row-hiding patch is BZ-only, and the other 6 patch targets are
name-identical on both games. Nautilus reports 13 options on SN and 3 on BZ (the BZ
`Slot1..Slot10` properties carry no `[Keybind]` attribute, see Settings).

## Settings (`Settings.cs`)

Nautilus options pane "Better Quick Slots" (Options → Mods), persisted at
`BepInEx/config/BetterQuickSlots/config.json`. Every option is wired to
`OnChange → QuickSlotsController.ForceUpdate = true`:

- `SlotCount` (Slider 5-10, step 1, default 10): number of quick slots.
- `Slot1`..`Slot10` (`KeyCode`, defaults `Alpha1`..`Alpha9`, `Alpha0`): per-slot toggle keys.
  `[Keybind]` options on SN only; on BZ `Buttons.cs` registers them through
  `ModInputUtility.RegisterKeybind` into the core's "Better Subnautica - Input" page, with the
  same `ForceUpdate` callback.
- `TextFontSize` (Slider 0-100, default 12) / `TextOffsetY` (Slider 0-100, default 8): key
  label appearance.

**Keybind rows.** The SN build of Nautilus pre.51 has no `ModKeybindOption` type: on SN the
pane shows the three sliders and logs `Failed to add ModKeybindOption …` for every `[Keybind]`
option — of this mod and of the sibling mods that keep SN `[Keybind]` options (BetterLights,
BetterPDA, BetterSavegames, BetterVehicles). On SN the keys are rebound in the game's Mod
Input tab instead: slots 6-10 are custom `GameInput.Button`s registered through
`ModInputUtility.RegisterButton` (`EnumHandler`, "Better Quick Slots" category) and seeded
from the config `KeyCode`s; slots 1-5 belong to the vanilla Input tab. On BZ the keybind rows
live on the core's consolidated "Better Subnautica - Input" page.

## MonoBehaviours — `QuickSlotsController`

Added to the `uGUI.main.quickSlots` GameObject by the `uGUI.Awake` postfix (`EnsureComponent`).
On `ForceUpdate` it resizes `QuickSlots.binding` preserving the existing items, sets
`slotCount`, re-runs `uGUI_QuickSlots.Init`, instantiates a TMP label per icon from
`HandReticle.main.compTextHand` (a `TextMeshProUGUI` on both games), then calls
`SlotsUtility.UpdateSlotBindings()` (BZ only) and `TooltipFactory.RefreshActionStrings()`.
Save data is size-safe in both directions: `QuickSlots.SaveBinding` writes one entry per
current slot and `RestoreBinding` clamps on `Mathf.Min(uids.Length, binding.Length)`.

## Patches and utility

- **Binding-write postfix** (`GameInputPatches.cs`): arms `ForceUpdate` on every binding
  write — SN patches `GameInputSystem.SetBinding` (the `IGameInput` implementation), BZ
  patches the int overload of `GameInput.SetBindingInternal` (the string overload funnels
  into it in the BZ tree).
- **`QuickSlots.Update/SelectInternal/DeselectInternal` transpilers**: remap the vanilla
  6-entry `QuickSlots.slotNames` reads to the 11-entry `SlotsUtility.SlotNames`, so equipment
  events from slots 7-10 stay in bounds. The three methods carry the same names in both game
  trees.
- **`uGUI_QuickSlots.HandleInput` postfix**: extra-slot key handling from
  `SlotsUtility.VanillaSlotCount` up to `SlotCount` — `GameInput.GetButtonDown/Held/Up` on the
  custom buttons on SN (mirroring the vanilla loop shape), legacy `Input.GetKeyDown/Up` on BZ
  (gated on `!WaitScreen.IsWaiting`; the SN branch gates on `!IntroLifepodDirector.IsActive`).
  The missing Held dispatch on BZ is a no-op: for
  extra-slot indices 5..9 no reachable `SlotKeyHeld` consumer exists — the player
  `QuickSlots.SlotKeyHeld` and `SeaTruckUpgrades.SlotKeyHeld` bodies are empty in the BZ tree,
  and `Exosuit.SlotKeyHeld` remaps `slotID += 2` against its 6-entry `slotIDs`, so every extra
  index lands out of range. `VanillaSlotCount` reads
  `uGUI_QuickSlots.quickSlotButtons.Length` on SN (the game has no
  `Player.quickSlotButtonsCount`) and `Player.quickSlotButtonsCount` on BZ.
- **`uGUI_TabbedControlsPanel.AddBindingOption` postfix (BZ, 5-param overload)**: hides the
  five vanilla `OptionSlot1..5` rows, matched by label and `uGUIUtility.KeyboardTabIndex`. The
  core `AddTab` postfix records the index for the tab labeled "Keyboard" (BZ) or "Input" (SN).
- **`TooltipFactory.RefreshActionStrings` postfix**: coroutine (waits on `icons` via
  `CoroutineUtility.WaitUntil`) that rebuilds `TooltipFactory.stringKeyRange15` from the
  configured keys, collapsing consecutive digits into ranges (`0-9` with the default 10 keys,
  `1-7` with 7 slots).
- **`SlotsUtility`**: builds the 11-entry `SlotNames` from the `SlotCount` slider's
  `SliderAttribute.Max`; on BZ it resolves per-slot keys by reflection and
  `UpdateSlotBindings` writes the first `VanillaSlotCount` keys into the game's own bindings
  via `KeyCodeUtility.SetKeyCode`. On SN the labels come from the buttons' current bindings
  (`ModInputUtility.GetButtonName`) and nothing is written back.
- **`KeyCodeUtility`** (core project, split per game): on SN converts `KeyCode` to InputSystem
  control paths (`TryGetKey` correction table + ignore-case `Enum.TryParse` into
  `UnityEngine.InputSystem.Key`, mouse buttons mapped to `<Mouse>/…`) and feeds
  `GameInput.SetBinding`/`GetDisplayText`; on BZ resolves names through `GameInput.inputs` and
  writes through `SetBindingInternal`.
