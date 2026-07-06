# Below Zero "Mod Input" tab — design proposal

Status: **Option C IMPLEMENTED** (2026-07-08), live-verified on BZ.STABLE through the
BetterRemote bridge. The Subnautica (SN) "Mod Input" tab is rated "ottima" and the goal is
the equivalent experience on Below Zero (BZ); the user chose the autonomous path = Option C
(consolidated mod-side input page). Options A and B remain documented below as the escalation
path if native Keyboard-tab placement is later required.

## Implementation (Option C, delivered)

One consolidated, category-grouped "Better Subnautica - Input" page under `Options > Mods`
aggregates every BZ mod keybind, rebindable in place and persisted through each plugin's
existing Nautilus `ConfigFile` JSON pipeline. Pieces:

- **Central registry** — `BetterSubnautica/Utility/ModInputUtility.cs` gains a `#if BELOWZERO`
  section (mirroring the SN block in the same file): `RegisterKeybind(category, displayName,
  tooltip, config, get, set, onChanged)` collects keybind entries; `GetCategories()` returns
  them grouped, emitting the fixed order Better Lights / Better Quick Slots / Better Savegames
  / Better Vehicles / Better PDA first, then any extra category.
- **The page** — `BetterSubnautica/Options/ModInputMenu.cs` (`#if BELOWZERO`), a `ModOptions`
  subclass registered once from `BetterSubnautica/Plugin.cs` `Awake`. It reads the live
  registry inside `BuildModOptions` (invoked when the panel opens, so every plugin has fed its
  keybinds by then regardless of load order), emits `panel.AddHeading` per category and one
  `ModKeybindOption.Create` row per keybind, and its `OnChanged` handler writes the new key
  back through the entry's setter, invokes any per-entry `onChanged`, and saves the owning
  `ConfigFile`.
- **Each plugin feeds the registry** — each `Buttons.cs` gains a `#elif BELOWZERO` branch (new
  `BetterPDA/Buttons.cs`) whose `Register()` feeds its keybinds with SN-mirrored display names
  and tooltips; the plugins' `Awake` call `Buttons.Register()` on BZ too. BetterQuickSlots
  passes an `onChanged` that force-updates the quick slots, matching the old `[OnChange]`.
- **Scatter suppression** — each plugin `Settings` `[Keybind]` attribute is gated
  `#if SUBNAUTICA`, so the per-plugin BZ pages no longer render duplicate keybind rows (on SN
  `[Keybind]` was already a no-op, so SN is byte-identical). The plain `KeyCode` properties and
  their defaults stay, so gameplay `Input.GetKeyDown(...)` consumption is untouched.

Live verification (BZ.STABLE, 2026-07-08): the page renders exactly 25 keybind rows in one
contiguous block under the category headings, no per-plugin duplicates, the registry holds 5
categories, and the write-back path (property set + `ConfigFile.Save`) persists to the config
JSON on disk. Known ambiguities resolved faithfully to C: the page name is
"Better Subnautica - Input" (matching the existing "Better Subnautica - Debug" convention);
BetterLights rows carry per-vehicle display names because the shared `[Keybind]` label is
identical across vehicles; the keybind-only `Better Vehicles - Seatruck` config loads on BZ
without registering a per-plugin options page (both its keys moved to the Input page), so no
empty page renders under Options > Mods — `BetterVehicles/Plugin.cs` calls a plain
`new SeatruckSettings().Load()` on BZ instead of `RegisterModOptions`, and the consolidated
page's `entry.Config.Save()` still persists rebinds. Harness limit: the in-menu rebind KEY CAPTURE cannot be driven
through the bridge on BZ (it reads OS `Event.current`/OnGUI key events, which the in-process
virtual input does not inject), so the live rebind-and-persist confirmation exercises the
write-back path the capture triggers rather than the capture keystroke itself.

## Goal

On SN the mod registers 16 custom keybinds across 5 categories (the Better PDA eat/use button
joined in the 2026-07-10 feature-parity round) that appear as native rows in the game's own
**Keyboard** options tab, rebindable in place, seeded from config, persisted by the game.
Reproduce that experience on BZ.

## The structural constraint (source-verified)

BZ's input system cannot accept a new `GameInput.Button` the way SN's can. The two games
store bindings differently:

- **SN** backs bindings with a `Dictionary<GameInput.Button, InputAction>`, so Nautilus adds
  custom buttons by casting an unused int to the enum and inserting a dictionary entry
  (`Nautilus\Patchers\GameInputPatcher.cs`, `Nautilus\Handlers\Enums\Extensions\
  EnumExtensions_Button.cs` — both entirely `#if SUBNAUTICA`).
- **BZ** backs bindings with a fixed-size 3D array sized once at startup
  (`BelowZero\Assembly-CSharp\GameInput.cs`, `Initialize()`):

  ```csharp
  numButtons     = GetMaximumEnumValue(typeof(Button)) + 1;      // 45 real enum members
  buttonBindings = new Array3<int>(numDevices, numButtons, numBindingSets);
  ```

  `GetMaximumEnumValue` reads `Enum.GetValues(typeof(Button))`, which only returns the 45
  compile-time members — a synthetic `(Button)45` never appears, and using it would index
  `buttonBindings` out of bounds. The `inputs` side (raw `KeyCode` → `Input`) IS open (built
  from `Enum.GetValues(typeof(KeyCode))`), which is why BZ can freely rebind an EXISTING
  action to any key but cannot gain a brand-new action.

