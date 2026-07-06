# BetterLights & BetterMap — Analysis

*Snapshot: 2026-07-03 — `develop`, game build BZ 1.22.53872, botbenson build of the same date.
Line references are valid for this snapshot.*

Analysis of the `BetterLights` (vehicle/tool light system) and `BetterMap` (map + external-mod
integration + server-side saving) modules of the BetterSubnautica BepInEx/Nautilus mod. Read-only
study; no code was modified.

## Build matrix and conditional symbols

Both projects inherit `Directory.Build.props` and `Directory.Build.targets` from the solution
root, which MSBuild imports on its own. The four build configurations and their
`DefineConstants` come from `Directory.Build.props`:

| Configuration | Symbols defined                          | Game       | Multiplayer |
|---------------|------------------------------------------|------------|-------------|
| `SN.STABLE`   | `SUBNAUTICA;STABLE;SUBNAUTICA_STABLE`    | Subnautica | no          |
| `SN.MULTI`    | `SUBNAUTICA;MULTI;SUBNAUTICA_MULTI`      | Subnautica | yes         |
| `BZ.STABLE`   | `BELOWZERO;STABLE;BELOWZERO_STABLE`      | Below Zero | no          |
| `BZ.MULTI`    | `BELOWZERO;MULTI;BELOWZERO_MULTI`        | Below Zero | yes         |

In source the guards used are `SUBNAUTICA`, `BELOWZERO`, `MULTI`, `STABLE` and the combined
`BELOWZERO_MULTI`. The `MULTI` builds add a dependency on `BetterNitrox`
(`BetterLights/Plugin.cs:10-12`, `BetterMap/Plugin.cs:14-16`). Current mod version is `0.0.3.7`
(`Directory.Build.props:31`).

---

# BetterLights

## 1. Purpose and architecture

BetterLights lets the player tune the lights of vehicles and tools (color, intensity offset, range
offset), toggle exterior lights with energy consumption, and control volumetric light cones. It is
built on **three independent MonoBehaviour hierarchies**, each following an
`Abstract<T> + interface` template-method pattern, all attached as **separate co-located components**
on the same vehicle GameObject by the Harmony patches. They are not nested; they discover each other
only through `GetComponent<IXxxController>()` / `GetComponentInParent<IXxxController>()` in the
patches.

### 1a. Lights hierarchy (`ILightsController` / `AbstractLightsController<T>`)

- `ILightsController` (`MonoBehaviours/Lights/ILightsController.cs:5-16`): read-only contract —
  `Light[] Lights`, `Color`, `IntensityOffset`, `RangeOffset`, plus `UpdateColor/Intensity/Range`.
- `AbstractLightsController<T>` (`MonoBehaviours/Lights/AbstractLightsController.cs:8-135`), generic
  over `T : Component` (the host vehicle/tool). Key mechanics:
  - `Component` is a lazy-cached accessor (`AbstractLightsController.cs:12-23`).
  - `Awake()` (`:41-50`) destroys the behaviour if the host component is missing, else calls the
    virtual `GetLights()` (`:79-85`, uses `Component.GetLightsParent()` + `GetLightsInChildren()`).
  - `Start()` (`:52-61`) destroys the behaviour when `Lights.Length == 0`, else `SetDefaults()`
    (`:89-101`) snapshots default color/intensity/range per light.
  - `Update()` (`:63-77`) is throttled by a `Timerwatch Timer` of 1 second (`:39`); on each tick it
    reads `GetSettings()` (abstract, `:87`) then applies `UpdateColor/Intensity/Range`
    (`:103-134`), writing to each light only when the actual value diverges from the target.
  - Concrete controllers implement only `GetSettings()` (reading from the `Settings/*` classes).

Concrete controllers (`MonoBehaviours/Lights/`):

