# BetterPDA — single-player feature verification

Pauses the game while the PDA is open (a "PDA Pause" toggle in the mod's options pane, on both
games), and on both games binds a configurable eat/use key (default middle mouse) that
eats/uses the item hovered in the inventory, with the item tooltips extended to show that
binding. Verified live on both games from branch `sp-verify` (SN.STABLE 19 warnings / 0
errors, BZ.STABLE 31 warnings / 0 errors), through the BetterRemote bridge with screenshot
evidence; the SN side of the eat/use key landed in the 2026-07-10 feature-parity round.
Old-feature baseline: `d51c6e9` (QModManager + SMLHelper); current tree: BepInEx + Nautilus.

## Plugin bootstrap

`Plugin.cs` (`[BepInPlugin]`, `Plugin : SubnauticaPlugin`) replaces the QMod `Core.cs` entry
point; `Using.cs` exposes the static `Core` instance unqualified in the mod's files.
`[BepInDependency(BetterSubnautica.MyPluginInfo.PLUGIN_GUID)]` declares the dependency the old
`mod.json` carried in `VersionDependencies` (restored in this verification, matching
BetterGraphics/BetterHUD/BetterLights/BetterMap). Verified: both games log `Plugin BetterPDA
v0.0.3.7 is Awake!`, `[Settings] Found 2 options to add to the menu`, BetterSubnautica loads
first, zero errors. Patch sets, identical on both games (4):
`GameSettings::SerializeSettings` + `uGUI_TabbedControlsPanel::AddToggleOption` +
`TooltipFactory::ItemActions` + `uGUI_InventoryTab::OnUpdate`.

## Settings (`Settings.cs`)

Nautilus options pane "Better PDA", persisted at `BepInEx/config/BetterPDA/config.json`. Two
options, identical to the old SMLHelper set:

- `EnablePDAPause` (Toggle, default `false`): drives the game's native PDA pause.
- `EatUse` (Keybind, default `MouseButtonMiddle`): the eat/use key, consumed on both games.
  On SN1 `Buttons.cs` registers it as the custom `GameInput.Button` `BetterPDAEatUse`
  (category "Better PDA" in the native Mod Input tab, the config `KeyCode` seeding the
  default binding); on BZ it feeds the consolidated "Better Subnautica - Input" page through
  `ModInputUtility.RegisterKeybind`. The toggle renders under Options → Mods on both games.

## PDA pause — one native path for both games

Both games implement PDA pause natively: `MiscSettings.pdaPause` (SN1 `MiscSettings.cs:33`, BZ
`MiscSettings.cs:33`), serialized as `Misc/PDAPause` (SN1 `GameSettings.cs:408`, BZ
`GameSettings.cs:470`), exposed as a vanilla "PDAPause" toggle on the Accessibility tab (SN1
`uGUI_OptionsPanel.cs:838`, BZ `uGUI_OptionsPanel.cs:1286`), and applied per frame by
`PDA.ManagedUpdate` — `FreezeTime.Set(FreezeTime.Id.PDA, flag ? sequence.t : 0f)` plus the
player-animator unscaled-time switch (SN1 `PDA.cs:138-145`, BZ equivalent at `PDA.cs:148`).

The mod owns that flag on both games:

- **`GameSettingsPatches.cs`** — `GameSettings.SerializeSettings` prefix copies
  `Core.Settings.EnablePDAPause` into `MiscSettings.pdaPause` on every settings
  serialization (settings load at boot, save on options-panel close). Verified live on BOTH
  games: with the mod toggle on, forcing `MiscSettings.pdaPause=false` in memory and closing
  the options panel snaps it back to `true`; with the mod toggle off (BZ), the same cycle
  drives it to `false` and persists it.
