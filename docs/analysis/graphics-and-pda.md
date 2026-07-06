# BetterGraphics and BetterPDA — technical analysis

*Snapshot: 2026-07-05 — `develop` working tree (revival not yet committed), game builds
BZ 1.22.53872 (botbenson-patched) and SN1 changeset 83031 (vanilla). Line references are valid
for this snapshot.*

Analysis of `BetterGraphics/` and `BetterPDA/`, two of the four projects revived from their last
sources at `d51c6e9` and ported to the current game versions. Cross-checked against BOTH
decompiled trees: Below Zero in `D:\Projects\Subnautica\BelowZero\` and Subnautica 1 in
`D:\Projects\Subnautica\Subnautica\` (one project folder per assembly). Every Harmony target,
field access and method call below was verified in the tree of the game(s) it compiles for.

---

## BetterGraphics

> **Status: builds in all four configurations; verified in-game on BZ.MULTI and SN.STABLE
> (2026-07-05).** BZ smoke: 10 patches applied, 9 Nautilus options in the Mods pane, runtime
> values `targetFrameRate=-1` / `vSyncCount=1` / `anisotropicFiltering=ForceEnable` prove the
> mod's overrides win over the vanilla settings pipeline. SN1 smoke: 8/8 patches applied (the
> two remaining patch files are `#if BELOWZERO`).

### 1. Purpose and architecture

- `BetterGraphics/Plugin.cs:8` — `Plugin : SubnauticaPlugin`, `[BepInPlugin]`. Registers
  `Settings` via `OptionsPanelHandler.RegisterModOptions<Settings>()` (line 10); `Core` set in
  `Awake()` (line 16).
- `BetterGraphics/Settings.cs:10` — `Settings : ConfigFile`, `[Menu("Better Graphics")]`.
  Options: `FullScreenMode` (choice, lines 32-33), `EnableVSync` (toggle, 35-36),
  `MaximumFrameCount` (slider 1-4, driver frame queue, 38-39), `EnableFramerateLimit` (toggle,
  41-42), `FramerateLimit` (slider 1-500, 44-45), `AnisotropicFiltering` (choice, 47-48),
  `ShadowQuality` (choice, 50-51), `ShadowDistance` (slider 0-200, 53-54), `ShadowResolution`
  (choice, 56-57). `[IgnoreMember] ResolutionWidth/Height` (27-30) are persisted without UI.
  Each `OnChange` event applies straight to Unity (`Screen`, `QualitySettings`,
  `Application.targetFrameRate`, lines 79-114). The `ConfigFile` callbacks
  `OnFinishedLoading`/`OnStartedSaving`/`OnFinishedSaving` (16-24) convert
  `MaximizedWindow` (real, borderless) ↔ `Windowed` (serialized) so the JSON stays valid for
  Unity's `FullScreenMode` while the game runs borderless.
- `BetterGraphics/Patches/GameSettingsPatches.cs:7-11` — postfix on the static
  `GameSettings.SerializeSettings` (no `__instance`); re-applies resolution, fullscreen mode,
  vsync/framerate, anisotropic filtering and shadow settings from `Core.Settings`, then
  `GraphicsUtility.OnQualityLevelChanged()` (line 30). The resolution/fullscreen change
  notifications compare the pre-call state against the `Core.Settings` targets (lines 34, 39) —
  Unity defers `Screen.SetResolution` to the next frame, so a same-frame `Screen.*` read cannot
  detect the change.
- `BetterGraphics/Patches/GraphicsUtilPatches.cs:9-12` — prefix on
  `GraphicsUtil.SetVSyncEnabled` returning `false`: the native vsync management is suppressed,
  the mod owns `QualitySettings.vSyncCount`.
- `BetterGraphics/Patches/MainMenuPatches.cs` — `MainMenuController.Start` postfix applies the
  framerate cap in the menu (7-14); `MainMenuVsync.ToggleVsync` prefix returns `false` (16-24),
  disabling the menu's own vsync toggle.
- `BetterGraphics/Patches/uGUIPatches.cs` — one code path for both games (no `#if`), 4 patches:
  - `uGUI_OptionsPanel.OnResolutionChanged` postfix (9-22): reads
    `__instance.resolutions[applyIndex]` and persists width/height.
  - `uGUI_TabbedControlsPanel.AddToggleOption` (5-param overload with trailing `string tooltip`)
    postfix (24-36): hides the vanilla `"Fullscreen"`/`"Vsync"` toggles on the General tab
    (`uGUIUtility.GeneralTabIndex`).
  - `uGUI_TabbedControlsPanel.AddSliderOption` (11-param overload, `SliderLabelMode` +
    `floatFormat` + trailing `tooltip`) postfix (38-49): hides the vanilla `"FPSCap"` slider.
  - `uGUI_OptionsPanel.OnVSyncChanged` postfix (51-59): hides
    `__instance.targetFrameRateOption`.
