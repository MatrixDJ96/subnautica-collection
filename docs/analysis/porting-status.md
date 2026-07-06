# Porting status — disabled features, bugs, and prioritized TODO

*Snapshot: 2026-07-05 — `develop` working tree (revival not yet committed), game builds
BZ 1.22.53872 (botbenson-patched) and SN1 changeset 83031 (vanilla). Line references are valid
for this snapshot.*

Consolidated from the per-project analysis docs in this folder, cross-checked against the
decompiled trees in `D:\Projects\Subnautica\BelowZero\` and `D:\Projects\Subnautica\Subnautica\`.
Scope: both games, all four configurations. Base of comparison: commit `06a354e` for the seven
surviving projects, `d51c6e9` for the four revived ones.

## Current runtime status

- **All 11 projects are alive and build green** in the configurations they target
  (`-t:Rebuild`, warning signature SN.STABLE=19, SN.MULTI=21, BZ.STABLE=31, BZ.MULTI=33 —
  the AUDIT-STATE baseline plus 2 CS0436 per config for each revived project).
- **Porting campaign Phase 1 DONE** — `BetterGraphics`, `BetterPDA`, `BetterSavegames`,
  `BetterVehicles` revived from their `d51c6e9` sources, ported to the current game APIs and
  smoke-verified live on BZ.MULTI (per-project details: [`graphics-and-pda.md`](graphics-and-pda.md),
  [`savegames-and-vehicles.md`](savegames-and-vehicles.md)).
- **Porting campaign Phase 2 DONE** — SN1 verified live on SN.STABLE (changeset 83031,
  installed at `C:\Games\Steam\steamapps\common\Subnautica`): all 9 SN plugins load with full
  patch sets and zero errors; a real SN regression in BetterGraphics (`currentIndex` vs
  `applyIndex`) was found and fixed by collapsing the converged uGUI gates.
- **Multiplayer**: the game reaches the menu with botbenson + Vortex mods + the full plugin
  suite; the light-sync round-trip with a second client was verified 2026-07-04. The Phase 3
  regression pass (all plugins + machine identity + deferred in-world checks) is pending.
- Deployed state: BZ game dir carries the BZ.MULTI flavor, SN game dir the SN.MULTI flavor.

## Open bugs (verified against source)

- None currently known. The per-feature single-player verification (every feature of every
  mod, both games, with the restored items and the confirmed-legitimate drops) lives in
  `docs/mods/<mod>.md`.

## Divergences from the base commits (by design)

- **`AbstractLightsController` rewritten** — single `Timerwatch(1s)` polling in `Update()`,
  read-only properties, `GetSettings()`/`SetDefaults()` (details in `lights-and-map.md`).
- **BetterPDA manual pause retired** — released SN1 pauses the PDA natively via
  `MiscSettings.pdaPause` (identical to BZ), so the old `SUBNAUTICA_STABLE` freeze stack and
  its core helpers (`PDAUtility`, `FreezeTimeUtility`, `CoroutineUtility.WaitForMilliseconds`)
  are gone; `InventoryUtility` and `StringExtensions` remain in the core
  (`graphics-and-pda.md` §BetterPDA).
- **SN Seaglide patches retired** — released SN1 ships the light toggle, map toggle and
  localized tooltips natively; only the BZ `Seaglide.Start` map-default postfix remains
  (`savegames-and-vehicles.md` §BetterVehicles).
- **Debug controllers** simplified (verbose props → expression bodies) — behavior preserved.

## Conditional-compilation audit (2026-07-05)

Every remaining `#if SUBNAUTICA`/`#if BELOWZERO` gate in the seven pre-existing projects was
checked against both decompiled trees:

- **Collapsed (applied)**: `BetterQuickSlots/MonoBehaviours/QuickSlotsController.cs` — the
  `SUBNAUTICA_STABLE` branch used `HandReticle.interactPrimaryText` + `UnityEngine.UI.Text`,
  which no longer exist in SN1 (`compTextHand` is `TextMeshProUGUI` in both games). Dead
  branch removed; BZ builds byte-identical (the branch was never compiled in the BZ-only
  configs).
- **Collapsible at signature level, KEPT after the Phase-3 in-game check (2026-07-05)**:
  `BetterLights/Patches/VehiclePatches.cs` (`Vehicle.OnDockedChanged(bool, DockType)` is
  identical in both trees) and `BetterLights/Patches/SubRootPatches.cs`
  (`SubRoot.UpdateLighting/lightingState/lightControl` identical, private, publicized).
  Both patch bodies would be no-ops or duplicates on BZ: BZ seabases have
  `SubRoot.lightControl == null` (verified live on a player-built base), and BZ vehicle
  docking is covered by the BZ-gated `DockablePatches`. The gates stand as SEMANTIC (SN-only
  features).
- **REQUIRED** (APIs genuinely diverge): `KeyCodeUtility` (BZ `GameInput` bindings API),
  `InventoryUtility` (split since 2026-07-10: SN filters the native `GetAllItemActions`
  mask, BZ rebuilds the gating on `GameModeManager`/`TechTypeGroups`, which are BZ-only),
  `WeatherManagerPatches` (`DebugPrintAll` absent in SN), the `SeaTruckSegment`/`Dockable`
  extension files, `MapRoomCameraPatches` (`ControlCamera` signatures differ),
  `ExosuitPatches` (`SubConstructionComplete` declaring type differs),
  `MapRoomCameraToggleLightsController` (`active` vs `controllingPlayer`), and the whole
  BZ-only Seatruck/Hoverbike/FlashlightHelmet cluster.
