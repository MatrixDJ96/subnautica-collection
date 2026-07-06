# BetterSubnautica — single-player feature verification

Core plugin: shared infrastructure (BepInEx bootstrap, Harmony pre/post patching, utilities,
extensions) plus one user-visible surface, the on-screen debug overlay. Verified live on both
games from branch `sp-verify` (SN.STABLE 19 warnings / 0 errors, BZ.STABLE 31 warnings / 0
errors), through the BetterRemote bridge. Old-feature baseline: `d51c6e9` (QModManager +
SMLHelper); current tree: BepInEx + Nautilus.

## Plugin bootstrap

`Plugin.cs` (`[BepInPlugin]`, `Plugin : SubnauticaPlugin`) replaces the QMod `Core.cs` entry
point. `Plugins/SubnauticaPlugin.cs` resolves name/version/location, owns the `Harmony`
instance and the `ManualLogSource`, and applies patches in `Awake`. `Using.cs` exposes
`Core.*` unqualified via `global using static BetterSubnautica.Plugin`. The plugin version
comes from `[BepInPlugin]`/`MyPluginInfo` (0.0.3.7). Verified: both games log
`Plugin BetterSubnautica v0.0.3.7 is Awake!` with the full per-method `- Patched <method>`
list and zero errors.

`Utility/HarmonyUtility.cs` keeps the three-bucket ordering (`[PrePatch]` classes first, plain
classes, `[PostPatch]` classes last) and logs each patched method.

## Settings (`Settings.cs`)

Nautilus options pane "Better Subnautica - Debug", persisted at
`BepInEx/config/BetterSubnautica/config.json`. `ShowDebugInfo` (default `false`) master-gates
the overlay; per-vehicle toggles (default `true`): `CyclopsInfo`/`SeamothInfo` (SN),
`SeatruckInfo`/`HoverbikeInfo`/`FlashlightHelmetInfo` (BZ), `MapRoomCameraInfo`,
`SubRootInfo`, `ExosuitInfo`, `FlashlightInfo`, `SeaglideInfo` (both). Verified: editing the
JSON and restarting activates the overlay on both games. NEEDS-USER: toggling the checkboxes
from the Nautilus pane in-game (real pointer input).

## Debug overlay

`DebuggerController` (singleton GameObject) draws `Dict`+`List` messages via `OnGUI`, scales
the font to the resolution, and clears on the `Delete` key. Each vehicle/tool gets an
`AbstractDebuggerController<T>` subclass attached by an `Awake`/`Start` postfix patch; it
pushes `LightsStatus` (when `ShowLights`), `EnergyPerSecond` and `AvailableEnergy`
(`charge/capacity (percent%)`) lines, refreshed once per second, and removes them on
disable/destroy.

Live verification (message format `[BetterSubnautica] (<instanceId>) <Type>.<Key>: <value>`):

| Controller | Game | Verified live |
|---|---|---|
| CyclopsDebuggerController | SN | `True (Internal)` and `True (Internal, External)` on two Cyclops; 6000/6000 and 1200/1200 energy from the SubRoot `PowerRelay` |
| SeamothDebuggerController | SN | `False (None)`, 200/200 |
| SeatruckDebuggerController | BZ | two trucks with independent `False (None)` / `True (External)` states |
| HoverbikeDebuggerController | BZ | attaches; `0/0 (NaN%)` on a save hoverbike with no battery (same arithmetic as the original) |
| FlashlightHelmetDebuggerController | BZ | `False (None)`, 500/500 |
| ExosuitDebuggerController | both | `True (External)` on both games |
| SubRootDebuggerController | both | energy lines only — `ShowLights = false` hides the lights line on bases by design |
| MapRoomCameraDebuggerController | both | BZ `True (External)` 98,85/100; SN `False (None)` on a fresh camera |
| SeaglideDebuggerController | both | `False (None)`, 100/100 |
| FlashlightDebuggerController | both | `False (None)`, 100/100 |

Controllers attach immediately on freshly spawned entities and on save-loaded entities in
range. The `Delete`-key overlay clear and the Cyclops camera-light state are verified live on
SN1 via BetterRemote virtual input: a virtual Delete empties `DebuggerController.Text`, and
with the external cams active and the camera light on the Cyclops line reads
`LightsStatus: True (Internal, Camera)` — cycling the light off through virtual `LeftHand`
clicks drops the `(Camera)` entry (`True (Internal)`).

**Restored — Cyclops `LightsStatus`** (`CyclopsDebuggerController.cs`): the controller
inherits `SubRootDebuggerController`, whose `ShowLights = false` suppressed the Cyclops lights
line the original showed (the old class inherited `ShowLights = true` from the abstract
base). `CyclopsDebuggerController` re-overrides `ShowLights = true`; verified live above.

`SeatruckDebuggerController` carries no `Awake` main-segment guard: the attach patch
(`Patches/Debug/SeatruckPatches.cs`) already targets only `SeaTruckSegment.GetHead` — one
controller per truck head, confirmed live with two trucks.

