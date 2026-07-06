# BetterVehicles — single-player feature verification

Vehicle quality-of-life for both games: keyboard shortcuts that open a piloted vehicle's
upgrade modules / torpedo storage / vehicle storage straight in the PDA, a `LinkedStorage`
PDA aggregation option, automatic docked-vehicle repair, a richer docked-vehicle info text
(health + energy), the Cyclops external-camera rotation damper (SN), the Seaglide map default
(BZ) and direct Seatruck cabin enter/exit with a segment-detach key (BZ). Verified live on
BOTH games from branch `sp-verify` (SN.STABLE 19 warnings / BZ.STABLE 31 warnings, 0 errors)
through the BetterRemote bridge. Old-feature baseline: `d51c6e9` (QModManager + SMLHelper,
`mod.json` `Game: "Both"`); current tree: BepInEx + Nautilus.

## Plugin bootstrap

`Plugin.cs` (`[BepInPlugin]`, `Plugin : SubnauticaPlugin`) replaces the QMod `Core.cs` entry
point; `Using.cs` exposes the static `Core` instance unqualified.
`[BepInDependency(BetterSubnautica.MyPluginInfo.PLUGIN_GUID)]` declares the dependency the old
`mod.json` carried in `VersionDependencies` (restored in this verification, same shape as the
other seven game mods). Verified live on both games: `Plugin BetterVehicles v0.0.3.7 is
Awake!` after BetterSubnautica, zero errors, exact patch sets — SN 8 patches
(`BaseUpgradeConsoleGeometry::GetVehicleInfo`, `CyclopsCameraInput::HandleInput`,
`Exosuit::Start`, `PDA::Open`, `SeaMoth::Start`, `SubRoot::Awake/Start/SetCyclopsUpgrades`),
BZ 12 patches (`GetDockedInfo`, `Exosuit::Start`, `PDA::Open`, `Seaglide::Start`,
`SeaTruckSegment::Start/EnterHatch/IsWalkable`, `SeaTruckMotor::StopPiloting`,
`SubRoot::Awake/Start/SetCyclopsUpgrades`, `VehicleDockingBay::Dock`).

## Settings