- `BetterGraphics/Patches/BasePatches.cs` (`#if BELOWZERO`) — `Base.RebuildGeometry` postfix
  (7-28): finds the large-aquarium glass meshes (name starts with `Large_Aquarium` and
  contains `glass`; the current game names them `Large_Aquarium_generic_room_glass_01`) and
  lowers the glass alpha from the stock 0.314 to 0.2 (`WithAlpha`, from the game's firstpass
  `SystemExtensions`, global namespace) while restoring `_SpecInt=3` / `_Shininess=7`
  (verified live on a Large Aquarium Room, 2026-07-05).
- `BetterGraphics/Patches/SkyApplierPatches.cs` (`#if BELOWZERO`) —
  `SkyApplier.OnEnvironmentChanged` postfix (7-19): when the new environment is a `Base`,
  re-registers the SkyApplier on the base's cell lighting
  (`Base.GetCellLightingFor(pos)` → `BaseCellLighting.RegisterSkyApplier(__instance, true)`).
  The in-code comment (line 13) documents a load-time exception that MUST NOT be "fixed" —
  suppressing it breaks the lighting refresh.
- `BetterGraphics/Using.cs:1` — `global using static BetterGraphics.Plugin` (alias `Core`).

### 2. Features/behavior

- Replaces the vanilla graphics panel: borderless-correct fullscreen modes, vsync + driver
  frame-queue depth, custom framerate cap, anisotropic filtering, shadow
  quality/distance/resolution — all live from the Nautilus "Better Graphics" options.
- Hides the redundant vanilla controls (Fullscreen/Vsync toggles, FPSCap slider, target
  framerate option) and re-applies the mod's values whenever the game serializes its settings.
- Below Zero only: clearer large-aquarium glass, and SkyApplier re-registration on base cell
  lighting when entering a base.

### 3. What changed in the revival (since `d51c6e9`)

The project was deleted by `06a354e` and revived from its `d51c6e9` sources; on top of the
QModManager→BepInEx scaffold (legacy `Core.cs`/`mod.json`/`AssemblyInfo` → `Plugin.cs` +
`SubnauticaPlugin` base + `Using.cs`, SDK-style csproj, solution GUID = the old assembly's COM
GUID `14657029…`), the porting deltas are:

- **`Base.UpdateSkyAppliers` prefix dropped** — the method no longer exists; the current games
  refresh base lighting natively per cell in `BaseCellLighting.UpdateSkyAppliers`. The
  remaining `SkyApplier.OnEnvironmentChanged` postfix covers the environment-change case.
- **`AddSliderOption` patch retargeted** to the current 11-param overload (BZ and SN1 both
  gained a trailing `string tooltip` param).
- **`GameSettings.SerializeSettings` is static** — the postfix takes no `__instance`.
- **`WithAlpha` resolves to the game's `SystemExtensions`** (firstpass, global namespace); the
  old mod-local helper is gone.
- **`uGUIPatches.cs` lost all its `#if` game gates** — the SN1 and BZ signatures have
  CONVERGED (`OnResolutionChanged(int applyIndex)`, `OnVSyncChanged`, `targetFrameRateOption`
  and the FPSCap slider now exist identically in both games). The old SN branch used a
  `currentIndex` parameter name that no longer matches SN1 (`applyIndex`); with named-argument
  patch binding a wrong name aborts the whole Harmony class, so the gate removal also fixed an
  SN1 regression (4/8 → 8/8 patches applied, verified live).