## Patches

- **`FlashingLightsDisclaimerPatches` (both games) — restored feature.** The original
  `EarlyAccessDisclaimerPatches` postfixed `EarlyAccessDisclaimer.SetText` to brand the Below
  Zero early-access disclaimer ("Modded with BetterSubnautica v… / Created by MatrixDJ96" in
  random colors). `EarlyAccessDisclaimer` does not exist in the released game; the startup
  disclaimer is now `FlashingLightsDisclaimer` (photosensitivity warning, same `SetText` hook,
  shown once per launch from `StartScreen.Awake`). The patch appends the same branding below
  the warning text (append, so the health warning stays intact; the warning re-renders on
  language change and the postfix re-appends after each rewrite). Verified live: captured
  disclaimer text ends with `Modded with BetterSubnautica v0.0.3.7 … Created by MatrixDJ96`
  with per-launch random colors. `Extensions/IntExtensions.cs` (`int.ToHex()`) is restored as
  its color helper. SN1 ships the byte-identical `FlashingLightsDisclaimer` class (same
  `SetText`/`text` surface, shown from `StartScreen`), so the patch is ungated and brands
  both games — applied on SN with a clean boot log in the 2026-07-10 feature-parity round.
- **`WeatherManagerPatches` (BZ) — obsolete but harmless.** The prefix suppresses
  `WeatherManager.DebugPrintAll` log spam. In the released game the method is
  `[Conditional("UNITY_EDITOR")]` with zero call sites. Verified live: invoking
  `DebugPrintAll` on the real `WeatherManager.main` produces no timeline dump — the prefix
  works if anything ever calls it.
- **`ModKeybindOptionPatches` (BZ) — patch-a-mod fix on Nautilus.** The upstream
  `ModKeybindOption.AddToPanel` (pre.51) assigns a `bindCallback` that regenerates the row
  label from the stale `binding.value`, so a rebound row keeps showing the OLD key until the
  options menu reopens (the stored setting updates correctly). The postfix wraps the
  assigned callback: after the original runs, `binding.value = s` (the new key string) and
  `RefreshValue()` — the Nautilus `RefreshValue` prefix renders modded rows from
  `binding.value` raw, so the label updates immediately. Null-guarded with a log warning
  (no bound `uGUI_Binding` found → fix skipped); once upstream fixes the label the extra
  assignment is a no-op. Verified live 2026-07-07 (V→F7→V, label and `binding.value`
  updated per press, no menu reopen).
- **`uGUIPatches`** records options-menu tab indices into `uGUIUtility`. The
  `KeyboardTabIndex` case matches the tab labeled "Keyboard" (BZ) or "Input" (SN). Verified
  live: BZ General=0, Graphics=1, Keyboard=2, Accessibility=3; SN General=0, Graphics=1,
  Keyboard(Input)=2, Accessibility=3. `KeyboardTabIndex`'s only consumer is BetterQuickSlots
  (both games).
- **Debug attach patches** (`Patches/Debug/*.cs`): postfix on each vehicle/tool
  `Awake`/`Start`, verified via the controller table above.

## Utilities and extensions

- `InventoryUtility.GetEatUseItemAction` (split `#if SUBNAUTICA`/`#elif BELOWZERO`): the SN
  branch filters the native `Inventory.GetAllItemActions(item)` mask to `Eat | Use`,
  inheriting the game's own survival/oxygen/container gating; the BZ branch is rewritten on
  `GameModeManager.GetOption` (the original `GameModeUtils` API is gone from BZ) and adds
  vegetarian/hunger/thirst gating to the original oxygen(Bladderfish)/cold rules. Consumers
  are BetterPDA's tooltip and inventory-tab patches on both games — the BZ gating is
  exercised live in the BetterPDA verification, the SN branch in the feature-parity round.
- `KeyCodeUtility` (split `#if SUBNAUTICA`/`#elif BELOWZERO`): on SN `GetBindingPath` builds
  the InputSystem control path from a static KeyCode map — no `Keyboard.current`/
  `Mouse.current` dependency, so it also works at plugin Awake during button registration;
  BZ keeps `GetName`/`SetKeyCode` over legacy `GameInput.inputs` + `SetBindingInternal` for
  BetterQuickSlots.