Three Nautilus `ConfigFile`s on one plugin (the repo's multi-config pattern), all
logic-identical to the old SMLHelper set:

- `GlobalSettings` (`config_global.json`, both games): `AutomaticVehicleRepair` toggle
  (default true, `OnChange` re-applies `SetCyclopsUpgrades()` on every `SubRoot` tracked in
  `SubRootContainer`), `LinkedStorage` toggle (default false), `UpgradeModules` keybind U,
  `TorpedoStorage` keybind T, `VehicleStorage` keybind V.
- `CyclopsSettings` (`config_cyclops.json`, SN only): `CameraRotationSpeedDamper` slider 0-5
  (default 1).
- `SeatruckSettings` (`config_seatruck.json`, BZ only): `ForceAction` keybind LeftControl,
  `DetachSegments` keybind V.

On SN the three global keybinds are custom `GameInput` buttons in the game's Mod Input tab
("Better Vehicles" category, registered in `Buttons.cs`); the config values seed the default
bindings. Verified live: Nautilus registers 5+1 options on SN and 5+2 on BZ at load, and the
U panel opens from the custom button while piloting (SN 2026-07-07).

## Storage controllers and PDA linked storage

`AbstractVehicleStorageController` binds a `Vehicle`, reads the three global keybinds while
piloting and opens the PDA inventory over the selected container set;
`ExosuitStorageController` (both games) and `SeamothStorageController` (SN) specialize the
storage sources. `PDAPatches` (`PDA.Open` prefix) implements `LinkedStorage` by attaching the
vehicle storage set when the PDA opens with no used storage. The old `storageContainers !=
null` guard (the array, never null there) is fixed to the element-level `storageContainer !=
null` in both loops (`AbstractVehicleStorageController.cs:45`, `PDAPatches.cs:24`) — a strict
bug fix over the old tree, which could pass null to `Inventory.main.SetUsedStorage`.

Verified live on both games: `SeamothStorageController` attached on `SeaMoth(Clone)` (SN),
`ExosuitStorageController` attached on `Exosuit(Clone)` (SN and BZ), and
`GetVehicleStorage()` / `GetUpgradeModules()` / `GetTorpedoStorage()` all invoke cleanly on
the live components (the game APIs they touch — `GetSlotCount`, `GetStorageInSlot`,
`upgradesInput.equipment` — resolve on both games). The keypress→PDA flow is verified live on
SN1 through BetterRemote virtual input while piloting: on a Seamoth carrying two
`VehicleStorageModule`s and a `SeamothTorpedoModule` (installed via the bridge `/equip`
endpoint), U opens the PDA over the upgrades equipment, T over the torpedo-module storage and
V over both storage-module containers (used-storage count 2); on the Exosuit, U and V open
(native storage container) while T with no torpedo arm leaves the PDA closed — each key maps
to its own container set and an empty set opens nothing. `LinkedStorage` on: a plain Tab PDA
open aggregates `GetVehicleStorage()` (count 2 on the module-equipped Seamoth); toggled off,
the same open attaches nothing. The BZ keypress flow is verified live the same way while
piloting the Exosuit (the only `Vehicle` on BZ): U opens the PDA over the upgrades equipment,
V over the native storage container, T with claw arms leaves the PDA closed and opens the
torpedo-arm container once `/equip` installs an `ExosuitTorpedoArmModule`; `LinkedStorage` on
BZ aggregates the Exosuit storage on a plain Tab open (used-storage count 1) and attaches
nothing when toggled off.

## Automatic vehicle repair

`SubRootPatches` forces `SubRoot.vehicleRepairUpgrade = AutomaticVehicleRepair` on every
non-Cyclops `SubRoot` at `Start` and again after `SetCyclopsUpgrades` (the vanilla method
resets the flag from installed modules, SN `SubRoot.cs:524/542`); the Cyclops keeps its
vanilla module-driven value. `SubRootContainer` (fed by the `SubRoot.Awake` postfix) tracks
live SubRoots for the settings `OnChange` re-apply.

Verified live: base `SubRoot` flag True and Cyclops flag False on SN, base flag True on BZ;
invoking `SetCyclopsUpgrades()` on a base re-lands True through the postfix. Functional chain
on SN: a SeaMoth teleported into the moonpool docking trigger docked through the full vanilla
flow, and with the docked SeaMoth set to 100/200 health the bay repaired it 100 → 150 →
175 → 200 (+25 every 5 s via the vanilla `InvokeRepeating("RepairVehicle")` at
`VehicleDockingBay.cs:247-248`, gated on `subRoot.vehicleRepairUpgrade` at `:297`).

`VehicleDockingBayPatches` (BZ only) re-arms `CancelInvoke + InvokeRepeating("RepairVehicle",
0, 5)` in a `Dock` postfix: BZ vanilla declares `RepairVehicle` (`VehicleDockingBay.cs:350`,
gated on `MoonpoolExpansionEnabled() || vehicleRepairUpgrade` at `:358`) but never schedules
it on the bay itself. The Seatruck Moonpool also repairs through its manager's own 2-second
tick (`MoonpoolExpansionManager.RepairTruck` → `bay.RepairVehicle()`, `repairTickTime = 2`);
a classic Moonpool has no vanilla scheduler at all, so there the patch IS the repair feature,
mirroring what SN schedules natively. The save-load restore path (`SetVehicleDocked`, `:303`)
bypasses `Dock`, so a vehicle already docked at load starts repairing after its next live
dock — identical to the old mod.

Verified live on BZ (2026-07-06): the piloted Seatruck cab entered the Moonpool docking
trigger and docked through the full vanilla chain (`VehicleDockingBay.OnTriggerEnter` →
`PrepDocking` → docking timeline → getup); the `Dock` postfix armed the repeating invoke —
`IsInvoking("RepairVehicle")` reads True on the bay, which vanilla never arms — and the cab
healed 380 → 500, with a re-damage to 400 back at 500 in under 5 s (the manager tick and the
patched invoke both add +25 while powered). The expansion undock path skips
`SetVehicleUndocked`, so the armed invoke persists as a no-op on the empty bay until the next
dock cycle. On the MP creative world (2026-07-08, two-client session) the classic Moonpool
holds a save-docked Exosuit and `IsInvoking("RepairVehicle")` reads True on that bay on BOTH
sides — the botbenson initial-sync dock replay runs the real dock chain, so in multiplayer a
save-docked vehicle arms the repair invoke without waiting for a live dock cycle.

## Docked-vehicle info text

`BaseUpgradeConsoleGeometryPatches` — one shared postfix body, `GetVehicleInfo(Vehicle)` on
SN / `GetDockedInfo(Dockable)` on BZ (the `#if` head is required: `Dockable` exists only in BZ,
where `GetEnergyScalar()` is native `Dockable.cs:223`; on SN it resolves to the core
`VehicleExtensions` extension). Strips the vanilla FullyCharged/Charging lines, appends a
`<size=30>` health+energy line (`VehicleStatusFormat` / `VehicleStatusChargedFormat`) under
the Docked line, upper-cases the result.

Verified live on SN with the docked SeaMoth — the moonpool console reads `SEAMOTH ATTRACCATO
/ <SIZE=30>SALUTE 100% ENERGIA COMPLETAMENTE CARICA</SIZE>` (the upper-cased tag renders:
TMP rich-text tags are case-insensitive; the old tree upper-cased identically), and with the
bay empty the vanilla "NESSUN VEICOLO ATTRACCATO" passes through untouched (the postfix exits
on null vehicle). On BZ the patch renders only on a classic Moonpool's upgrade console: the
Seatruck Moonpool terminal is driven by `MoonpoolExpansionTerminal.GetDockedInfo` — a private
body on a separate `MonoBehaviour` (`MoonpoolExpansionTerminal.cs:113`) — and the
`BaseUpgradeConsoleGeometry` sharing that console GameObject holds a null `dockingBay`, so
the patched `GetDockedInfo` never runs there. Verified live with the cab docked (2026-07-06):
the expansion terminal shows the vanilla `ATTRACCATO / COMPLETAMENTE CARICO` text without the
mod's health line. The classic Moonpool render is verified live on the MP creative world
(2026-07-08, two-client session): with the Exosuit docked in the classic bay, the console's
`infoPanel.text` reads `ATTRACCATO / <SIZE=30>SALUTE 100 % / ENERGIA COMPLETAMENTE
CARICA</SIZE>` — the patched health line under the vanilla Docked line — identically on
host and client.

## Cyclops camera rotation damper (SN)

`CyclopsPatches` prefixes `CyclopsCameraInput.HandleInput`, applying
`CyclopsSettings.CameraRotationSpeedDamper` to `rotationSpeedDamper` whenever it differs. The
old tree patched `CyclopsCameraInput.Update`, which the released SN1 removed; `HandleInput`
(`CyclopsCameraInput.cs:35`) is the only reader of `rotationSpeedDamper` (`:37`, vanilla
default 3) and runs per input-frame from `CyclopsExternalCams.HandleInput`
(`CyclopsExternalCams.cs:143`) while the external cams are active — the retarget is
semantically equivalent to the old per-frame hook.

Verified live with the external cams driven through `CyclopsExternalCams.SetActive(true)`:
the active camera's damper snaps 3 → 1 (the config value) while the two inactive cameras hold
the vanilla 3, and after `ChangeCamera(+1)` the newly active camera snaps 3 → 1 in turn.
Rotation follows the slider through the real input path (BetterRemote look injection into
`GameInput.GetLookDelta`): identical small-delta injections yaw the active camera 202.5° at
slider 1 and 67.5° at slider 3 — the exact 3× ratio of the division semantics (large
per-frame deltas saturate the look pipeline and compress the ratio). Cycling cameras with the
real `CycleNext`/`CyclePrev` bindings (virtual `[` / `]`) moves `cameraIndex` and the prefix
snaps each newly active camera to the slider value.

## Seaglide

- **SN — the old `#if SUBNAUTICA_STABLE` patch family is RETIRED as obsolete.** The four old
  patches (force `toggleLights.lightState = 2` at `PlayerTool.Awake`, save/restore
  `lightState` around `Seaglide.Update`, toggle 2↔0 in `PlayerTool.OnAltDown`, Italian custom
  use text in `PlayerTool.GetCustomUseText`) targeted the 2021 legacy build, where
  `lightState` drove the seaglide map. The released SN1 provides all of it natively and
  disconnects the old lever:
  - `ToggleLights.lightState` is a write-only vestige — declared and incremented
    (`ToggleLights.cs:26/133-136`), read by NOTHING in either game's codebase — so all three
    lightState patches are no-ops today (verified live: the forced `2` lands and nothing
    changes).
  - The holographic map toggles natively on AltTool (`VehicleInterface_MapController.cs:67`),
    the light on RightHand (`ToggleLights.CheckLightToggle`, `:129`).
  - `Seaglide` overrides `GetCustomUseText` (`Seaglide.cs:258`) with localized
    lights+map tooltips and never calls the patched `PlayerTool` base — the old text patch is
    unreachable (verified live: the vanilla localized string comes back).
- **BZ — `Seaglide.Start` postfix kept**: sets `VehicleInterface_MapController.mapActive =
  false`, flipping the vanilla default-on map (BZ `VehicleInterface_MapController.cs:50`;
  AltTool still toggles it back on, `:132-134`). Verified live: a freshly spawned seaglide
  reads `mapActive = False`.

## Seatruck direct enter/exit (BZ)

`SeaTruckSegmentStartPatch` attaches `SeatruckController` to the main segment
(`IsMainSegment()`, a core extension); the controller self-limits to the cab with a motor and
reads `ForceAction` / `DetachSegments` in `Update`, gated on the PDA being closed and
`!FreezeTime.HasFreezers()` (`SeatruckController.cs:34`, the repo-wide freeze idiom). The
`EnterHatch` prefix/postfix pair marks the hatch window (`DirectEnter = ForceAction`), the
`StopPiloting` prefix marks `DirectExit = ForceAction`, and the `IsWalkable` prefix skips
walk-mode (returns false without the original) when `DirectExit` or `DirectEnter &&
EnterHatch`, then resets all flags.

Verified live on a spawned `SeaTruck(Clone)` (controller present on the cab; absent on a
spawned `SeaTruckStorageModule(Clone)` — the main-segment guard holds while the sibling
debugger controller attaches fine): baseline `IsWalkable() = True`; `DirectExit = true` →
`IsWalkable() = False` + flags auto-reset + next call True again; `DirectEnter` alone →
True with the flag retained; `DirectEnter + EnterHatch` → False + full reset.

The real LeftControl UX is verified live through BetterRemote virtual input on a seatruck
with a docking module attached (rear connection occupied, so vanilla hatch entry walks in):
a hatch click without ForceAction enters walk mode (`Player.currentInterior` = the cab),
while the same click with virtual LeftControl held lands straight in the pilot seat
(`SeaTruckMotor.IsPiloted()` = True); exiting with E while ForceAction is held drops the
pilot straight into the water (`currentInterior` null), while a plain E exits to walk mode
inside the cab. V while piloting detaches the rear module (`isRearConnected` True → False),
and reversing back into the module reconnects it through the vanilla trigger. A held
virtual key expires on a wall-clock hold window — reads and clicks that must see the key as
down chain over raw HTTP within the hold, since MCP round-trip latency outlives the default.

## Restored / retired / dropped

- **RESTORED — `[BepInDependency]` on `Plugin.cs`**: the BetterSubnautica dependency from the
  old `mod.json`.
- **RETIRED — SN Seaglide patch family**: obsolete against the released SN1 (native map/light
  toggles and tooltips; `lightState` vestigial) — evidence above. `SeaglidePatches.cs` keeps
  the BZ branch only.
- **FIXED — element-level null guards** in the two storage loops (old array-level guard was a
  no-op).
- Everything else survives with identical logic (`EnsureComponent`, Nautilus namespaces and
  `HasFreezers()` are the repo-wide modernizations).

## NEEDS-USER checklist

Real-input-only checks:

- ~~Press U / T / V while piloting Seamoth, Exosuit (SN)~~ — VERIFIED on SN1 2026-07-06 via
  BetterRemote virtual input, including the `LinkedStorage` plain-open aggregation (see the
  storage controller section). ~~The BZ keypress flow~~ — VERIFIED on BZ 2026-07-06 on the
  Exosuit, `LinkedStorage` included (same section). ~~The while-docked variant~~ — VERIFIED
  2026-07-07 on the BZ Exosuit docked in the classic Moonpool: U, V and T (torpedo arm
  installed) each open their panel; one panel at a time, closing with Tab between presses,
  is the intended flow.
- ~~BZ Seatruck with `ForceAction` (LeftControl) held: enter the cabin hatch straight into
  the pilot seat, exit straight into the water, and detach segments with V while piloting~~
  — VERIFIED on BZ 2026-07-06 via virtual LeftControl hold + hatch click, with plain-click
  and plain-E vanilla controls walking in/out instead (see the Seatruck section).
- ~~BZ: dock the Seatruck cab — a damaged vehicle repairs +25 every 5 s~~ — VERIFIED on BZ
  2026-07-06 through the live Seatruck Moonpool dock cycle (see the repair section).
  ~~The docked-info health/energy line on a classic Moonpool console with the Exosuit
  docked~~ — VERIFIED on the BZ MP creative world 2026-07-08, identical render on host and
  client (see the docked-info section).
- ~~SN Cyclops: ride the external cameras — rotation follows the damper slider~~ — VERIFIED
  on SN1 2026-07-06 via look injection: 3× yaw ratio between slider 1 and 3, camera cycle on
  the real `CycleNext`/`CyclePrev` bindings (see the damper section).