| Controller                        | Host `T`             | Guard          | Instantiating patch                                                 |
|-----------------------------------|----------------------|----------------|---------------------------------------------------------------------|
| `CyclopsLightsController`         | `SubRoot`            | `#if SUBNAUTICA` | `CyclopsPatches.cs:18` (`SubRoot.Awake`) — active |
| `CyclopsCameraLightsController`   | `CyclopsExternalCams`| `#if SUBNAUTICA` | `CyclopsCameraPatches.cs:14` (`CyclopsExternalCams.Start`) — active |
| `ExosuitLightsController`         | `Exosuit`            | none           | `ExosuitPatches.cs:8-9` (`Exosuit.Awake`) — active                   |
| `FlashlightLightsController`      | `FlashLight`         | none           | `FlashlightPatches.cs:7-8` (`PlayerTool.Awake`, filtered `is FlashLight`) |
| `FlashlightHelmetLightsController`| `FlashlightHelmet`   | `#if BELOWZERO`| `FlashlightHelmetPatches.cs:8-9` (`FlashlightHelmet.Awake`) — active |
| `HoverbikeLightsController`       | `Hoverbike`          | `#if BELOWZERO`| `HoverbikePatches.cs:9-10` (`Hoverbike.Awake`) — active             |
| `MapRoomCameraLightsController`   | `MapRoomCamera`      | none           | `MapRoomCameraPatches.cs:8-9` (`MapRoomCamera.Start`) — active       |
| `SeaglideLightsController`        | `Seaglide`           | none           | `SeaglidePatches.cs:7-8` (`PlayerTool.Awake`, filtered `is Seaglide`)|
| `SeamothLightsController`         | `SeaMoth`            | `#if SUBNAUTICA` | `SeamothPatches.cs:9-10` (`SeaMoth.Start`) — active               |
| `SeatruckLightsController`        | `SeaTruckSegment`    | `#if BELOWZERO`| `SeatruckPatches.cs:11-12` (`SeaTruckSegment.Start`) — active       |

The SN/BZ split matches the games' vehicle rosters: Seamoth and Cyclops are Subnautica-only
(`#if SUBNAUTICA`), Seatruck, Hoverbike and FlashlightHelmet are Below-Zero-only (`#if BELOWZERO`).
The same gating governs which `Settings/*` are registered in `BetterLights/Plugin.cs:15-33`.

### 1b. ToggleLights hierarchy (`IToggleLightsController` / `AbstractToggleLightsController<T>`)

Parallel and independent of the Lights hierarchy. It manages on/off switching of exterior/flood
lights with energy draw, on/off FMOD sounds, and the vanilla `global::ToggleLights` component state.

- `IToggleLightsController` (`BetterSubnautica/Components/IToggleLightsController.cs` — in the
  core project, same home as `IEnergySource`, so that BetterNitrox's `ZeroGamePatches` can
  reference it without a circular project dependency): `LightsActive`, `SetLightsActive`,
  `ToggleLightsActive`, `IsPowered`.
- `AbstractToggleLightsController<T>` (`MonoBehaviours/ToggleLights/AbstractToggleLightsController.cs`)
  wires up the vanilla `ToggleLights` (`:71-96`), the lights parent (`:98-121`) and an `IEnergySource`
  (`:123-141`); zeroes the vanilla `energyPerSecond` (`:84`) and takes over energy consumption in
  `UpdateLightsEnergy()` (`:159-169`). `CanToggleLightsActive()` reads a configurable keybind and
  guards against PDA-in-use / frozen time (`:171-174`); `SetLightsActive()` plays the appropriate
  FMOD sounds and forces the vanilla component and lights parent to match (`:176-216`).

### 1c. VolumetricLights hierarchy (`IVolumetricLightsController` / `AbstractVolumetricLightsController<T>`)

Also parallel. It controls the intensity offset of volumetric light cones (`VFXVolumetricLight[]`).

- `AbstractVolumetricLightsController<T>`
  (`MonoBehaviours/VolumetricLights/AbstractVolumetricLightsController.cs:8-81`) refreshes in
  `LateUpdate()` via the abstract `UpdateSettings()` (`:67-70,80`) and registers each managed
  `VFXVolumetricLight` into the global singleton dictionary `VolumetricLightsContainer`
  (`MonoBehaviours/VolumetricLightsContainer.cs`), keyed by `GetInstanceID()`. The Harmony patch on
  `VFXVolumetricLight.UpdateMaterial` (`Patches/VFXVolumetricLightPatches.cs:8-23`) uses that
  dictionary to redirect the vanilla call to the correct controller.
- Exosuit and MapRoomCamera have no native volumetric cones, so their controllers clone the data
  from the Seamoth via an async coroutine (`ExosuitVolumetricLightsController.cs:20-60`,
  `MapRoomCameraVolumetricLightsController.cs:20-60`).

`SeatruckLightsContainer` (`MonoBehaviours/SeatruckLightsContainer.cs`, `#if BELOWZERO`) is a second
singleton dictionary (`int → IToggleLightsController`) used to route `SeaTruckLights.Update` through
the mod controller (Prefix in `SeatruckPatches.cs:53-71`).

