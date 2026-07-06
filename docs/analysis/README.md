# BetterSubnautica — Technical analysis and porting status

*Snapshot: 2026-07-05 — `develop` working tree, game builds BZ 1.22.53872 (botbenson-patched)
and SN1 changeset 83031 (vanilla). Line references are valid for this snapshot.*

Code analysis starting from commit `06a354e "Updated projects for new versions"` (the merge-base
where the post-*modpocalypse* porting + multiplayer-integration work began) and, for the four
revived projects, from their last sources at `d51c6e9`. Covered games: **Below Zero** (BZ,
primary) and **Subnautica 1** (verified live since 2026-07-05). Remaining TODO: full Nitrox
support.

> Per-project documents in this folder:
> - [`core-and-multiplayer.md`](core-and-multiplayer.md) — BetterSubnautica (framework) + BetterNitrox
> - [`lights-and-map.md`](lights-and-map.md) — BetterLights + BetterMap (external mods)
> - [`hud-and-quickslots.md`](hud-and-quickslots.md) — BetterHUD + BetterQuickSlots
> - [`graphics-and-pda.md`](graphics-and-pda.md) — BetterGraphics + BetterPDA (revived)
> - [`savegames-and-vehicles.md`](savegames-and-vehicles.md) — BetterSavegames + BetterVehicles (revived)
> - [`porting-status.md`](porting-status.md) — consolidated: disabled features, risks, prioritized TODO

---

## 1. Context: the "modpocalypse"

With Subnautica's **"Living Large"** update, **QModManager was deprecated**: the whole mod
ecosystem migrated to **BepInEx + Nautilus** (the old SMLHelper was replaced by Nautilus). Most
mods broke and had to be rewritten on the new framework. The work from `06a354e` onward is exactly
that: **porting BetterSubnautica's features onto the new BepInEx/Nautilus stack and the updated
game**, plus adding:
- a **multiplayer bridge** to the "botbenson" mod (namespace `Subnautica.*`);
- **initial external-mod support** (e.g. the "SubnauticaMap" map mod).

Verified reference build: **Below Zero 1.22.53872**, Unity 2019.4.36, BepInEx 5.4.23.5,
Nautilus 1.0.0.51.

## 2. Solution structure

Eleven projects (`BetterSubnautica.sln`), all `net48`, with configurations
`SN.STABLE / SN.MULTI / BZ.STABLE / BZ.MULTI` (`Directory.Build.props`):

| Project | Role | Configurations built |
|---|---|---|
| **BetterSubnautica** | Framework/core: base plugin, patching, utilities, debugger | all 4 |
| **BetterNitrox** | Multiplayer bridge to botbenson | MULTI only |
| **BetterGraphics** | Graphics settings replacement (fullscreen/vsync/framerate/shadows) | all 4 |
| **BetterHUD** | Time/HUD display | all 4 |
| **BetterLights** | Configurable lights for vehicles/tools | all 4 |
| **BetterMap** | Map + external-mod integration + server save | all 4 |
| **BetterPDA** | PDA pause + eat/use key (BZ) | all 4 |
| **BetterQuickSlots** | Inventory quick slots | all 4 |
| **BetterRemote** | Dev-only HTTP instrumentation bridge for in-game verification | all 4 |
| **BetterSavegames** | Quicksave/autosave/quickload on dedicated slots | all 4 |
| **BetterVehicles** | Vehicle storage shortcuts, auto-repair, Seatruck/Cyclops helpers | all 4 |

Build/deploy: `dotnet build BetterSubnautica.sln -c BZ.MULTI` (.NET SDK 10). The `CopyToGameFolder`
target (`Directory.Build.targets`) auto-copies to
`…\SubnauticaZero\BepInEx\plugins\MatrixDJ96\<project>`.
References are resolved by `Directory.Build.targets` + `GameDir.targets` (game Managed +
`Dependencies\<Config>` + for BZ.MULTI the botbenson `Subnautica.*.dll` from `NitroxDir`).
`Assembly-CSharp[-firstpass]` are *publicized* (BepInEx.AssemblyPublicizer) to access the game's
private members.

## 3. Base framework (BetterSubnautica)