- `ModInputUtility` (split `#if SUBNAUTICA` / `#if BELOWZERO`) — the Mod Input registration
  surface, one per game. **SN:** `RegisterButton` creates a custom `GameInput.Button` through
  Nautilus `EnumHandler` (`CreateInput` + `WithCategory` + `AvoidConflicts`, keyboard default
  seeded from the plugin's config `KeyCode` via `GetBindingPath`); `GetButtonName` renders a
  button's current primary keyboard binding for UI labels. The config `KeyCode` only seeds the
  default: rebinds made in the game's Mod Input tab persist in the game's own input settings
  (the Nautilus `SerializeSettings` transpiler includes custom buttons). Every Nautilus-
  consuming plugin declares `[BepInDependency(Nautilus.PluginInfo.PLUGIN_GUID)]` — the
  enum-register hooks live in Nautilus's own initializer, so a plugin loading before Nautilus
  registers nameless buttons (empty language keys, no name in the binding serialization).
  Verified live 2026-07-07 on SN (15 buttons / 4 categories at the time), extended to 16
  custom buttons across 5 categories on 2026-07-10 (`BetterPDAEatUse`, category "Better
  PDA"): labels resolved with the game in Italian (Nautilus English fallback), rebind +
  persistence across a game restart.
  **BZ:** BZ sizes its `GameInput` button array once at startup, so a mod action cannot become
  a native Keyboard-tab row; instead `RegisterKeybind(category, displayName, tooltip, config,
  get, set, onChanged)` collects each plugin's `[Keybind]` `KeyCode` into a central registry
  and `GetCategories()` returns them grouped in a fixed order. Each plugin's `Buttons.Register`
  (`#elif BELOWZERO`) feeds its keybinds with SN-mirrored display names/tooltips.
- `Options/ModInputMenu` (BZ only) — the consolidated "Better Subnautica - Input" page under
  `Options > Mods`, a `ModOptions` subclass registered once from `Plugin.cs` `Awake`. Its
  `BuildModOptions` reads the live `ModInputUtility` registry (populated by every plugin's
  `Awake`, so it is complete by the time the panel opens), emits a `panel.AddHeading` per
  category and one `ModKeybindOption.Create` row per keybind, and its `OnChanged` handler
  writes the new key through the entry setter, runs any per-entry `onChanged` (quick-slot force
  update), and saves the owning `ConfigFile`. The per-plugin `[Keybind]` attributes are gated
  `#if SUBNAUTICA` so BZ shows each keybind only once, on this page. Verified live 2026-07-08
  on BZ.STABLE: 25 rows across 5 categories in one page, no duplicates, rebind write-back
  persisted to the config JSON. The in-menu rebind key CAPTURE reads OS `Event.current` events
  the bridge cannot inject, so a real in-menu rebind keystroke stays a user check.
- `CoroutineUtility` exposes `WaitUntil(predicate, action)` (BetterQuickSlots is its
  consumer).
- `ComponentExtensions`: `GetToggleLights` searches self + children (covers the SeaMoth
  without the original hardcode); `GetLightsParent` adds the BZ seatruck floodlight branch;
  new `GetLightsInChildren<T>`. `GraphicsUtility`, `uGUIUtility`, energy-source adapters
  (`EnergyMixinSource`/`EnergyInterfaceSource`/`PowerRelaySource`), enums and marker
  attributes are unchanged from the original.
- New infrastructure with no runtime surface in this mod: `Components/IToggleLightsController`
  (consumed by BetterLights) and `Helpers/Timerwatch`.

## Dead code removed in the port (verified zero callers solution-wide)

`PatchClassProcessorExtensions` (folded into `HarmonyUtility`), `BaseExtensions.GetCellIndex`,
`DebuggerUtility.ShowWarning/WriteMessage/ClearMessages`, `DebuggerController.ShowWarning/
WriteMessage`, `ComponentExtensions.WriteComponents`, `StringExtensions.IsNullOrWhiteSpace`,
`CoroutineUtility.WaitForSeconds`, `KeyCodeUtility.GetKeyCode`, the QMod `mod.json` manifest
(superseded by `[BepInPlugin]`).

## NEEDS-USER checklist

- Toggle the "Better Subnautica - Debug" checkboxes from the Nautilus Mods pane (both games).
- ~~Press `Delete` in-game with the overlay populated~~ — VERIFIED on SN1 2026-07-06 via
  BetterRemote virtual input: overlay populated via `AddMessage`, a virtual Delete empties
  `DebuggerController.Text`. VERIFIED on BZ 2026-07-06 the same way: the persistent test
  message vanishes on a virtual Delete while the live per-frame debugger lines re-add
  themselves.
- ~~Cyclops external-camera light state shows `(Camera)` while piloting a camera with lights
  on (SN)~~ — VERIFIED on SN1 2026-07-06 via BetterRemote: `True (Internal, Camera)` with the
  camera light on, `(Camera)` gone after virtual `LeftHand` clicks cycle the light off.
- Eat/Use tooltip gating across game modes (BZ, via BetterPDA surfaces): vegetarian filters
  non-vegetarian food, hunger/thirst/oxygen/cold options gate the Eat action. Out of the
  bridge harness's reach: both consumers key off a PDA item hover, and the uGUI cursor
  position comes from `Input.mousePosition` (`FPSInputModule.GetCursorScreenPosition`), which
  the virtual-input patches do not cover — a real pointer is required. The game-option lever
  itself works over the bridge (dotted `/set` on
  `GameModeManager.gameOptionsManager.options`, exercised in the BetterLights BZ run).
- BZ disclaimer branding visual check on a real screen (text content verified via bridge).