### 1d. Complete `[HarmonyPatch(typeof(...))]` target inventory

- `CyclopsCameraPatches.cs`: `CyclopsExternalCams` ×3 (`:8,21,42`)
- `CyclopsPatches.cs`: `SubRoot` (`:9`), `CyclopsLightingPanel` ×2 (`:35,48`)
- `DockablePatches.cs`: `Dockable` ×2 (`:7,20`)
- `ExosuitPatches.cs`: `Exosuit` ×4 (`:8,32,50,66`), `Vehicle` in the `#else` branch (`:35`)
- `FlashlightHelmetPatches.cs`: `FlashlightHelmet` (`:8`)
- `FlashlightPatches.cs`: `PlayerTool` (`:7`)
- `HoverbikePatches.cs`: `Hoverbike` ×3 (`:9,33,49`)
- `MapRoomCameraPatches.cs`: `MapRoomCamera` ×3 (`:8,31,56`)
- `SeaglidePatches.cs`: `PlayerTool` (`:7`)
- `SeamothPatches.cs`: `SeaMoth` ×2 (`:9,32`)
- `SeatruckPatches.cs`: `SeaTruckSegment` ×2 (`:11,37`), `SeaTruckLights` (`:53`)
- `SubRootPatches.cs`: `SubRoot` (`:6`)
- `ToggleLightsPatches.cs`: `global::ToggleLights` (`:6`)
- `VFXConstructingPatches.cs`: `VFXConstructing` (`:8`)
- `VFXVolumetricLightPatches.cs`: `VFXVolumetricLight` ×2 (`:8,25`)
- `VehiclePatches.cs`: `Vehicle` (`:7`)

## 2. Features / behaviours

- Automatic tuning of light intensity/range relative to the game's defaults, with per-vehicle
  configurable offsets (`AbstractLightsController.cs:114-134`).
- Configurable light color via `[ColorPicker]` for Exosuit, Flashlight, FlashlightHelmet, Hoverbike,
  MapRoomCamera, Seaglide, Seamoth, Seatruck (e.g. `Settings/ExosuitSettings.cs:15-16`).
- Configurable energy consumption for switched-on lights (`[Slider("Lights Consumption"…)]` in each
  Settings class; Cyclops splits it into interior/exterior/camera, `Settings/CyclopsSettings.cs:12-19`).
- Key-toggled exterior lights with energy and on/off sounds
  (`AbstractToggleLightsController.cs:176-216`). Attachment is active for every host —
  Seatruck, Hoverbike, Seaglide, Flashlight, FlashlightHelmet, Exosuit, MapRoomCamera, plus
  Cyclops and Seamoth on SN (verified in-game on both games, `docs/mods/betterlights.md`).
  Exosuit & MapRoomCamera take their on/off sounds from the Seamoth prefab via
  `CraftData.GetPrefabForTechTypeAsync(TechType.Seamoth)`, which resolves on BZ too (verified
  live: the controllers attach and run with zero exceptions).
- Volumetric light cones with a separate intensity offset, routed through `VolumetricLightsContainer`
  + the `VFXVolumetricLight` patches (`Patches/VFXVolumetricLightPatches.cs:8-41`). Attachment
  is active for Seatruck, Hoverbike, Exosuit and MapRoomCamera (verified in-game: Hoverbike
  binds 1 `VFXVolumetricLight`; the Seatruck BZ prefab ships an empty `dimFloodlightsOnEnter`,
  so its controller is a harmless no-op; Exosuit & MapRoomCamera clone their cone data from the
  Seamoth prefab, which resolves on both games).
- Dynamic recreation of `VFXVolumetricLight` for Exosuit/MapRoomCamera by cloning Seamoth data
  (`ExosuitVolumetricLightsController.cs:20-60`, `MapRoomCameraVolumetricLightsController.cs:20-60`).
- Disable/restore volumetric cones when the player enters/exits or pilots the vehicle
  (`ExosuitPatches.cs:56-63,72-77`; Hoverbike `HoverbikePatches.cs:39-46,55-61`; MapRoomCamera
  `MapRoomCameraPatches.cs:46-52,67-73`; Seatruck `SeatruckPatches.cs:43-50`).
