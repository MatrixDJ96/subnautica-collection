# BetterSavegames and BetterVehicles — technical analysis

*Snapshot: 2026-07-05 — `develop` working tree (revival not yet committed), game builds
BZ 1.22.53872 (botbenson-patched) and SN1 changeset 83031 (vanilla). Line references are valid
for this snapshot.*

Analysis of `BetterSavegames/` and `BetterVehicles/`, two of the four projects revived from
their last sources at `d51c6e9` and ported to the current game versions. Cross-checked against
BOTH decompiled trees (`D:\Projects\Subnautica\BelowZero\`, `D:\Projects\Subnautica\Subnautica\`).

---

## BetterSavegames

> **Status: builds in all four configurations; verified in-game on BZ.MULTI (2026-07-05):
> 6 patches applied, 6 options registered, and the Play panel shows the created
> "SaveGameSlot" TMPro label ("Slot 0000") — the `MainMenuLoadPanel` patch verified live.**
> The `WaitScreenController` framerate unlock engages only while a load/wait is in progress;
> it is exercised during the Phase 3 world-load session, together with
> quicksave/autosave/quickload.

### 1. Purpose and architecture

- `BetterSavegames/Plugin.cs:8` — `Plugin : SubnauticaPlugin`, registers `Settings` (line 10),
  `Core` singleton (line 12).
- `BetterSavegames/Settings.cs:8` — `Settings : ConfigFile`, `[Menu("Better Savegames")]`,
  default ctor (default config path, unlike BetterVehicles' named configs): `Quicksave` keybind
  F5 (line 11), `Quickload` keybind F9 (14), `EnableAutosave` toggle (17), `AutosaveInterval`
  slider 1-60 min (20), `AutoloadLatestSavegame` toggle (23), `MaximizeLoadingSpeed` toggle
  (26).
- `BetterSavegames/MonoBehaviours/SavegameController.cs:12` —
  `AbstractAwakeSingleton<SavegameController>`. Under `BELOWZERO_MULTI`, `Awake` destroys the
  component when a botbenson multiplayer session is active
  (`Network.Session.Current?.ServerId`, the BetterMap `SaveController` convention): saving is
  server-managed there and the vanilla slot directories do not exist (verified live,
  2026-07-05). `AsyncStart()` waits on the singleton
  availability chain (`LightmappedPrefabs`, `PAXTerrainController`, `uGUI`,
  `WaitScreen.IsWaiting`, `HandReticle`, lines 36-43 — the same convention as BetterHUD's
  `TimeDisplayController`); `Update()` (65-89) accumulates the autosave timer only while time
  is not frozen (`!FreezeTime.HasFreezers()`, line 69) and reads the quicksave/quickload keys;
  `CanSaveGame()` (91-100) gates on `IngameMenu.main.GetAllowSaving()` and permadeath;
  slot copy is async via `UserStorage` container operations (138-183); `SaveToSlot` (185-220)
  swaps `SaveLoadManager.currentSlot` to the quick/auto slot, triggers
  `IngameMenu.SaveGameAsync`, and restores; `LoadLatestSlot` (222-230). The original slot name
  survives in a `slotname.bin` file, Base64-coded via the core
  `StringExtensions.Base64Encode/Decode` (`BetterSubnautica/Extensions/StringExtensions.cs:8-18`,
  restored with the revival).
- `BetterSavegames/MonoBehaviours/WaitScreenController.cs:5` — `OnWaitingChanged(bool)`
  (11-26) tracks the waiting transition; while waiting and `MaximizeLoadingSpeed` is on,
  `UnlockFramerate` forces `targetFrameRate=-1` + `vSyncCount=0`, and `RestoreFramerate`
  re-applies the previous values when the wait ends (28-47).
- Patches:
  - `PlayerPatches.cs:8` — `Player.Awake` postfix, `EnsureComponent<SavegameController>`.
  - `WaitScreenPatches.cs:8` — `WaitScreen.Awake` postfix, `EnsureComponent<WaitScreenController>`;
    `WaitScreenPatches.cs:18` — `WaitScreen.Update` postfix forwarding the private
    `__instance.isWaiting` to `OnWaitingChanged` (the waiting flag flips inside `Update`).
  - `IngameMenuPatches.cs:10` — `IngameMenu.SaveGameAsync` prefix (quick/auto slot rerouting).
  - `MainMenuLoadPanelPatches.cs:9` — `MainMenuLoadPanel.UpdateLoadButtonState` postfix: adds the
    "SaveGameSlot" TMPro label with the decoded original slot name (`Text` aliases
    `TMPro.TextMeshProUGUI`, line 3 — both games use TMPro on `MainMenuLoadButton`).
  - `uGUIMainMenuPatches.cs:7` — `uGUI_MainMenu.Start` prefix: autoload of the most recent
    savegame when `AutoloadLatestSavegame` is on.

### 2. Features/behavior

- Quicksave (F5) and quickload (F9) on dedicated slots, plus interval autosave — all without
  touching the player's original slot: the mod copies containers and swaps
  `SaveLoadManager.currentSlot` around the native save, and the original slot name is shown in
  the load menu via the injected label.
- Optional autoload of the latest savegame straight from the main menu.
- Optional framerate unlock while a loading/wait screen is active (`MaximizeLoadingSpeed`).

### 3. What changed in the revival (since `d51c6e9`)

- **`WaitScreen.Show/Hide` are gone** — the waiting flag now flips inside `WaitScreen.Update`,
  so the old Show/Hide prefixes were redesigned as a single `Update` postfix feeding
  `WaitScreenController.OnWaitingChanged` transition tracking.
- **`IngameMenu.SetPleaseWaitVisible` is gone** — the current `SaveGameAsync` handles the
  please-wait panel, `NotifySaveInProgress` and menu close itself; the mod's explicit calls
  were dropped.
- **`PAXTerrainController.isWorking` + `uGUI.isLoading` are gone** — the `AsyncStart` wait
  loop uses the repo-wide `WaitScreen.IsWaiting` convention.
- **`GameModeUtils` exists ONLY in SN1, `GameModeManager` ONLY in BZ** — `CanSaveGame` gates
  the permadeath check with `#if SUBNAUTICA` (`SavegameController.cs:93-97`), the only game
  gate in the project.
- Stale `Text` alias gate dropped — BOTH games use TMPro on `MainMenuLoadButton`.
- `EnsureComponent` idiom (repo canon) replaced the GetComponent==null→AddComponent pattern.
- `FreezeTime` freeze check uses the public `FreezeTime.HasFreezers()` (same idiom as
  BetterVehicles) — `FreezeTime` lives in firstpass as `UWE.FreezeTime`.

### 4. Game-API notes (verified against both decompiled trees)

Private members (publicized at build time): `IngameMenu.SaveGameAsync` (BZ:573/SN:532),
`IngameMenu.GetAllowSaving` (BZ:314/SN:274), `WaitScreen.Awake/Update/isWaiting`
(BZ=SN:92/97/74), `uGUI_MainMenu.Start/HasSavedGames/LoadMostRecentSavedGame` (:80/114/128),
`MainMenuLoadPanel.UpdateLoadButtonState` (BZ:66/SN:86), `SaveLoadManager.currentSlot`
(BZ:345/SN:314), `UserStoragePC.savePath` (BZ:78/SN:77).

Public API: `WaitScreen.IsWaiting` (:80), `SaveLoadManager.GetActiveSlotNames/LoadSlotsAsync`
(BZ:904/702, SN:871/664), `PlatformUtils.main.GetUserStorage()` (`PlatformUtils.cs:227`),
`UserStorage.DeleteContainerAsync/CreateContainerAsync/CopyFilesFromContainerAsync`
(`UserStoragePC.cs:296/265/341`), `UserStorageUtils.AsyncOperation/CopyOperation`,
`IngameMenu.Open/Close/QuitGame(bool)` (BZ:292/309/511, SN:252/269/475),
`MainMenuLoadButton.saveGame/load`, `UWE.FreezeTime.HasFreezers()` (firstpass `:271`, both
trees), `GameModeUtils.IsPermadeath()` (SN `GameModeUtils.cs:101`),
`GameModeManager.GetOption<bool>(GameOption.PermanentDeath)` (BZ).

---

## BetterVehicles

> **Status: builds in all four configurations; verified in-game on BZ.MULTI (2026-07-05):
> 12/12 patches applied (exact expected list), Nautilus menus registered (Global 5 options,
> Seatruck 2 options), `config_global.json` + `config_seatruck.json` created on disk, zero
> errors. SN smoke (re-verified live 2026-07-06): 8/8 SN patches applied — exact list in
> `docs/mods/bettervehicles.md` — including the retargeted
> `CyclopsCameraInput::HandleInput`.** The world-session checks (Seatruck direct enter/exit +
> detach, linked storage, docking auto-repair, Cyclops camera damper) are verified — see the
> NEEDS-USER checklist in `docs/mods/bettervehicles.md`.

### 1. Purpose and architecture

- `BetterVehicles/Plugin.cs:9` — `Plugin : SubnauticaPlugin`. Registers **multiple named
  ConfigFiles** (`base("config_…")` ctor): `GlobalSettings` always (line 11), plus
  `CyclopsSettings` under `#if SUBNAUTICA` (13) or `SeatruckSettings` under `#elif BELOWZERO`
  (15).
- Settings:
  - `Settings/GlobalSettings.cs:9` (`config_global`): `AutomaticVehicleRepair` toggle with
    `OnChange` (13), `LinkedStorage` toggle (16), `UpgradeModules` keybind U (19),
    `TorpedoStorage` keybind T (22), `VehicleStorage` keybind V (25).
    `AutomaticVehicleRepairEvent` (28-37) re-applies `SetCyclopsUpgrades()` on every tracked
    `SubRoot` via `SubRootContainer.Instance.Dict`.
  - `Settings/CyclopsSettings.cs:8` (`config_cyclops`, `#if SUBNAUTICA`):
    `CameraRotationSpeedDamper` slider 0-5 (12).
  - `Settings/SeatruckSettings.cs:9` (`config_seatruck`, `#if BELOWZERO`): `ForceAction`
    keybind LeftControl (13), `DetachSegments` keybind V (16).
- MonoBehaviours:
  - `AbstractVehicleStorageController.cs:7` (file name matches the class) — base
    `MonoBehaviour` bound to a `Vehicle` in `Awake` (11-14); `Update` (16-57) reads the three
    global keybinds and opens the PDA on `PDATab.Inventory` over the requested storage set;
    `GetStorageContainers(TechType)` (59-72); abstract `GetDefaultStorage/GetTorpedoStorage`,
    virtual `GetUpgradeModules` (reads `component.upgradesInput.equipment`, 76-79) and
    `GetVehicleStorage`.
  - `ExosuitStorageController.cs:3` — default storage = `(component as Exosuit)
    .storageContainer.container`.
  - `SeamothStorageController.cs:6` (`#if SUBNAUTICA`) — torpedo storage from
    `SeamothTorpedoModule` slots.
  - `SeatruckController.cs:8` (`#if BELOWZERO`) — attached per Seatruck; `Awake` (18-30)
    self-destroys unless on the main segment (`IsMainSegment()`, a core extension); `Update`
    (32-51) handles force-action and detach keys, gated on `!FreezeTime.HasFreezers()` (34).
  - `SubRootContainer.cs:5` — `AbstractSingletonContainer<SubRootContainer,int,SubRoot>`
    tracking live `SubRoot` instances for the auto-repair option.
- Patches:
  - `BaseUpgradeConsoleGeometryPatches.cs` — one shared postfix body with a gated head:
    `#if SUBNAUTICA` targets `GetVehicleInfo(Vehicle)` (9-14), `#elif BELOWZERO` targets
    `GetDockedInfo(Dockable)` (16-20). Rewrites the docked-vehicle info text with health +
    energy (`Language.main.GetFormat`, 40-44), joining lines via HarmonyLib's
    `GeneralExtensions.Join` (50).
  - `CyclopsPatches.cs:8` (`#if SUBNAUTICA`) — **prefix on `CyclopsCameraInput.HandleInput`**
    scaling `rotationSpeedDamper` by the `CameraRotationSpeedDamper` setting.
  - `ExosuitPatches.cs:8` — `Exosuit.Start` postfix, attaches `ExosuitStorageController`.
  - `SeamothPatches.cs:9` (`#if SUBNAUTICA`) — `SeaMoth.Start` postfix, attaches
    `SeamothStorageController`.
  - `PDAPatches.cs:9` — `PDA.Open` prefix implementing `LinkedStorage` (aggregates the
    docked/linked containers into the opened storage view).
  - `SeaglidePatches.cs` (`#if BELOWZERO`) — a single `Seaglide.Start` postfix defaulting the
    Seaglide HUD map to off (`mapActive = false`; the BZ vanilla default is on). Released SN1
    ships the old SN branch's behavior natively — light toggle on RightHand
    (`ToggleLights.CheckLightToggle`), map toggle on AltTool (`VehicleInterface_MapController`),
    localized tooltips via a `Seaglide.GetCustomUseText` override — so no SN patches exist
    (`docs/mods/bettervehicles.md`).
  - `SeatruckPatches.cs` (`#if BELOWZERO`) — `SeaTruckSegment.Start` postfix (10),
    `SeaTruckSegment.EnterHatch` prefix+postfix (23), `SeaTruckMotor.StopPiloting` prefix
    (45), `SeaTruckSegment.IsWalkable` prefix (58): direct enter/exit of the Seatruck cabin
    and walkability control.
  - `SubRootPatches.cs` — `SubRoot.Awake` (7, registers into `SubRootContainer`),
    `SubRoot.Start` (17), `SubRoot.SetCyclopsUpgrades` (31) postfixes implementing the
    auto-repair option (no game gate: `SubRoot` is the seabase/Cyclops root in both games).
  - `VehicleDockingBayPatches.cs:7` (`#if BELOWZERO`) — `VehicleDockingBay.Dock` postfix
    re-invoking `RepairVehicle`.

### 2. Features/behavior

- Keyboard shortcuts (U/T/V, remappable) that open a docked or piloted vehicle's upgrade
  modules, torpedo storage, or storage directly in the PDA inventory view.
- `LinkedStorage`: the PDA storage view aggregates the vehicle's linked containers.
- Automatic vehicle repair while docked (Cyclops upgrades in SN, docking bays in BZ) and a
  richer docked-vehicle info text (health + energy).
- Below Zero: direct Seatruck cabin enter/exit, segment detach key, force-action key, Seaglide
  HUD map defaulted off.
- Subnautica 1: Cyclops external-camera rotation damper slider.

### 3. What changed in the revival (since `d51c6e9`)

- **SN1 `CyclopsCameraInput.Update` is GONE** — the patch was retargeted to a `HandleInput`
  prefix: `HandleInput` is the ONLY reader of `rotationSpeedDamper`, called per input-frame by
  `CyclopsExternalCams.HandleInput` while the external cams are active, so the prefix is
  semantically equivalent to the old per-frame `Update` hook.
- **`FreezeTime` is `UWE.FreezeTime` in firstpass** with the private `freezers` list — the
  code uses the public `FreezeTime.HasFreezers()` instead.
- **BZ `Dockable.GetEnergyScalar()` is native now** — on BZ the call in
  `BaseUpgradeConsoleGeometryPatches.cs:27` resolves to the game method (`Dockable.cs:223`);
  on SN it resolves to the core extension (`VehicleExtensions.cs:28`). `HasEnergySource()` is
  a core extension on both sides (`DockableExtensions.cs:6` BZ / `VehicleExtensions.cs:18`
  SN).
- **Many patch targets turned private** — `SeaTruckSegment.Start`, `Seaglide.Start` (BZ),
  `VehicleInterface_MapController.mapActive`, `SubRoot.SetCyclopsUpgrades`,
  `BaseUpgradeConsoleGeometry.GetVehicleInfo/GetDockedInfo` — all covered by the publicizer.
  `CyclopsCameraInput.rotationSpeedDamper` stayed public.
- File renamed `AbstractVehicleController.cs` → `AbstractVehicleStorageController.cs` (matches
  the class); the old `storageContainers != null` foreach typo fixed to `storageContainer`;
  `EnsureComponent` idiom applied.

### 4. Game-API notes (verified against both decompiled trees)

- `CyclopsCameraInput.HandleInput` (SN `CyclopsCameraInput.cs:35`, public) reads
  `rotationSpeedDamper` (`:5`, public) at `:37`; its caller is
  `CyclopsExternalCams.HandleInput` (`CyclopsExternalCams.cs:143`). No `Update` method exists
  on the class.
- `Dockable` exists ONLY in BZ (`Dockable.cs:8`; `GetEnergyScalar()` `:223`); SN's
  `BaseUpgradeConsoleGeometry.GetVehicleInfo(Vehicle)` (SN `:116`, private) vs BZ's
  `GetDockedInfo(Dockable)` (BZ `:146`, private) — the `#if` head in
  `BaseUpgradeConsoleGeometryPatches.cs` is REQUIRED.
- Seatruck API (BZ only): `SeaTruckSegment.Start` (`:199`, private),
  `EnterHatch(Player)`/`IsWalkable()`/`Detach()` (`:727/714/1145`),
  `SeaTruckMotor.IsPiloted()/StopPiloting()` (`:249/626`); `IsMainSegment()` is a core
  extension (`SeaTruckSegmentExtensions.cs:7`, `#if BELOWZERO`) over the native
  `GetHead()`/`isMainCab`.
- `Vehicle.upgradesInput.equipment` (`:132`), `GetSlotCount()` (`:1438`),
  `GetStorageInSlot(int,TechType)` (BZ:1531/SN:1684), `Inventory.SetUsedStorage/
  GetUsedStorageCount/ClearUsedStorage` (BZ:228/246/251), `PDA.Open(PDATab)` (BZ:166/SN:158),
  `AvatarInputHandler.main.IsEnabled()`, `VehicleDockingBay.Dock(Dockable)/RepairVehicle()`
  (BZ:287/350) — all present and public.
- `.Join(null, "\n")` (`BaseUpgradeConsoleGeometryPatches.cs:50`) is HarmonyLib's
  `GeneralExtensions.Join<T>(this IEnumerable<T>, Func<T,string> converter = null, string
  delimiter = ", ")` — a mod-stack extension, not game API.
- Released SN1 native Seaglide behavior (grounds for the SN retirement): map toggle handled at
  `VehicleInterface_MapController.cs:67` (AltTool), light toggle in
  `ToggleLights.CheckLightToggle`, tooltip via the `Seaglide.GetCustomUseText` override
  (`Seaglide.cs:258`) that never calls the patched `PlayerTool` base;
  `ToggleLights.lightState` is a write-only vestige in BOTH games (declared and incremented,
  zero readers).

---

## Common section

### Solution registration

| Project | GUID | `.Build.0` |
|---|---|---|
| BetterSavegames | `973FE786-0517-4AA9-84F6-A56EBD27F58F` | 4/4 configurations |
| BetterVehicles | `C1FA4C2F-D045-4790-A75D-559CFB602A39` | 4/4 configurations |

Both GUIDs come from the projects' original `d51c6e9` `AssemblyInfo.cs` COM GUIDs. Solution
folder order is alphabetical (BetterSavegames after BetterRemote, BetterVehicles after
BetterSubnautica's siblings).

### Shared conventions

- Both projects follow the repo canon: `EnsureComponent` for idempotent component attachment,
  `WaitScreen.IsWaiting` for load-wait loops, `UWE.FreezeTime.HasFreezers()` for freeze
  checks, UTF-8 without BOM on source files (`.editorconfig` `charset = utf-8`), publicizer
  for private game members.
- BetterVehicles demonstrates the **multi-ConfigFile pattern** (three named configs on one
  plugin); BetterSavegames uses the default single config path.