- The change notifications go through `DebuggerUtility.ShowMessage` (debug-overlay level);
  `DebuggerUtility.ShowWarning` is the always-visible HUD toast channel, reserved for real
  failures (e.g. BetterSavegames' slot errors).

### 4. Game-API notes (verified against both decompiled trees)

| Member | BZ | SN1 |
|---|---|---|
| `GameSettings.SerializeSettings(ISerializer)` | `GameSettings.cs:364` private static | `GameSettings.cs:348` private static |
| `GraphicsUtil.SetVSyncEnabled(bool)` | firstpass `GraphicsUtil.cs:95` | firstpass `GraphicsUtil.cs:95` |
| `MainMenuController.Start()` | `MainMenuController.cs:5` private | same, private |
| `MainMenuVsync.ToggleVsync(bool)` | `MainMenuVsync.cs:20` | `MainMenuVsync.cs:20` |
| `uGUI_OptionsPanel.OnResolutionChanged(int applyIndex)` | `uGUI_OptionsPanel.cs:465` private | `uGUI_OptionsPanel.cs:344` private |
| `uGUI_OptionsPanel.resolutions` | `:51` private | `:49` private |
| `uGUI_OptionsPanel.OnVSyncChanged(bool)` | `:526` private | `:399` private |
| `uGUI_OptionsPanel.targetFrameRateOption` | `:102` private | `:81` private |
| `AddToggleOption(int,string,bool,UnityAction<bool>,string)` | `uGUI_TabbedControlsPanel.cs:261` | `:214` |
| `AddSliderOption(…,SliderLabelMode,string,string)` | `:281` | `:221` |
| `Base.RebuildGeometry()` | `Base.cs:5740` | BZ-gated |
| `Base.GetCellLightingFor(Vector3)` | `Base.cs:3305` | BZ-gated |
| `BaseCellLighting.RegisterSkyApplier(SkyApplier,bool)` | `BaseCellLighting.cs:210` | BZ-gated |
| `SkyApplier.OnEnvironmentChanged(GameObject)` | `SkyApplier.cs:218` | BZ-gated |
| `SystemExtensions.WithAlpha(Color,float)` | firstpass `SystemExtensions.cs:1409` | firstpass `SystemExtensions.cs:1409` |

All the private members compile thanks to the publicizer on `Assembly-CSharp[-firstpass]`.

---

## BetterPDA

> **Status: builds in all four configurations; verified in-game on BOTH games — patch sets
> SN 5 / BZ 4, the vanilla `PDAPause` toggle suppressed on the Accessibility pane and the
> stored flag driven in both directions (full evidence in `docs/mods/betterpda.md`).** The
> deep eat-use flow needs real hover input (synthetic input is banned in this environment) —
> see the NEEDS-USER list in that doc. botbenson blocks non-blacklisted `FreezeTime.Begin` in
> MP (a `Subnautica.Events` prefix), which does not affect this plugin: it performs no manual
> freeze.

### 1. Purpose and architecture

Released SN1 (the 2.0 codebase) ships the same native `MiscSettings.pdaPause` stack as Below
Zero — the stored flag, the Accessibility toggle, and `PDA.ManagedUpdate` recomputing
`FreezeTime.Set(Id.PDA, …)` every frame the PDA is active — so every configuration takes the
native path: the mod drives the flag and suppresses the redundant vanilla toggle.

- `BetterPDA/Plugin.cs` — `Plugin : SubnauticaPlugin` with
  `[BepInDependency(BetterSubnautica.MyPluginInfo.PLUGIN_GUID)]`, registers `Settings`.
- `BetterPDA/Settings.cs:8` — `[Menu("Better PDA")]`: `EnablePDAPause` (toggle, lines 10-11),
  `EatUse` keybind, default `KeyCode.Mouse2` (13-14).
- `BetterPDA/Patches/GameSettingsPatches.cs` — `GameSettings.SerializeSettings` prefix
  (unconditional, both games): copies `Core.Settings.EnablePDAPause` into the game's native
  `MiscSettings.pdaPause`.
- `BetterPDA/Patches/TooltipFactoryPatches.cs` (an `#if SUBNAUTICA` and an `#elif BELOWZERO`
  branch) — `TooltipFactory.ItemActions` postfix: if the hovered item has no native Eat action
  (`Inventory.main.GetAllItemActions`), computes `InventoryUtility.GetEatUseItemAction(item)`
  and writes Eat/Use tooltip lines bound to the configured key (`TooltipFactory.WriteAction` +
  `GetUseActionString`); otherwise rewrites the native lines adding the key binding next to
  `stringButton0` (32-47).
- `BetterPDA/Patches/uGUIInventoryTabPatches.cs` (an `#if SUBNAUTICA` and an `#elif BELOWZERO`
  branch) — `uGUI_InventoryTab.OnUpdate` postfix: with the PDA open and an
  `ItemDragManager.hoveredItem`, pressing the `EatUse` key eats/uses the item (SN through the
  native `Inventory.ExecuteItemAction`, BZ via `Survival` + remove/destroy directly).
- `BetterPDA/Patches/uGUIPatches.cs` — a prefix on `uGUI_TabbedControlsPanel.AddToggleOption`
  (both games) suppressing the vanilla `"PDAPause"` toggle on the Accessibility tab (the mod's
  own toggle drives `MiscSettings.pdaPause` instead). The HUD bars need no patch: they run on
  `PDA.deltaTime` (unscaled while the pause holds) natively, and `CoroutineTween.ignoreTimeScale`
  is write-only in both game builds.
- Core helper restored with the revival: `BetterSubnautica/Utility/InventoryUtility.cs`
  (`GetEatUseItemAction`, an `#if SUBNAUTICA` and an `#elif BELOWZERO` branch).

### 2. Features/behavior

- Pauses the game while the PDA is open by driving the game's native `MiscSettings.pdaPause`
  on both games, suppressing the redundant vanilla toggle; the HUD bars animate natively on
  unscaled PDA time while the pause holds.
- Both games: a configurable key (default middle mouse) eats/uses the item hovered in the
  inventory, with the item tooltips extended to show the Eat/Use action and its binding.

### 3. What changed in the revival (since `d51c6e9`)

- **The manual SN pause stack is retired** — the old `d51c6e9` branch targeted the 2021
  legacy-stable build (string-id `FreezeTime.Begin("PDAPause", …)`, no native pause).
  Released SN1 pauses natively, and `PDA.ManagedUpdate` recomputes `FreezeTime.Set(Id.PDA, …)`
  every frame the PDA is active, cancelling any external freeze on the next frame — a manual
  freeze cannot coexist with the native stack. The BZ path (drive the flag + suppress the
  vanilla toggle) covers both games; `PDAUtility`, `FreezeTimeUtility` and
  `CoroutineUtility.WaitForMilliseconds` have no callers and are absent from the core.
- **`InventoryUtility.GetEatUseItemAction` rewritten on `GameModeManager`/`GameOption`** —
  `GameModeUtils`/`GameModeOption` no longer exist in BZ. The gates mirror the current
  `Inventory.GetAllItemActions`: Hunger/Thirst food-water values, OxygenDepletes +
  OrganicOxygenSources for Bladderfish, BodyTemperatureDecreases for cold value,
  VegetarianDiet + `TechTypeGroups` non-vegetarian guard.

### 4. Game-API notes (verified against both decompiled trees)

BZ branch (`BelowZero\Assembly-CSharp\`): `MiscSettings.pdaPause` (`MiscSettings.cs:33`,
public static), `TooltipFactory.ItemActions` (`TooltipFactory.cs:534`, private static, as are
`WriteAction:667`, `GetUseActionString:676`, `stringButton0/stringEat/stringUse`),
`Inventory.GetAllItemActions` (`Inventory.cs:378`), `uGUI_InventoryTab.OnUpdate`
(`uGUI_InventoryTab.cs:193`), `ItemDragManager.hoveredItem` (`ItemDragManager.cs:38`),
`GameModeManager.GetOption<T>(GameOption)` (`GameModeManager.cs:101`) with the `GameOption`
enum members the utility reads, `Eatable.GetFoodValue/GetWaterValue/coldMeterValue`
(`Eatable.cs:80/95/33`), `TechTypeGroups.IsTechTypeInGroup` + `TechTypeGroup.NonVegetarian`.

SN1 branch (`Subnautica\Assembly-CSharp\`): `MiscSettings.pdaPause` (`MiscSettings.cs:33`,
public static), the vanilla Accessibility toggle registration (`uGUI_OptionsPanel.cs:838`),
the store (`GameSettings.cs:408`), and the per-frame application in `PDA.ManagedUpdate`
(`PDA.cs:138-145`).

Shared (firstpass, identical in both trees): `UWE.FreezeTime` with the `Id`-based API — the
native `pdaPause` stack drives `FreezeTime.Set(Id.PDA, …)` from `PDA.ManagedUpdate`.

All private members are covered by the publicizer.

---

## Common section

### Solution registration

| Project | GUID | `.Build.0` |
|---|---|---|
| BetterGraphics | `14657029-7FC6-4C1C-947A-CA603ADE0A5E` | 4/4 configurations |
| BetterPDA | `F6845D88-8025-43B1-A6A9-538571012666` | 4/4 configurations |

Both GUIDs are the projects' original assembly COM GUIDs from their `d51c6e9`
`AssemblyInfo.cs`, preserving identity across the revival. Both projects build in all four
configurations; the game split is entirely `#if`-driven (BetterGraphics: 2 BZ-only patch
files; BetterPDA: the `SUBNAUTICA_STABLE` axis described above).
