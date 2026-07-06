# BetterGraphics — single-player feature verification

Graphics settings replacement: the mod owns resolution, fullscreen mode, vsync, frame queue,
framerate limit, anisotropic filtering and shadow settings through a Nautilus options pane,
suppresses the vanilla controls that would fight it, and re-applies everything whenever the
game serializes its settings. Verified live on both games from branch `sp-verify` (SN.STABLE
19 warnings / 0 errors, BZ.STABLE 31 warnings / 0 errors), through the BetterRemote bridge.
Old-feature baseline: `d51c6e9` (QModManager + SMLHelper); current tree: BepInEx + Nautilus.

## Plugin bootstrap

`Plugin.cs` (`[BepInPlugin]`, `Plugin : SubnauticaPlugin`) replaces the QMod `Core.cs` entry
point; `Using.cs` exposes the static `Core` instance unqualified in all patch files.
`[BepInDependency(BetterSubnautica.MyPluginInfo.PLUGIN_GUID)]` declares the dependency the old
`mod.json` carried in `VersionDependencies` (restored in this verification; BetterLights and
BetterMap already declare it). Verified: both games log `Plugin BetterGraphics v0.0.3.7 is
Awake!`, `[Settings] Found 9 options to add to the menu`, the full per-method patch list
(SN 8 methods, BZ 10 — `BasePatches.cs` and `SkyApplierPatches.cs` are `#if BELOWZERO`) and
zero errors.

## Settings (`Settings.cs`)

Nautilus options pane "Better Graphics", persisted at `BepInEx/config/BetterGraphics/
config.json`. Nine menu options plus two hidden fields:

- `ResolutionWidth`/`ResolutionHeight` (`[IgnoreMember]`, no UI): written by the
  `OnResolutionChanged` postfix, consumed by the `SerializeSettings` postfix.
- `FullScreenMode` (Choice: Exclusive FullScreen / FullScreen Window / Windowed): the ctor
  callbacks map the on-disk `Windowed` to `MaximizedWindow` in memory after load and back to
  `Windowed` while saving; `FullScreenModeEvent` applies `GetFixedFullScreenMode()` to
  `Screen.fullScreenMode`.
- `EnableVSync` (default `true`; `GetFixedVSyncCount()` = 1/0), `MaximumFrameCount` (slider
  1-4, default 2 → `QualitySettings.maxQueuedFrames`), `EnableFramerateLimit` (default
  `false`) + `FramerateLimit` (slider 1-500, default 60; `GetFixedFramerateCount()` = limit or
  -1), `AnisotropicFiltering` (default Force Enabled), `ShadowQuality`/`ShadowDistance`/
  `ShadowResolution` — each `OnChange` writes the matching `QualitySettings` member and fires
  `GraphicsUtility.OnQualityLevelChanged()`.

Verified: runtime `QualitySettings.*`, `Application.targetFrameRate` and `Screen.*` match the
config values on both games at the main menu. NEEDS-USER: changing the options from the
Nautilus Mods pane in-game (real pointer input for sliders/choices).

## Patches

