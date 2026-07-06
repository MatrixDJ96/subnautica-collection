# BetterHUD — single-player feature verification

An always-visible HUD clock: the in-game time of day, rendered as an IMGUI label over every
screen (HUD, PDA, pause menu), with position, size and style owned by a Nautilus options pane.
Verified live on both games from branch `sp-verify` (SN.STABLE 19 warnings / 0 errors,
BZ.STABLE 31 warnings / 0 errors), through the BetterRemote bridge with screenshot evidence.
Old-feature baseline: `d51c6e9` (QModManager + SMLHelper); current tree: BepInEx + Nautilus.

## Plugin bootstrap

`Plugin.cs` (`[BepInPlugin]`, `Plugin : SubnauticaPlugin`) replaces the QMod `Core.cs` entry
point; `Using.cs` exposes the static `Core` instance unqualified in the mod's files.
`[BepInDependency(BetterSubnautica.MyPluginInfo.PLUGIN_GUID)]` declares the dependency the old
`mod.json` carried in `VersionDependencies` (restored in this verification, matching
BetterGraphics/BetterLights/BetterMap). Verified: both games log `Plugin BetterHUD v0.0.3.7 is
Awake!`, `[Settings] Found 5 options to add to the menu`, `Patched DMD<Player::Awake>`,
BetterSubnautica loads first, zero errors.

## Settings (`Settings.cs`)

Nautilus options pane "Better HUD" (Options → Mods, tab index 6 on both games), persisted at
`BepInEx/config/BetterHUD/config.json`. Five options, identical to the old SMLHelper set:

- `ShowHUDClock` (Toggle, default `true`): master switch for the clock label.
- `TimeFontSize` (Slider 0-100, default 32).
- `TimeFontStyle` (Choice: Normal / Bold / Italic / Bold & Italic, default Normal; the index
  casts straight to `UnityEngine.FontStyle`).
- `TimePositionX` (Slider 0-512, default 10) / `TimePositionY` (Slider 0-256, default 10):
  screen-space label origin in pixels.

Verified live: the pane shows all five controls with the values loaded from `config.json`
(SN at defaults; BZ with 64 / Bold / 300 / 100 — the pane and the rendered label both follow).
`ShowHUDClock=false` suppresses the label while the controller stays alive (`started=true`,
nothing drawn — SN screenshot). The four appearance settings apply together on BZ: the clock
renders bold at 64 px anchored at (300,100).

## MonoBehaviours — `TimeDisplayController`

Added to the `Player` GameObject; owns the whole clock feature.

- **Time text**: `DayNightCycle.main.GetDayScalar()` scaled to a 24 h day, formatted `hh\:mm`.
  Verified exact on BZ (`GetDayScalar()=0.4112942` → label `09:52`) and within one in-game
  minute on SN (bridge calls are sequential; one in-game minute passes in ~0.83 s of real
  time).
- **Style/Position getters re-read `Core.Settings` on every access**, so an options-pane change
  applies on the next frame with no restart. Verified via the config path: `Position` returns
  the configured (300,100) and the label renders with the configured font size/style.
- **Async start gate**: the controller draws nothing until `LightmappedPrefabs`,
  `PAXTerrainController`, `uGUI`, `HandReticle` and `DayNightCycle` exist and
  `WaitScreen.IsWaiting` is false. `WaitScreen.IsWaiting` (public static, `WaitScreen.cs:80`
  in both games) carries the job of the removed `PAXTerrainController.isWorking` and
  `uGUI.isLoading`; the extra `DayNightCycle.main` guard protects the `Text` getter it feeds.
  Verified: `started=true` after each save load, no early draw, no exceptions.
- **Font source is `HandReticle.main.compTextHand.font.sourceFontFile` on both games**. The
  old SN-only `interactPrimaryText` branch is gone with the field itself: current SN1 uses the
  same `TextMeshProUGUI compTextHand` as BZ (`HandReticle.cs:41` in both decompiled trees), so
  one unconditional expression replaces the old `#if SUBNAUTICA_STABLE` split.
- **`OnGUI` draws a white, overflow-clipped `GUI.Label`** whenever `ShowHUDClock && started`.
  The label renders above every uGUI surface, including the in-game menu and options panel
  (screenshots on both games).

## Patches (`PlayerPatches.cs`)

- **`Player.Awake` postfix** (both games): `EnsureComponent<TimeDisplayController>()` — the
  same add-once semantics the old `GetComponent == null ? AddComponent` pair spelled out.
  Verified: the component answers on the `Player` object on both games after save load.

## Restored / dropped

- **RESTORED — `[BepInDependency]` on `Plugin.cs`**: the BetterSubnautica dependency from the
  old `mod.json`.
- **Dropped: nothing.** Every old feature, setting and behavior survives in the current tree.

## NEEDS-USER checklist

- Move the Better HUD sliders and cycle the font-style choice from the Mods pane in-game (real
  pointer input): confirm the label restyles on the next frame and the values persist to
  `config.json` after closing the panel. The bindings and the render path are verified via the
  config file; only the pointer-driven flow itself remains.