- `Plugins/SubnauticaPlugin.cs`: abstract base `SubnauticaPlugin : BaseUnityPlugin`. `Awake()`
  calls `ApplyPatches()` → `HarmonyUtility.PrePatchAll / PatchAll / PostPatchAll`. Each child
  plugin gets its own `Harmony` instance.
- `Attributes/PrePatchAttribute`, `PostPatchAttribute`: mark patch classes to apply in the *pre*
  or *post* phase relative to normal patching. Used to control ordering (critical for multiplayer,
  see §4).
- `MonoBehaviours/AbstractAwakeSingleton`, `AbstractSingletonContainer`: singleton pattern for
  controllers.
- `MonoBehaviours/Debug/*DebuggerController`: debug overlays for vehicles/tools (Cyclops, Exosuit,
  Flashlight, FlashlightHelmet, Hoverbike, MapRoomCamera, Seaglide, Seamoth, Seatruck, SubRoot).
  Simplified since the base commit (verbose properties → expression bodies), behavior preserved.
- `Utility/*` (`KeyCodeUtility`, `InventoryUtility`, …) and `Extensions/*` (`ComponentExtensions`).

## 4. botbenson multiplayer integration (verified at runtime)

How botbenson hooks in (details in `core-and-multiplayer.md`):

1. botbenson **overwrites** `SubnauticaZero_Data\Managed\Assembly-CSharp.dll` with its patched
   multiplayer build (identical hash to `…\.botbenson\…\Core\Assembly-CSharp.dll`). It does not go
   through BepInEx.
2. Its build patches `StartScreen`: `Awake()` calls `TryToShowDisclaimer()`, `Start()` calls
   `SubnauticaBootstrap.Load()` (self-bootstrap: loads `Subnautica.API` + `Subnautica.Loader` and
   invokes `Subnautica.Loader.Loader.Run()` → loads dependencies from `Game\Dependencies` and the
   `Subnautica.Client`/`Subnautica.Events` plugins from `Game\Plugins`, calling
   `Initialize()`/`OnEnabled()`).
3. **Anti-tamper**: `TryToShowDisclaimer()` recurses infinitely if it detects BepInEx's
   `winhttp.dll` next to a `.ex*` module → StackOverflow on the main thread → **black screen**.

Role of **BetterNitrox** (the bridge), all `[PrePatch]` applied BEFORE StartScreen runs:
- `StartScreenPatches`: Prefix on `StartScreen.TryToShowDisclaimer` → `return false` → neutralizes
  the anti-tamper (this is **the** black-screen fix).
- `ToolsPatches`: Prefix on `Subnautica.API.Features.Tools.IsBepinexInstalled` → `false` → botbenson
  does not refuse to start under BepInEx.
- `Plugin.cs` `LoadNitrox()`: loads loader+API via reflection, pre-runs `Loader.Run()` and sets
  `SubnauticaBootstrap.IsLoaded = true` (so StartScreen's `Load()` becomes a no-op).
- `LoggerPatches`: redirects botbenson logging (`Subnautica.API.Features.Log`) to the BepInEx log.
- `ZeroGamePatches`: patch on `ZeroGame.SetLightsActive` (multiplayer-side light hook forwarding
  light-state changes to BetterLights' `IToggleLightsController`).

Status: **the game reaches the menu** with botbenson + Vortex mods + BetterSubnautica, and the
**light-sync round-trip with a second live client was verified 2026-07-04**. Known residual (not
blocking): NetBird fails to load `wintun.dll` when the botbenson files are OneDrive cloud-only
placeholders (materialized). The Phase 3 full regression pass is pending (`porting-status.md`).

All BetterNitrox Harmony targets were verified against the current decompiled botbenson build — no
compatibility break. Future risk is only a botbenson-side version bump (these types are
botbenson-owned, not Unknown Worlds').

## 5. External-mod support

BetterMap already contains "mini-support" to integrate with the external map mod (SubnauticaMap)
and the multiplayer server (`Subnautica.Server`). Details in `lights-and-map.md`.

## 6. Porting status and per-mod verification

See [`porting-status.md`](porting-status.md) for the consolidated porting status (by-design
divergences, at-risk patches, and the prioritized TODO); the per-feature single-player
verification of every mod lives in `docs/mods/<mod>.md`.
Precise references live in the per-project docs.