- Light handling on docking/undocking (`DockablePatches.cs:11-17` BZ, `VehiclePatches.cs:11-20` SN)
  with an `EnableLightsOnUndocking` option (`Settings/VehiclesSettings.cs:11-12`).
- Exosuit-specific volumetric-cone scale fix via manual trigonometry
  (`VFXVolumetricLightPatches.cs:29-39`).
- Cyclops external-camera handling: camera light color halved when light state is 2
  (`CyclopsCameraPatches.cs:25-39`); state preserved across camera changes (`:42-55`); volumetric
  cones disabled when exterior floodlights are on and the player is aboard (`CyclopsPatches.cs:48-63`);
  lighting panel forced on construction complete (`:35-46`); `SubRoot.UpdateLighting` fix for an
  unpiloted Cyclops (`SubRootPatches.cs:6-21`, SN only).
- Generic `ToggleLights.SetLightsActive` patch (`ToggleLightsPatches.cs:6-18`) that, in `STABLE`
  builds, blocks the vanilla execution when an `IToggleLightsController` exists in the parent, so the
  mod controller is not overwritten.

## 3. Changes since commit `06a354e`

`git diff 06a354e -- BetterLights`: **25 files changed, none added/removed/renamed** (161
insertions, 217 deletions).

### 3a. Controller attachment (all hosts active)

- `Patches/CyclopsPatches.cs:18-22` — `CyclopsAwakePatch.Postfix` (`SubRoot.Awake`) attaches
  `CyclopsLightsController`, `CyclopsToggleLightsController` and
  `CyclopsVolumetricLightsController` via `EnsureComponent`.
- `Patches/CyclopsCameraPatches.cs:14` — `CyclopsExternalCamsStartPatch.Postfix`
  (`CyclopsExternalCams.Start`) attaches `CyclopsCameraLightsController` the same way.
- `MonoBehaviours/ToggleLights/AbstractToggleLightsController.cs:102-107` —
  `InitializeLightsParent` resolves the parent via `component.GetLightsParent()` first and
  falls back to `toggleLights.lightsParent` when the extension finds none, so toggle
  controllers work on hosts without a vanilla `ToggleLights` component.

### 3b. `AbstractLightsController.cs` rewrite

A design-pattern shift from manual per-property dirty-tracking to unified timer-based polling:

- The old version kept `lastColorUpdate/lastIntensityUpdate/lastRangeUpdate` floats compared against
  `Time.time + UpdateInterval` (public, default 60s), each invalidated by a public setter on `Color`,
  `IntensityOffset`, `RangeOffset`. The new version uses a single `Timerwatch Timer` of 1s in
  `Update()` (`AbstractLightsController.cs:39,63-77`) and re-reads `GetSettings()` every tick instead
  of reacting to setter writes.
- `Color`/`IntensityOffset`/`RangeOffset` become read-only auto-properties (`{ get; protected set; }`,
  `:25-31`); `ILightsController` correspondingly loses its public setters and the `UpdateInterval`
  member.
- Light collection moves out of `Awake()` into the virtual `GetLights()` (`:79-85`, using the
  `GetLightsInChildren()` extension), so concrete controllers can override it.
- The abstract method is renamed `UpdateSettings()` → `GetSettings()`, and its call moves from a
  now-removed `LateUpdate()` into the `Timer`-guarded `Update()`.
- New private `SetDefaults()` (`:89-101`) also snapshots `DefaultColors[]` (the old code stored only
  start intensities and ranges).
- `UpdateRange()` keeps the volumetric cones in scale: when a light's range changes, the
  registered cone's `volumetricLight.UpdateScale()` is called
  (`AbstractLightsController.cs:124-139`).

On the Cyclops, `CyclopsLightsController.GetLights()` (`CyclopsLightsController.cs:9-25`)
collects the floodlight set from `CyclopsLightingPanel.floodlightsHolder`, and
`CyclopsCameraLightsController.GetLights()` (`CyclopsCameraLightsController.cs:8`) resolves the
camera light; the camera controller's `UpdateColor()` is a deliberate no-op (`:16`) — the
camera color is owned by the half-color light-state logic in `CyclopsCameraPatches`.
`SeatruckLightsController` overrides only `GetSettings()` and relies on the base `GetLights()` path
(`GetLightsParent()`, whose BZ branch resolves `seatruckLights.floodLight`); it finds its lights and
works in-game (see `porting-status.md`).

### 3c. Other notable changes