- **`GameSettings.SerializeSettings` postfix** (both games; the method is static in the
  current games, so the postfix takes no parameters): re-applies resolution + fullscreen mode
  via `Screen.SetResolution` when drifted, then vsync, frame queue, target framerate,
  anisotropic filtering and the three shadow settings from `Core.Settings`, then
  `GraphicsUtility.OnQualityLevelChanged()`. Verified live on both games: with forced drift
  (`vSyncCount=0`, `maxQueuedFrames=4`, `targetFrameRate=77`, wrong fullscreen mode/
  resolution), closing the options panel (which runs `GameSettings.SaveAsync`) restores 1/2/-1
  and the configured resolution+mode.
  **Fixed — change notifications**: the debug-overlay messages compare the pre-apply values
  against the `Core.Settings` targets. Unity applies `Screen.SetResolution` at end of frame,
  so the original's same-frame re-read of `Screen.width/fullScreenMode` never saw the change
  and the notification never fired (verified live before the fix: correction applied, no
  overlay message, `DebuggerController` never instantiated). Verified after the fix:
  `FullScreen Mode: FullScreenWindow -> Windowed` (SN), `Resolution: 1280x720 -> 800x600`
  (BZ) appear in the overlay. The notification channel is `DebuggerUtility.ShowMessage` (the
  current `DebuggerUtility` API; the original's `ShowWarning` no longer exists).
- **`GraphicsUtil.SetVSyncEnabled` prefix** (both): returns `false` — the native vsync
  management is dead code while the mod owns `QualitySettings.vSyncCount`. Verified live on
  both games: invoking `SetVSyncEnabled(false)` leaves `vSyncCount` at 1.
- **`MainMenuController.Start` postfix** (both): applies `GetFixedFramerateCount()` at the
  main menu. Verified live: `Application.targetFrameRate == -1` (default settings) on both
  games.
- **`MainMenuVsync.ToggleVsync` prefix** (both): returns `false`. The component is
  scene-wired legacy UI with no active instance at either game's main menu — the patch is a
  dormant kill-switch, identical to the original's.
- **`uGUI_OptionsPanel.OnResolutionChanged` postfix** (both, param `applyIndex` in both
  games): stores `resolutions[applyIndex]` into the settings and saves immediately. Both games
  defer the actual window change to the Apply flow; the mod records the choice at selection
  time. Verified live on both games via direct invoke: `config.json` gains the selected
  resolution instantly, window untouched.
- **`uGUI_TabbedControlsPanel.AddToggleOption` postfix** (both, 5-param overload with
  `tooltip`): deactivates the vanilla `Fullscreen` and `Vsync` toggles on the General tab
  (`uGUIUtility.GeneralTabIndex` from BetterSubnautica). Verified live: 3 of the 5 built
  toggles active on the General pane of both games — `Fullscreen`/`Vsync` rows inactive.
- **`uGUI_TabbedControlsPanel.AddSliderOption` postfix** (both, 11-param overload with
  trailing `tooltip`): deactivates the vanilla `FPSCap` slider on the General tab. Verified
  live: SN 6 of 7 built sliders active, BZ 7 of 8 — the FPSCap row is the inactive one.
- **`uGUI_OptionsPanel.OnVSyncChanged` postfix** (both): keeps `targetFrameRateOption`
  inactive; vanilla re-shows it whenever vsync turns off. Verified live on both games:
  invoking `OnVSyncChanged(false)` leaves the FPSCap row hidden.
- **`Base.RebuildGeometry` postfix (BZ)**: on children whose name starts with
  `Large_Aquarium` and contains `glass` (the current mesh is
  `Large_Aquarium_generic_room_glass_01`; the original's exact `Large_Aquarium_02_glass` is
  extinct), lowers `_Color` alpha from the stock 0.314 to 0.2 and sets `_SpecInt=3`,
  `_Shininess=7`. Verified live 2026-07-05 on BZ during the porting campaign (same compiled
  source). The SP verification save has no Large Aquarium Room — rebuild re-check is in the
  NEEDS-USER list.
- **`SkyApplier.OnEnvironmentChanged` postfix (BZ)**: when the new environment is a `Base`
  with cell lighting at the applier's position, re-registers the applier via
  `BaseCellLighting.RegisterSkyApplier(applier, overrideInterior: true)`. Patch applied and
  targets verified (`Base.GetCellLightingFor`, `BaseCellLighting.RegisterSkyApplier` exist in
  the current game). Static base-interior appliers wear `Sky:BaseCell(Clone)` on the SP save
  (vanilla per-cell registration). The dynamic path fires only on a physical hatch entry
  (console `warp` into the hull does not cross the entry triggers) — NEEDS-USER. The in-code
  comment tolerates a savegame-load exception; no exception appears in the BepInEx log when
  the base streams in from ~52 m during save load.

## Dropped — `Base.UpdateSkyAppliers` prefix (BZ), with evidence

The original `BaseUpdateSkyAppliersPatch` (old `BasePatches.cs:8-25`) replaced
`Base.UpdateSkyAppliers` to send `SkyEnvironmentChanged` only to appliers inside base cells
(helper `BaseExtensions.GetCellIndex`, also removed). The method does not exist in current
BZ: `Base.BuildGeometryForCell` now calls `BaseCellLighting.UpdateSkyAppliers()` per rebuilt
cell (decompiled `Base.cs:5076`, `BaseCellLighting.cs:199`) — the game itself implements the
per-cell scoping the old prefix enforced. The surviving `SkyApplier.OnEnvironmentChanged`
postfix covers the environment-change path.

## NEEDS-USER checklist

- Change Better Graphics options from the Nautilus Mods pane in-game (sliders and choices need
  real pointer input); confirm `QualitySettings`/`Screen` follow each change (both games).
- Pick a resolution from Options → General on a real monitor and confirm it persists across a
  restart (the write path is verified; the end-to-end UI flow needs a real click).
- Enter the BZ base through a hatch and confirm carried/dropped items pick up the base cell
  lighting (dynamic `SkyApplier.OnEnvironmentChanged` path).
- Build or extend a BZ Large Aquarium Room and confirm the glass turns clearer (alpha 0.2)
  with `_SpecInt=3`/`_Shininess=7` after each geometry rebuild.
- Load a BZ save while standing inside a base and check the BepInEx log for the tolerated
  SkyApplier load-time exception (not reproducible when the base streams in from a distance).
- Watch base-interior lighting while adding/removing rooms in BZ (vanilla
  `BaseCellLighting.UpdateSkyAppliers` now owns what the dropped prefix did).