Consequence: the entire Nautilus SN custom-button API is `#if SUBNAUTICA`-gated and has no BZ
counterpart anywhere in the Nautilus checkout. A native BZ Keyboard-tab row for a mod action
requires patching the game's array allocation, not just calling an API.

## What already exists on BZ (and ships today)

BZ keybinds already work through a DIFFERENT, older mechanism that this repo uses live:

- Each plugin's `Settings.cs` exposes its keys as `[Keybind]` `KeyCode` fields (no `#if` in the
  settings files: on SN `[Keybind]` is a silent no-op used only to seed a default; on BZ it
  renders a real rebind row).
- Nautilus renders those rows via `ModKeybindOption` (`#if BELOWZERO`), inside the mod's OWN
  options page (`Options > Mods > <plugin>`), backed by BZ's legacy `KeyCode` `GameInput`.
- This repo already patches the row-label refresh bug there
  (`BetterSubnautica\Patches\ModKeybindOptionPatches.cs`, `#if BELOWZERO`, verified live
  2026-07-07).
- Gameplay reads the KeyCode directly, e.g. `Input.GetKeyDown(Core.HoverbikeSettings.
  LightsButtonToggle)`.

So on BZ the keybinds are already rebindable and persisted — the gap versus SN is purely
**presentation**: the rows are scattered across separate per-plugin "Mods" sub-pages instead of
one consolidated, categorized tab in the native place.

## Options

### Option A — native Keyboard-tab rows (mod-side patching)

Patch `GameInput.Initialize()` (publicized-private, Harmony transpile/prefix) to reserve N
extra array slots beyond the 45 enum members, postfix the private `uGUI_OptionsPanel.
AddBindings` to emit `AddBindingOption` rows for the reserved indices (the public
`uGUI_TabbedControlsPanel.AddBindingOption` + `uGUI_Bindings` already work off any array-safe
`Button` value — this repo already postfixes `AddBindingOption` in
`BetterQuickSlots\Patches\uGUIPatches.cs`), whitelist them in `IsBindable`, and add a BZ
save-serialization hook.

- **Pro**: pixel-for-pixel the SN experience — native tab, native place.
- **Con**: reimplements from scratch, un-upstreamed, the array-resize + serialization safety
  Nautilus already solved for SN's dictionary system, against BZ's fixed-array private
  internals. Real hazards: save/settings format mismatch, index drift across plugin load
  orders (SN already warns about "nameless buttons if a plugin loads before Nautilus"), and
  unknown consumers of `numButtons == 45`. Highest engineering + regression risk.

### Option B — upstream the custom-button API into Nautilus for BZ

The same engine work as Option A, but as a `GameInputPatcher_BelowZero` + BZ
`EnumExtensions_Button` in Nautilus, so `EnumHandler.AddEntry<GameInput.Button>` works
identically on both games (the mod-side code would then mirror SN almost 1:1).

- **Pro**: closes the gap in Nautilus itself; every mod benefits; mod-side code stays clean
  and symmetric with SN.
- **Con**: same technical risk as A, plus a Nautilus PR/review cycle; this repo would depend
  on an unreleased/forked Nautilus build until merged.

### Option C — consolidated mod-side "Input" page (reuse what ships)

Build ONE `[Menu]` options page that aggregates every plugin's `[Keybind]` fields, grouped by
category headers, still under `Options > Mods` and still backed by the proven
`ModKeybindOption` + `ConfigFile` JSON pipeline.

- **Pro**: near-zero risk — pure C#, no game-internals patching, no save-format work; reuses
  exactly the machinery already verified live; ships fast; one place to rebind all BZ keys.
- **Con**: does NOT match the SN placement — it stays a mod-settings page, not a native
  Keyboard-tab category list. Aggregating fields owned by different plugins into one page
  needs a small cross-plugin wiring design.

## Recommendation

Start with **Option C**. It delivers the substance the user liked (one categorized list of all
mod keybinds, rebindable and persisted) at near-zero risk, on machinery already proven on BZ,
and it is reversible. If, after seeing C in-game, the user specifically needs the rows in the
native Keyboard tab (SN's exact placement), escalate to **Option B** (upstream) over A — the
engine work is identical, and doing it in Nautilus avoids a private, un-upstreamed patch of the
game's binding array living in this repo forever.

Option A is the fallback only if an upstream path is unavailable and native placement is
mandatory.

## Decision needed from the user

1. Is native Keyboard-tab placement REQUIRED, or is a consolidated `Options > Mods` input page
   (Option C) acceptable? This is the pivot between a fast, safe change and a heavy engine
   patch.
2. If native placement is required: upstream to Nautilus (B) or keep it mod-local (A)? Is there
   appetite to run this repo on a Nautilus fork until a PR merges?

## Open questions (need a live/source check before A or B)

- BZ's binding save-serialization path (the analogue of SN's
  `GameInputSystem.SerializeSettings` transpiler) has not been traced — confirm it exists and
  is patchable before committing to A/B.
- Whether anything else in the BZ base depends on `numButtons`/`Button` being exactly 45
  (debug loops, console save-slot layout) in a way an array resize would break — only
  `GameInput.cs` was checked.