- **SEMANTIC** (game-exclusive content, kept): the Cyclops and Seamoth clusters
  (SN1 gameplay content; BZ's `SeaMoth` type is vestigial).
- `BetterNitrox`/`BetterMap` `#if BELOWZERO_MULTI` layers cover the botbenson integration;
  the SN branch of `BetterNitrox/Plugin.cs` targets Nitrox, a different framework — not
  collapsible.

## Feature-parity round (2026-07-10)

Full SN↔BZ parity sweep: feature matrix built from the per-mod docs crossed with every
`#if` gate in the solution, each asymmetric cell classified (game-exclusive / by-design
split / portable-but-missing), and all three configurations exercised live over the bridge
(BZ.STABLE, BZ.MULTI single-client hosted, SN.STABLE) — every plugin loads, every patch set
applies, zero plugin errors on all three.

Portable gaps closed in the round:

- **BetterPDA eat/use key on SN** — `Buttons.cs` SN branch registers `BetterPDAEatUse`
  (16th custom button, 5th category "Better PDA"); `uGUIInventoryTabPatches` and
  `TooltipFactoryPatches` gained SN branches that reuse the native
  `GetAllItemActions`/`ExecuteItemAction` pipeline (detail in `docs/mods/betterpda.md`).
- **Disclaimer branding on SN** — `FlashingLightsDisclaimerPatches` is ungated:
  SN1 ships a byte-identical `FlashingLightsDisclaimer`, so the same postfix brands both
  games.
- **BZ helmet debug toggle fix** — `FlashlightHelmetDebuggerController` reads
  `FlashlightHelmetInfo` (it read `FlashlightInfo`, leaving the helmet debug option dead);
  verified live on BZ.STABLE and BZ.MULTI by flipping both settings over the bridge.

Asymmetries confirmed intentional: the game-exclusive vehicle clusters, the SN native Mod
Input tab vs the BZ consolidated Input page (equivalent by design), `GUIHandPatches` (SN
tool-use reroute; BZ reads the key directly), SN `VehiclePatches` ↔ BZ `DockablePatches`
(paired docking implementations), SN.MULTI Nitrox layers deferred as P2. The SN HUD-bar
`ignoreTimeScale` patches are gone (quality pass, 2026-07-10): `ignoreTimeScale` is
write-only against the current builds and the bars pause-behave natively (see
`betterpda.md`).

Verification environment facts (BZ): botbenson permanently replaces the game's
`Assembly-CSharp.dll`, and its anti-tamper walk crashes the boot under the BepInEx `winhttp`
doorstop unless BetterNitrox's `[PrePatch]` neutralizers are deployed — the MULTI-built
`BetterNitrox.dll` therefore stays in the plugins folder for STABLE rounds too (the game
does not boot without it). BepInEx pads short logger names with spaces
(`[Info   : BetterPDA]`): grep log sources with a padding-tolerant pattern.

## Compatibility notes

- **botbenson patches**: every BetterNitrox Harmony target matches the current decompiled
  botbenson build exactly. Future risk is only a botbenson version bump (botbenson-owned
  types).
- **SubnauticaMap** (external map mod): no decompiled sources available; BetterMap's references
  (`Controller.Run/ReloadMaps`, `Storage.slot/userStorage`, `Logger.Print`) were confirmed only
  via publicized-DLL string matching, not exact signatures. Verify when touching BetterMap.
- **SN1/BZ signature convergence**: the current SN1 build has adopted several BZ-era
  signatures (`uGUI_OptionsPanel.OnResolutionChanged(int applyIndex)`, `OnVSyncChanged` +
  `targetFrameRateOption` + FPSCap, `AddToggleOption`/`AddSliderOption` tooltip params,
  TMPro on `HandReticle`/`MainMenuLoadButton`, single `FreezeTime.Begin(Id)` overload).
  When porting old dual-path code, check convergence FIRST — the old SN branch may be dead.

## Prioritized TODO (implementation phase)

**P1 — multiplayer regression pass (separate campaign):**
- All plugins load under BZ.MULTI with a second client; machine-derived identity
  (`IdentityPatches.cs`) accepted live by `JoiningProcessor`; light-sync round-trip still
  green.

**P1 — real-input-only checks (consolidated NEEDS-USER lists in `docs/mods/<mod>.md`):**
- BetterPDA eat-use + PDA-open pause; BetterGraphics dynamic SkyApplier re-registration +
  aquarium glass (needs a Large Aquarium Room); BetterVehicles storage keybinds, Seatruck
  LeftControl enter/exit + detach, BZ Moonpool dock/repair/info; BetterQuickSlots extended-key
  selection and rebind; BetterSavegames F5/F9 keys; Cyclops camera damper mouse feel (SN).

**P2 — cross-target (later, per user):**
- Full Nitrox support — `BetterNitrox/Plugin.cs:11` TODO (`// TODO: Gestire Subnautica Nitrox`).
- Phase 4: total rehistory of `develop` from `d51c6e9` (gated — the user reviews and pushes).
  The `refactor` branch is a confirmed dead end: its `GameInputSystemPatches` targets a
  `GameInputSystem` type absent from the current game (verified twice, 2026-07-03 and
  2026-07-05); it stays as reference material until campaign end.