- `Patches/ExosuitPatches.cs`: `ExosuitSubConstructionCompletePatch` gains a conditional target —
  `#if BELOWZERO` patches `Exosuit.SubConstructionComplete`, the `#else` branch patches
  `Vehicle.SubConstructionComplete` (`:32-35`).
- `Patches/MapRoomCameraPatches.cs`: the `Postfix` signature of `MapRoomCameraControlCameraPatch`
  becomes conditional (`Player player, MapRoomScreen screen` for BZ vs `MapRoomScreen screen` for
  SN), and `MapRoomCameraFreeCameraPatch` is simplified from a redundant ternary to
  `dockingPoint == null && LightsActive`.
- New `[ColorPicker("Lights Color")]` added to the Exosuit/FlashlightHelmet/Flashlight/Hoverbike/
  MapRoomCamera/Seaglide/Seamoth/Seatruck settings (the "configurable light color" feature). No files
  were added, removed, or renamed.

## 4. Compatibility with the updated game

- All patched types exist in the decompiled Below Zero assembly at
  `D:\Projects\Subnautica\BelowZero\Assembly-CSharp\`, including the Cyclops types
  (`CyclopsExternalCams.cs`, `CyclopsLightingPanel.cs`, `SubRoot.cs` with `public bool isCyclops;` at
  `SubRoot.cs:184`, and `TechType.Cyclops = 2003`). The Cyclops classes survive in BZ as inherited
  dead code from the shared Subnautica-1 fork.
- Consequence for the "Cyclops does not exist in Below Zero" hypothesis: it is imprecise as the
  reason for the disabling. The Cyclops sources are already `#if SUBNAUTICA`-gated
  (`CyclopsPatches.cs:1`, `CyclopsCameraPatches.cs:1`, the four Cyclops controllers, and
  `Settings/CyclopsSettings.cs:1`), so they never compile for BZ regardless. The commented
  `AddComponent` calls sit inside the `#if SUBNAUTICA` branch — i.e. in the game where the Cyclops
  does exist — so the disabling is a WIP consequence of the emptied `GetLights()` overrides (§3b),
  not a game-roster incompatibility. (The Cyclops is not craftable/playable in BZ, but that is a
  prefab/recipe matter, orthogonal to the C# type availability.)
- The decompiled tree is a single BZ `Assembly-CSharp`; there is no separate SN-vs-BZ folder split.
  BZ-exclusive types used by the mod (`SeaTruckSegment`, `Hoverbike`, `FlashlightHelmet`) are present
  there too, confirming it is the real BZ assembly.

---

# BetterMap

## 1. Purpose and architecture

BetterMap integrates the external **SubnauticaMap** mod (referenced as the publicized
`SubnauticaMap.dll` for SN and `SubnauticaMap_BZ.dll` for BZ — `BetterMap.csproj:10-16`) and, in the
Below-Zero multiplayer build, redirects its map save/load I/O to a per-server path and triggers a
save when the multiplayer server shuts down. Almost every file does `using SubnauticaMap;`.

### 1a. `PingController` (`MonoBehaviours/PingController.cs`)

Singleton (`AbstractAwakeSingleton<PingController>`, `:6`) attached to the SubnauticaMap `Controller`
GameObject; self-destructs if that `Controller` is absent (`:12-18`). `ReloadPings()` (`:23-44`)
rebuilds the map ping icons: it clears `Controller.pingMapIconList` (`:27`), iterates the game's
`PingManager` (`:29`), and for each ping whose `origin` is still alive calls
`Controller.CreatePingMapIcon(ping)` (`:38`). SubnauticaMap references: `:2,8,27,38`.

### 1b. `SaveController` (`MonoBehaviours/SaveController.cs`)

Singleton (`AbstractAwakeSingleton<SaveController>`, `:14`) that hijacks SubnauticaMap's map I/O to a
multiplayer-client save path.

- `Awake()` (`:31-67`): under `#if BELOWZERO_MULTI` it reads
  `Network.Session.Current?.ServerId` (`:39`, game API `Subnautica.API.Features`), nulls `Controller`
  when the id is empty (`:41-44`), computes `SavePath = Paths.GetMultiplayerClientSavePath()` (`:56`)
  and `MapPath = Path.Combine(serverId, "SubnauticaMap")` (`:57`), and builds
  `RealUserStorage = new UserStoragePC(SavePath)` (`:62`) plus `FakeUserStorage = new FakeStorage()`
  (`:64`). The non-`BELOWZERO_MULTI` branch is only a `// TODO: Implementare per Subnautica`
  placeholder (`:46,59`) — the feature is implemented for BZ multiplayer only.
- `Start()`/`StartAsync()` (`:71-84`): waits for `Controller.isStarted`, then calls `PerformLoad`.
- `Update()` (`:86-94`): when loaded and the 10-second `Timerwatch Timer` (`:18`) elapses, calls
  `PerformSave()` — periodic autosave.
- `PerformLoad` (`:96-117`) and `PerformSave` (`:119-135`) run under a lock and drive
  SubnauticaMap's `Map`/`Fogmap`/`Controller.LoadMapIcons/LoadNotes/ReloadMaps` and
  `Fogmap.SaveAll`/`Controller.SaveMapIcons/SaveNotes`.

The multiplayer dependency here is indirect: `Network.Session.Current.ServerId` (game API) is used to
build the client-side save path. The explicit server hook lives in `ServerPatches.cs`.

### 1c. `FakeStorage` (`Components/FakeStorage.cs`)

A null-object implementation of the game's `UserStorage` interface: every operation (`:24-42`)
returns one of six pre-built fake operations, all set to `UserStorageUtils.Result.NotFound` with
`"Fake Message"` (`:16-21`). Assigned to `SaveController.FakeUserStorage` (`SaveController.cs:64`) and
served by `StorageUserStoragePatch` (below) while `SaveController.Instance.IsStarted` is false, so
SubnauticaMap never touches the real storage container during startup.

### 1d. Patches

`Patches/ControllerPatches.cs` (no conditional file guard):

- `ControllerRunPatch` — Postfix on `SubnauticaMap.Controller.Run` (`:7-25`), body under `#if MULTI`.
  Adds `SaveController` if absent (`:14-17`) and `PingController` if absent (`:19-22`), both
  guarded by `GetComponent<...>() == null`.
- `ControllerReloadMapsPatch` — Postfix on `Controller.ReloadMaps` (`:27-38`, always active): if
  `PingController.Instance` exists, calls `ReloadPings()`.

`Patches/ServerPatches.cs` (`#if BELOWZERO_MULTI`, `:1,23`):

- `ServerDisposePatch` — Prefix on `Subnautica.Server.Core.Server.Dispose(bool isEndGame)` (`:8-21`,
  botbenson type, import `:4`): when `SaveController.Instance` exists, logs and forces `PerformSave()`
  before the server shuts down.

`Patches/StoragePatches.cs` (`#if BELOWZERO_MULTI`, `:1,44`):

- `StorageSlotPatch` — Postfix on `SubnauticaMap.Storage.slot` getter (`:8-22`): when
  `SaveController.Instance.IsStarted`, overrides the result with `SaveController.Instance.MapPath`.
- `StorageUserStoragePatch` — Postfix on `Storage.userStorage` getter (`:24-42`): returns
  `RealUserStorage` when started, else `FakeUserStorage`.

`Patches/LoggerPatches.cs` (always active): `LoggerPrintPatch` — Prefix on `SubnauticaMap.Logger.Print`
(`:6-16`): replaces it with `Logger.Write(text)` and returns `false` to suppress the original,
redirecting SubnauticaMap's logging.

### 1e. External-mod and multiplayer references (BetterMap-wide)

| Term               | File:line                                                                 | Role                                                                                     |
|--------------------|---------------------------------------------------------------------------|------------------------------------------------------------------------------------------|
| `Subnautica.Server`| `Patches/ServerPatches.cs:4`                                              | Import of the botbenson namespace to patch `Server.Dispose`                               |
| `Nitrox`           | `Plugin.cs:5,8,15,30,32`                                                  | `BetterNitrox` dependency; waits for the `NitroxLoaded` event before applying patches (`#if BELOWZERO_MULTI`) |
| `SubnauticaMap`    | `PingController.cs:2`; `SaveController.cs:5,57`; `ControllerPatches.cs:3`; `LoggerPatches.cs:2`; `StoragePatches.cs:4` | `using SubnauticaMap;` in the controllers/patches; `"SubnauticaMap"` literal is the map subfolder name in the save path |
| `botbenson`        | *no literal occurrence*                                                   | The bridge is via the `Subnautica.Server.Core` namespace, never the name "botbenson"     |
| multiplayer guards | `SaveController.cs:56` (`Paths.GetMultiplayerClientSavePath()`)           | No runtime `IsMultiplayer` flag; the guard is entirely `#if MULTI` / `#if BELOWZERO_MULTI` |

`BetterNitrox` (Nitrox bridge, `Plugin.cs`) and `Subnautica.Server.Core` (botbenson bridge,
`ServerPatches.cs`) are two distinct multiplayer integrations coexisting in the repo.

## 2. Features / behaviours

- Redirects SubnauticaMap save/load from the local container to a per-server multiplayer path
  (BZ.MULTI), using `FakeStorage` during startup.
- Periodic autosave every 10 seconds while the controller is loaded (`SaveController` `Timer`).
- Forced map save when the botbenson server disposes/closes (`ServerDisposePatch`).
- Recreates `SaveController`/`PingController` when SubnauticaMap's `Controller` runs, in `MULTI`
  builds only.
- Rebuilds map ping icons whenever the maps reload.
- Redirects SubnauticaMap's internal logging (`Logger.Print` → `Logger.Write`).
- Waits for Nitrox initialization before applying patches, in BZ.MULTI (`Plugin.cs:27-42`).

## 3. Changes since commit `06a354e`

`git diff 06a354e -- BetterMap`: 5 files changed (+106/-20), one file **added**, none removed/renamed.

- **New file** `MonoBehaviours/PingController.cs` (added in commit `8f35043 "Improved map ping
  and save controllers"`).
- `SaveController.cs`: the whole class was under `#if BELOWZERO_MULTI` at `06a354e`; now the class
  exists unconditionally with only the server-specific parts gated (`:7-10,38-47,55-60`), plus the
  two new `// TODO: Implementare per Subnautica` placeholders (`:46,59`) — groundwork for future
  Subnautica support. The autosave timer type changed from a `StopwatchItem`(10000f) to a
  `Timerwatch`(10) (`:18`); `ServerId` became a local `serverId` inside `Awake` (`:39`) instead of a
  public property; explicit load/save log lines were added (`:98,116,123,134`); `Timer.Restart()` is
  now also called in `PerformLoad` (`:113`).
- `ControllerPatches.cs`: the file was fully under `#if BELOWZERO_MULTI` at `06a354e`; it now compiles
  always, with only the component-creation block under the broader `#if MULTI` (so also active in
  `SN.MULTI`). Adds the conditional `PingController` creation
  and the new `ControllerReloadMapsPatch`.
- `ServerPatches.cs`: adds the `"Server is disposing, performing save..."` log line before
  `PerformSave()` (`:16`).
- `FakeStorage.cs`: the `#if BELOWZERO_MULTI` wrapper was removed; it now compiles in all variants,
  consistent with `UserStorage` being a shared game interface.

## 4. Compatibility with the updated game

- `Subnautica.Server.Core.Server` exists at
  `D:\Projects\Subnautica\BelowZero\Subnautica.Server\Subnautica.Server.Core\Server.cs:19`, and its
  `Dispose(bool isEndGame = false)` (`Server.cs:306`) matches the `ServerDisposePatch` signature —
  no compatibility break for that patch.
- **SubnauticaMap has no decompiled sources** in `D:\Projects\Subnautica\BelowZero\` (recursive grep
  for "SubnauticaMap" returns nothing). The only reference is the compiled, publicized
  `SubnauticaMap.dll` / `SubnauticaMap_BZ.dll` in `BetterMap/obj/<CONFIG>/publicized/`. A
  metadata-string scan of both DLLs confirms the presence of the patched/called symbols (`Controller`,
  `PingManager`, `Logger`, `Storage`, `Fogmap`, `Map`, and members `pingMapIconList`,
  `CreatePingMapIcon`, `LoadMapIcons`, `SaveMapIcons`, `LoadNotes`, `SaveNotes`, `ReloadMaps`,
  `userStorage`, `slot`, `Run`) in both variants. This confirms symbol existence, not full signature
  compatibility, since no decompiled source is available for a signature-level check.
- No patched type is missing: the botbenson `Server.Dispose` patch matches the real source, and every
  SubnauticaMap symbol patched by `ControllerPatches`/`LoggerPatches`/`StoragePatches` is present in
  both `SubnauticaMap.dll` and `SubnauticaMap_BZ.dll`.

## Observations (potential bugs)

- None currently known in BetterLights/BetterMap; the per-feature single-player verification
  lives in `docs/mods/betterlights.md` and `docs/mods/bettermap.md`.