- **`uGUIPatches.cs` — `uGUI_TabbedControlsPanel.AddToggleOption` prefix** suppresses the
  vanilla `"PDAPause"` row on the Accessibility tab (`uGUIUtility.AccessibilityTabIndex`,
  captured by BetterSubnautica's `AddTab` postfix; index 3 on both games), so the mod's
  toggle is the single owner. Verified live with before/after screenshots on both games: the
  Accessibility pane shows UIScale + Flashes (SN1) / UIScale + Flashes + Highlight
  Interactions (BZ) and the "Pausa PDA" row is gone.
The HUD bars need no patch on either game: `CoroutineTween.ignoreTimeScale` is write-only in
the current builds of BOTH games (`CoroutineTween.MoveNext` advances solely through
`deltaTimeProvider`), the bars' fill and punch run on `PDA.deltaTime` (fed from
`Time.unscaledDeltaTime` while pdaPause holds the freeze), and only the decorative pulse loop
rides scaled `Time.deltaTime`.

### Retired: the manual SN freeze branch

The old `#if SUBNAUTICA_STABLE` branch implemented the pause by hand — `PDA.Activated/Close`
postfixes around `FreezeTime.Begin/End(Id.PDA)` with a 500 ms delay coroutine,
`Survival.Eat/Use` unfreeze/refreeze, an `IngameMenu.QuitGame` safety unfreeze, and the
string-id `FreezeTime` overload — because the 2021 SN1 stable build had no native PDA pause.
The released SN1 (2.0 codebase, the old experimental line) has the full native stack above,
and its `PDA.ManagedUpdate` recomputes `FreezeTime.Set(Id.PDA, …)` **every frame the PDA is
active**: with the native flag off, any externally begun `Id.PDA` freeze is cancelled on the
next frame, so the manual branch was not only redundant but non-functional (its freeze could
survive at most one frame). The old two-argument `FreezeTime.Begin(string, bool)` is also gone
from the game — `UWE.FreezeTime` (firstpass, `FreezeTime.cs:252/257`) exposes `Begin(Id)`/
`End(Id)` only. Removed with the branch, as now-dead code with zero callers repo-wide
(develop's MULTI code included): `PDAPatches.cs`, `SurvivalPatches.cs`,
`IngameMenuQuitGamePatches.cs`, `BetterSubnautica/Utility/PDAUtility.cs`,
`BetterSubnautica/Utility/FreezeTimeUtility.cs`, and
`CoroutineUtility.WaitForMilliseconds` (`WaitUntil` stays — BetterQuickSlots uses it).

## Eat/use key — both games

Verified against the decompiled trees; both patch files carry an `#if SUBNAUTICA` and a
`#elif BELOWZERO` branch:

- **`TooltipFactoryPatches.cs`** — `TooltipFactory.ItemActions` postfix (SN
  `TooltipFactory.cs:474`, BZ `:534`; same `WriteAction`/`GetUseActionString` helpers on
  both): when `InventoryUtility.GetEatUseItemAction` resolves an action, any native Eat/Use
  action line gets the configured key appended to its LMB binding (`stringButton0` →
  `LMB/<key>`, one uniform line); only an item with no native Eat/Use line gets dedicated
  `WriteAction` lines bound to the configured key. With no resolved action the tooltip stays
  untouched, so the key is never advertised where the press would be a no-op. The key name
  comes from `ModInputUtility.GetButtonName(Buttons.EatUse)` on SN and
  `KeyCodeUtility.GetName(Core.Settings.EatUse)` on BZ.
- **`uGUIInventoryTabPatches.cs`** — `uGUI_InventoryTab.OnUpdate` postfix (SN
  `uGUI_InventoryTab.cs:152`, BZ `:193`): with the PDA open and an
  `ItemDragManager.hoveredItem`, the key press eats/uses the item. SN reads
  `GameInput.GetButtonDown(Buttons.EatUse)` and delegates to the native
  `Inventory.ExecuteItemAction(action, item)` (same `Survival.Eat/Use` + remove + destroy
  sequence, Use winning over Eat like the vanilla left-click); BZ reads
  `Input.GetKeyDown(Core.Settings.EatUse)` and performs `Survival.Eat/Use` + remove +
  destroy directly.
- **`InventoryUtility.GetEatUseItemAction`** (BetterSubnautica) — SN branch: filters the
  native `Inventory.GetAllItemActions(item)` mask to `Eat | Use`, inheriting the game's own
  survival/oxygen/container gating (`Inventory.cs:350`). BZ branch: mirrors the current
  `Inventory.GetAllItemActions` gates on `GameModeManager.GetOption`
  (`GameModeManager.cs:101`): Hunger/Thirst food-water values, OxygenDepletes +
  OrganicOxygenSources for Bladderfish, BodyTemperatureDecreases for cold value,
  VegetarianDiet + `TechTypeGroups` non-vegetarian guard; FirstAidKit and
  WaterPurificationTablet map to Use. The old `GameModeUtils`/`GameModeOption` API is gone
  from BZ.
- **`KeyCodeUtility`** (BetterSubnautica, `#if BELOWZERO`) renders the binding name for the
  BZ tooltips via `GameInput.inputs` + `uGUI.GetDisplayTextForBinding`.

All four patches (two per game) apply cleanly (Harmony log, feature-parity round 2026-07-10;
on SN the `BetterPDAEatUse` button resolves to its seeded "Middle Button" binding). The
in-flow press needs real pointer hover — NEEDS-USER on SN, verified 2026-07-07/08 on BZ.

## Config end-state

Both games run the DEFAULTS: the user reset every `Better*` config (files deleted
2026-07-11), so the mod regenerates `config.json` with `EnablePDAPause: false` and
`EatUse: MouseButtonMiddle` at first boot. Turning the pause on is a user choice from
Options > Mods (with the mod owning the flag, the game's own `Misc/PDAPause` store follows
on the next settings save).

## Restored / dropped

- **RESTORED — `[BepInDependency]` on `Plugin.cs`**: the BetterSubnautica dependency from the
  old `mod.json`.
- **RETIRED — manual SN pause branch** (see above): the PDA-pause *feature* survives on both
  games through the native `MiscSettings.pdaPause`; only the obsolete hand-rolled
  implementation is gone.
- **RETIRED — SN HUD-bar tween postfixes** (code-quality pass, 2026-07-10): the three
  `uGUI_FoodBar`/`uGUI_HealthBar`/`uGUI_WaterBar.Awake` postfixes wrote the write-only
  `ignoreTimeScale` flag and changed no behavior (see "PDA pause" above for the native
  timing).
- Everything else is byte-identical to the old sources.

## NEEDS-USER checklist

- ~~SN1, PDA Pause on: open the PDA with Tab~~ — VERIFIED 2026-07-06 via BetterRemote
  virtual input (branch `remote-input`): with `MiscSettings.pdaPause` on, a virtual Tab opens
  the PDA and `FreezeTime.HasFreezers()` turns true; closing releases the freezers; with the
  toggle off the PDA opens with no freeze. The HUD-bar pulse remains a visual-only check.
- ~~BZ, in the inventory: hover food and a FirstAidKit, consume with the configured key,
  and confirm no Eat line in Creative~~ — VERIFIED 2026-07-07 live (NutrientBlock +
  FirstAidKit, Hunger/Thirst flipped live over the bridge): Eat/Use action text renders with
  the configured key, consumption works, and Creative shows no action line. The round also
  produced the tooltip-uniformity rework above (merge into the native line); the merged
  single-line shape is VERIFIED live 2026-07-08 on both NutrientBlock and FirstAidKit.
- Both games: flip "PDA Pause" from the Mods pane with real input and confirm it persists to
  `BepInEx/config/BetterPDA/config.json` and to the game's own `Misc/PDAPause` store after
  closing the options.
- SN1, in the inventory: hover food and a FirstAidKit and consume them with the configured
  eat/use key (default middle mouse) — the mechanism (patches, button registration, native
  action gating) is bridge-verified; only the hover+press flow needs a human pointer.

## Fold-time notes (develop)

- `docs/analysis/graphics-and-pda.md` §BetterPDA (develop-only) describes the
  `SUBNAUTICA_STABLE` manual-pause branch, `PDAUtility`, `FreezeTimeUtility` and
  `CoroutineUtility.WaitForMilliseconds` as current code — stale against `sp-verify` after
  the retirement. SN1's own native pdaPause stack (verified at the lines cited above) makes
  the manual branch obsolete on every configuration, SN.STABLE included.
