# BetterLights — single-player feature verification

Vehicle and tool light tuning: per-vehicle color, intensity offset and range offset,
key-toggled exterior lights with configurable energy draw and on/off sounds, and volumetric
light cones with their own intensity offset. Three independent MonoBehaviour families (Lights,
ToggleLights, VolumetricLights) attach as co-located components on each vehicle/tool
GameObject through Harmony patches. Verified live on both games (SN.STABLE and BZ.STABLE
builds), through the BetterRemote bridge. Old-feature baseline: `d51c6e9` (QModManager +
SMLHelper); current tree: BepInEx + Nautilus.

## Plugin bootstrap

`Plugin.cs` registers the Nautilus settings panes (ten classes: seven registered on SN, eight
on BZ) with the same per-game guards the old `Core.cs` used (Flashlight, Seaglide, Exosuit,
MapRoomCamera, Vehicles on both games; Seamoth and Cyclops `#if SUBNAUTICA`; FlashlightHelmet,
Seatruck, Hoverbike `#if BELOWZERO`). The BetterSubnautica `[BepInDependency]` is declared in
`Plugin.cs` (MULTI builds add the BetterNitrox one) — this mod already carried the dependency
the old `mod.json` expressed. Verified: both games log
`Plugin BetterLights v0.0.3.7 is Awake!`, the full per-method patch list (SN: SubRoot,
CyclopsExternalCams ×3, CyclopsLightingPanel ×2, SeaMoth ×2, Vehicle ×2, Exosuit ×3,
MapRoomCamera ×3, PlayerTool ×2, SubRoot.UpdateLighting, ToggleLights ×2, GUIHand,
VFXVolumetricLight ×2; BZ: Dockable ×2, Exosuit ×4, FlashlightHelmet, Hoverbike ×3,
MapRoomCamera ×3, PlayerTool ×2, SeaTruckSegment, SeaTruckLights, ToggleLights ×2,
VFXConstructing, VFXVolumetricLight ×2) and zero errors.

## Architecture

The three families share one template: an abstract controller generic over the host
component `T` (`AbstractLightsController<T>`, `AbstractToggleLightsController<T>`,
`AbstractVolumetricLightsController<T>`) behind an interface (`ILightsController`,
`IToggleLightsController`, `IVolumetricLightsController`). The patches attach the families as
separate components on the host GameObject; they find each other only through
`GetComponent`/`GetComponentInParent` on those interfaces.

- **Lights**: `Awake` destroys the controller when the host component is missing, `Start`
  destroys it when `GetLights()` found no `Light` and otherwise snapshots each light's default
  color, intensity and range. A 1-second `Timerwatch` in `Update` re-reads `GetSettings()` and
  writes color, intensity and range only where they diverge from the target.
- **ToggleLights**: the controller wraps the host's vanilla `ToggleLights`, records itself as
  its owner in `ToggleLightsRegistry`, zeroes the vanilla `energyPerSecond` and draws the
  configured consumption itself in `UpdateLightsEnergy()`. The toggle key is read only while
  the PDA is closed, no time freezer is active and the cursor is locked.
- **VolumetricLights**: the controller registers each managed `VFXVolumetricLight` in the
  `VolumetricLightsContainer` singleton, keyed by instance id; the
  `VFXVolumetricLight.UpdateMaterial` prefix hands registered cones to their controller and
  lets the vanilla method run for the others. Settings refresh in `LateUpdate`.
- **Seatruck routing (BZ)**: `SeatruckLightsContainer` maps a `SeaTruckLights` instance to its
  toggle controller; the `SeaTruckLights.Update` prefix skips the vanilla update for mapped
  instances and drives only the cab `LightingController` state.
- **Exosuit cone scale**: the `VFXVolumetricLight.UpdateScale` prefix replaces the vanilla
  scale of cones under an `Exosuit` with one computed from the light's spot angle and range.
- **Unpiloted Cyclops lighting (SN)**: a `SubRoot.UpdateLighting` postfix lerps the light
  control to state 1 when the lighting state is 0 and the player is not aboard that sub.

The Cyclops sources are `#if SUBNAUTICA`-gated although the decompiled Below Zero
`Assembly-CSharp` still carries the Cyclops types (`SubRoot.isCyclops`, `CyclopsExternalCams`,
`CyclopsLightingPanel`, `TechType.Cyclops`): the gate follows the playable roster.

## Settings

Every option of the old tree survives (names, labels, ranges, steps, defaults, registration
order), with two deltas. Additive: a `[ColorPicker("Lights Color", Advanced = true)]` (default
white) in the eight per-vehicle panes (all except Cyclops and Vehicles) — the Advanced mode
renders R/G/B sliders instead of the game's swatch row, so any color (white included) is
reachable from the pane (verified live: slider drags propagate to the live `Light.color`).
Moved: the `Lights Button Toggle` keybinds left the per-vehicle panes (see below).
Config persists per pane at `BepInEx/config/BetterLights/config_<vehicle>.json`. Keybind
defaults: `Mouse1` everywhere except Exosuit (`Mouse2`); Cyclops has no keybind (its lights are
driven by the vanilla lighting panel) and Vehicles holds the single
`EnableLightsOnUndocking` toggle. The FlashlightHelmet pane has no `LightsConsumption` slider
(the helmet draws no energy: the vanilla prefab ships `ToggleLights.energyPerSecond = 0`). On
SN the five toggles (Flashlight, Seaglide, Seamoth, Exosuit, Map Room Camera) are custom
`GameInput` buttons in the game's Mod Input tab ("Better Lights" category, registered in
`Buttons.cs`); the config `KeyCode`s seed the default bindings. On BZ the seven toggles
(Flashlight, Flashlight Helmet, Seaglide, Seatruck, Hoverbike, Exosuit, Map Room Camera) stay
`KeyCode` fields of their pane's config, exposed on the shared "Better Subnautica - Input"
options page ("Better Lights" category, `ModInputUtility.RegisterKeybind` in `Buttons.cs`).
The Flashlight and Seaglide toggles act only while the tool is the held one
(`Inventory.main.GetHeldTool() == component`, both games).

## Restorations

The porting had disabled a large part of the mod; everything below is restored and verified
live.

- **Toggle lights parent resolution** (`InitializeLightsParent` in
  `AbstractToggleLightsController`): `lightsParent = component.GetLightsParent();` was
  commented out. With it gone,
  every toggle controller whose host has no vanilla `ToggleLights` resolved a null
  `lightsParent` and self-destroyed in `Awake` (`MandatoryLightsParent` defaults to true) —
  the root cause behind all the commented `AddComponent` calls below. The current
  `GetLightsParent` extension also embeds the `lights_parent` Find, the BZ
  `seatruckLights.floodLight` branch and the `toggleLights.lightsParent` fallback
  (`BetterSubnautica/Extensions/ComponentExtensions.cs`).
- **Seamoth, Exosuit and MapRoomCamera toggle + volumetric controllers**:
  the six `AddComponent` calls in `SeamothPatches.cs`, `ExosuitPatches.cs` and
  `MapRoomCameraPatches.cs` were commented out (active at `d51c6e9`); they are
  `EnsureComponent` calls again, matching the sibling patches. Verified live: all three
  controllers alive on `SeaMoth(Clone)` (SN), `Exosuit(Clone)` (both games) and
  `MapRoomCamera(Clone)` (both games); Seamoth toggle drives its `lights_parent` active state;
  Exosuit and MapRoomCamera volumetric coroutines clone `VFXVolumetricLight` data from the
  Seamoth prefab — **`CraftData.GetPrefabForTechTypeAsync(TechType.Seamoth)` resolves on Below
  Zero too** (clones observed on `light_left`/`light_right` and `light_top`/`light_bottom`,
  zero exceptions).
- **Cyclops family, SN**: `CyclopsLightsController.GetLights()` and
  `CyclopsCameraLightsController.GetLights()` were empty overrides — with `Lights.Length == 0`
  both self-destroyed in `Start`, and the four `AddComponent` calls (`CyclopsPatches.cs`,
  `CyclopsCameraPatches.cs`) were commented out. Restored: floodlight collection from the
  direct children of `CyclopsLightingPanel.floodlightsHolder` (the old collection scope),
  camera light collection from `CyclopsExternalCams.cameraLight`, `UpdateColor` virtual again
  in `AbstractLightsController` with the no-op override on the camera controller (the camera
  color belongs to the `SetLight` patch), and `Color = white` in both `GetSettings` (the old
  abstract initialized `Color.white`; the current default would paint the lights black).
  Verified live on SN: all three sub controllers alive on `Cyclops-MainPrefab(Clone)`,
  floodlight range 130 = default 100 + `ExternalLightsRangeOffset` 30, camera light range 105 =
  default 80 + `CameraLightsRangeOffset` 25, camera intensity 2 = default 1.5 +
  `CameraLightsIntensityOffset` 0.5, tampered range snaps back within the 1-second enforcement
  tick, `SetLightsActive(true/false)` drives the floodlight holder children.
- **Volumetric cone scale on range changes**: the old abstract called
  `VFXVolumetricLight.UpdateScale()` whenever it wrote a new light range; the rewritten
  abstract had lost the call, leaving cone scale stale after a `RangeOffset` change. Restored
  inside `UpdateRange`. Verified live on SN: the Cyclops floodlight cones carry `range 130`
  in sync with the offset light.
- **Debug logging gated**: the `ToggleLights.SetLightsActive` prefix
  logged every vanilla call (`[ToggleLights.SetLightsActivePatch] active: …`, observed
  repeatedly during normal BZ play). The old patch has no logging; the per-call trace now
  compiles only under `DEBUG_LOGS` (on by default, stripped with `-p:DebugLogs=false`). The
  prefix suppresses the vanilla method whenever an `IToggleLightsController` owns the component
  (`ToggleLightsRegistry`), in every build configuration.

## Verified port deltas (legitimate)

- **Lights refresh model**: per-property dirty-tracking with public setters and a 60-second
  `UpdateInterval` became a 1-second `Timerwatch` poll that re-reads `GetSettings()` and
  re-applies color/intensity/range only on divergence. `ILightsController` members are
  read-only now. Same steady-state behavior, faster convergence; enforcement verified live.
- **Configurable light color**: the old abstract forced every managed light white; the current
  controllers read the per-vehicle `LightsColor` (default white — identical at defaults) and
  snapshot `DefaultColors` alongside intensities and ranges.
- **`CyclopsExternalCams.GetUsingCameras()` → `GetActive()`**: the current game exposes
  `GetActive()` only (`CyclopsExternalCams.cs` in the decompiled SN tree); same `active`
  field underneath.
- **Camera light state preservation is still needed**: vanilla `ChangeCamera` resets
  `lightState = 1` on every camera switch (`CyclopsExternalCams.cs`); the Prefix/Postfix
  pair keeps it. Verified live: state 2 set, `ChangeCamera(1)` invoked, state still 2, and the
  `SetLight` postfix paints the camera light `(0.5, 0.5, 0.5, 1)` = controller white halved
  with alpha forced to 1.
- **`ExosuitSubConstructionCompletePatch` target**: `#if BELOWZERO` patches
  `Exosuit.SubConstructionComplete` (exists in BZ `Exosuit.cs`); the SN branch patches
  `Vehicle.SubConstructionComplete` (SN1's `Exosuit` does not override it — only the
  `Vehicle` virtual exists) with the `is Exosuit` guard in the body.
- **`MapRoomCameraControlCameraPatch` signature** is conditional (BZ passes `Player player`,
  SN does not). The toggle gate reads the per-game controlling flag (SN `component.active`,
  BZ `component.controllingPlayer != null`), so the keybind toggles only while driving the
  camera. Vanilla `HandleInput` flips `lightsParent` on the same button while controlling; the
  controller's per-frame enforcement absorbs that transient flip, so one press yields one net
  toggle (with the vanilla on/off sound).
- **Construction re-ensure** (SN `SeaMoth.SubConstructionComplete`, BZ
  `VFXConstructing.WakeUpSubmarine`): during the construction animation the light children
  are inactive, so the toggle controller added by the `Start` patch destroys itself in
  `Awake`; both completion patches `EnsureComponent` the controller before re-lighting.
  Verified live on SN: a constructed Seamoth keeps its
  `SeamothToggleLightsController` and the custom keybind toggles it.
- **Vanilla toggle detach**: `ToggleLightsRegistry` records each controller's wrapped
  `ToggleLights` instance, and the `SetLightsActive` + `CheckLightToggle` prefixes skip the
  vanilla methods on owned instances — exact instance match, because a hierarchy lookup from
  the vanilla child object misses the controller on some spawn paths. On SN the flashlight's
  RightHand tool-use pipeline (use animation + `OnToolUseAnim`) reads the mod button instead
  of RightHand (`GUIHandPatches`), so the rebindable key plays the full vanilla experience
  while the hardwired right mouse stays fully detached. The detach applies to every build
  configuration, MULTI included. Verified live on SN: RMB inert on Seamoth and Seaglide (no
  sound, no transient flip), flashlight custom key animates + toggles.
- **Seatruck**: the lights controller relies on the base `GetLights()` (the extension resolves
  `seatruckLights.floodLight`) instead of a dedicated `Awake`; collection is recursive rather
  than direct-children. The toggle controller gains an `OnDestroy` that removes its
  `SeatruckLightsContainer` entry (the old version leaked stale entries). Verified live on BZ:
  all three controllers alive on `SeaTruck(Clone)`, toggle drives `SeaTruckLights.lightsActive`
  and the suppression state.
  The `SeaTruckLights.Update` prefix keeps the cab's `LightingController` on the vanilla
  polarity — `LerpToState(powered ? 0 : 2)`, Operational while powered, Damaged when unpowered
  (a state held on `Damaged` while powered washes the hull in the overexposed emissive skin —
  observed live).
  The BZ Seatruck prefab ships `SeaTruckLights.dimFloodlightsOnEnter` EMPTY and carries no
  `VFXVolumetricLight` anywhere under `floodlight` (live dump:
  `dimFloodlightsOnEnter.Length = 0`, `light_left/center/right` bare Light components).
  The Seatruck volumetric controller therefore clones three cones onto the floodlight
  Lights via the shared `CreateVolumetricLightsAsync(Light[])` (Seamoth prefab as template)
  and hands them to `SeaTruckLights.dimFloodlightsOnEnter`, so the vanilla
  `SendMessage("OnPlayerEnter"/"OnPlayerExit")` pair in `SeaTruckSegment`
  drives the dim-on-enter legs on the cloned cones (cones verified live,
  user look: three distinct beams, `IntensityOffset -0.8`).
- **`IToggleLightsController` lives in `BetterSubnautica/Components`** (unchanged content), so
  the multiplayer stack can reference it without a project cycle; `GetToggleLights` tries
  `GetComponent` then `GetComponentInChildren` for every host type (superset of the old
  SeaMoth special case).
- **SN-only `VehiclePatches` and `SubRootPatches`**: `Vehicle.OnDockedChanged` and
  `SubRoot.UpdateLighting` have the same signatures in both games, yet both files stay
  `#if SUBNAUTICA`: Below Zero docks vehicles through `Dockable` (the BZ-gated
  `DockablePatches` pair on `OnDockingStart`/`OnUndockingComplete`), and BZ seabases leave
  `SubRoot.lightControl` null (observed on a player-built base). On BZ, `VehiclePatches` would
  duplicate `DockablePatches` and `SubRootPatches` would never pass its `lightControl != null`
  guard.

## Dropped — Cyclops camera view patches, with evidence

The old `CyclopsExternalCamsEnterCameraViewPatch` and `CyclopsExternalCamsExitCameraPatch`
targeted methods that no longer exist: the current `CyclopsExternalCams` manages the camera
lifecycle through `SetActive(bool)` and per-camera `CyclopsCameraInput.ActivateCamera` /
`DeactivateCamera` (decompiled SN `CyclopsExternalCams.ChangeCamera` and `SetActive`). The
behaviors the old patches added are vanilla now: the camera light turns off on exit
(`SetActive(false)`) and non-current cameras are deactivated on every switch
(`ChangeCamera`); the old EnterCameraView body was already
commented out at `d51c6e9`.

## Energy consumption

`UpdateLightsEnergy` draws `EnergyConsumption × DayNightCycle.deltaTime` from the host's
`IEnergySource` while the lights are on, and forces the lights off when the source is dry.
Verified live on BZ (survival save): with the Exosuit lights forced on, one of the two power
cells drains at ≈0.021/s — 0.042/s total, exactly `ExosuitSettings.LightsConsumption`, split by
`EnergyInterface` across both cells. The SN save runs a creative-flavored mode
(`GameModeUtils.RequiresPower() == false`), where the vanilla power layer ignores consumption —
zero drain there is vanilla behavior, not a mod gap.

## Virtual-input verification (SN1)

Closed via BetterRemote virtual input on the SN1 quicksave world:

- **Keybind toggles**: virtual `Mouse1` toggles the Seamoth lights while piloting
  (`LightsActive` True↔False, `lights_parent` active state follows) and does nothing after
  exiting the vehicle (the `GetPilotingMode()` gate); virtual `Mouse2` toggles the Exosuit
  lights while piloting; virtual `Mouse1` toggles the Flashlight and the Seaglide while held
  (drawn through virtual quickslot keys `Alpha2`/`Alpha4`). A holstered tool deactivates its
  GameObject, so the controller `Update` — and with it the toggle — cannot run.
- **Docking (SN `Vehicle.OnDockedChanged`)**: the piloted Seamoth entered the Cyclops docking
  trigger with lights on and docked through the full vanilla flow (player ejected into the
  sub, `docked` True) — `LightsActive` dropped to False; undocking through
  `VehicleDockingBay.SetVehicleUndocked()` (the `docked` property setter drives
  `OnDockedChanged`) re-lit the lights with `EnableLightsOnUndocking` True. Approach flying
  used virtual WASD bursts + look steering; the final trigger entry teleported the piloted
  vehicle (the launch-bay `DockedVehicleHandTarget` board cinematic does not start from a
  bridge click).
- **Cyclops construction complete**: right after a `sub cyclops` console spawn, the lighting
  panel reads `lightingOn` True AND `floodlightsOn` True (vanilla floodlights default off) —
  the `CyclopsLightingPanelSubConstructionCompletePatch` forced both on.
- **Cyclops external camera cycle**: with the cams active, the real `CycleNext`/`CyclePrev`
  bindings (virtual `[` / `]`) move `cameraIndex` 0→1→0 and the BetterVehicles damper prefix
  snaps each newly active camera; virtual `LeftHand` clicks cycle `lightState` 1→2→0 through
  `IterateLightState`.

## Scanner-room camera verification (SN1)

Closed via BetterRemote on the SN1 save with a built scanner room and three deployed cameras,
through the real interaction path: hatch enter (virtual left click on the dive-hatch hand
target sets `Player.currentSub`, which makes `MapRoomScreen` bind a camera), then a virtual
left click on the screen hand target (`GUIHand.activeTarget` = the screen's `input` object).

- **Camera drive**: the screen click runs `MapRoomCamera.ControlCamera` (camera `active` True,
  player mode `LockedPiloting`, drone HUD live); virtual `E` runs
  `FreeCamera`/`ExitLockedMode` (camera `active` False, player mode `Normal`).
- **Lights toggle while controlling**: virtual `Mouse1` flips
  `MapRoomCameraToggleLightsController.LightsActive` ON and OFF while driving, `lightsParent`
  following each flip; when not controlling, `Mouse1` leaves the state untouched (the
  `component.active` gate).
- **Volumetric cones**: `disableVolumetricVFX` on the camera's `light_top`/`light_bottom`
  cones reads True while controlling (`ControlCamera` postfix) and False after release
  (`FreeCamera` postfix).
- **Docked release**: releasing a camera docked at the scanner room forces its lights off —
  the `FreeCamera` postfix applies `dockingPoint == null && LightsActive`.

## Virtual-input verification (BZ)

Closed via BetterRemote virtual input on the prepared BZ save (base with scanner room +
deployed camera, Seatruck with docking module, Exosuit, Hoverbike, FlashlightHelmet worn):

- **Keybind toggles**: virtual `Mouse1` toggles the Seatruck lights while piloting
  (`LightsActive` and the physical `floodLight` active state follow each flip) and does
  nothing when not piloting (`IsPiloted()` gate); virtual `Mouse1` toggles the Hoverbike
  lights while riding and does nothing on foot (`isPiloting` gate; the save's bike carries no
  battery, so the check runs with `technologyRequiresPower` flipped off through the dotted
  `GameModeManager.gameOptionsManager.options` path — `EnergyMixin.charge` then reports
  infinite); virtual `Mouse1` toggles the worn FlashlightHelmet with empty hands
  (True→False→True) and is gated while a tool is held — the same press toggles the held
  Flashlight instead, leaving the helmet untouched — and while piloting; virtual `Mouse2`
  toggles the Exosuit lights while piloting (the overlay line flips to `True (External)`).
- **Volumetric cones**: the Exosuit `light_left`/`light_right` cones read
  `disableVolumetricVFX` True right after pilot begin (`OnPilotModeBegin` postfix) and False
  after exit (`OnPilotModeEnd` postfix); the camera cone follows the control/free cycle
  below. The Hoverbike, Seatruck player-inside and Cyclops cone transitions stay unexercised.
- **Scanner-room camera (BZ)**: the real screen path — a virtual left click on the screen's
  `input` hand target runs `ControlCamera` (player mode `LockedPiloting`, localized drone HUD
  live with camera number, distance, health and energy). Look injection yaws the drone 90°
  and a virtual `W` burst moves it ~9 m; virtual `E` frees it (`FreeCamera`, mode `Normal`).
  A bridge teleport never crosses the base entry triggers, so `Player.currentSub` stays null
  and the screen holds `currentIndex` -1: the verification seeds `currentIndex = 0` over the
  bridge, and the click then runs the vanilla `FindCamera → CanBeControlled → ControlCamera`
  chain unmodified. Virtual `Mouse1` toggles the camera lights ON and OFF while controlling
  (`MapRoomCameraToggleLightsController.LightsActive` plus the overlay `True (External)`
  line) and is a no-op when not controlling (`controllingPlayer` gate). Releasing the
  free-water camera with lights on keeps them on and restores the cone
  (`disableVolumetricVFX` False): the `dockingPoint == null && LightsActive` branch of the
  `FreeCamera` postfix, complementing the SN docked-release check.
- **Docking (BZ `Dockable.OnDockingStart`/`OnUndockingComplete`)**: the piloted Seatruck cab
  (docking module detached with V) drove into the Seatruck Moonpool entrance trigger with
  lights on and docked through the full vanilla chain (`VehicleDockingBay.OnTriggerEnter` →
  `PrepDocking` → docking timeline → getup) — `LightsActive` dropped to False; re-boarding
  the docked cab fired the expansion undock flow (`MoonpoolExpansionManager.Update` starts
  `StartUndocking` once the docked motor reads piloted; timeline → `bay.OnUndockingComplete`
  → exit thrust to `seatruckExitLocation`) and re-lit the lights with
  `EnableLightsOnUndocking` True. The re-board used `SeaTruckMotor.SetPiloting(true)` over
  the bridge — the call the manager's own getup sequence issues — since the rear-door hand
  target never resolves from the walkway; the undock chain itself ran unmodified.

## NEEDS-USER checklist

- ~~Keybind toggles on the BZ vehicles/tools (Seatruck, Hoverbike, FlashlightHelmet) and on
  the BZ MapRoomCamera while controlling it from a scanner room~~ — VERIFIED on BZ via
  BetterRemote virtual input, Exosuit `Mouse2` included (see the BZ verification section).
- On/off sounds on toggle: ~~BZ (Hoverbike, FlashlightHelmet, Seatruck — including the
  automatic dock/undock re-light)~~ — VERIFIED by ear; the SN toggles remain.
- Volumetric cones disable/restore: ~~Exosuit pilot begin/end and BZ MapRoomCamera
  control/free~~ — VERIFIED on BZ; ~~Hoverbike enter/exit~~ — VERIFIED live
  (`disableVolumetricVFX` True mounted / False dismounted, both directions);
  ~~Seatruck cones~~ — CLONED by the controller (see the Seatruck note above) and VERIFIED by
  user look; the player-inside dim transition rides the vanilla `dimFloodlightsOnEnter` path
  (quick eye check while boarding still open); Cyclops floodlights-while-aboard
  (`ToggleFloodlights` patch, SN) remains.
- ~~Seatruck hull lighting after the `LerpToState` polarity fix~~ — VERIFIED (user look +
  live dump: `LightingController.state = Operational` on the powered cab, hull renders
  normally).
- ~~BZ docking behavior: `Dockable.OnDockingStart`/`OnUndockingComplete` (Seatruck into its
  moonpool) — lights off on dock, `EnableLightsOnUndocking` on undock~~ — VERIFIED on BZ
  through the live Seatruck Moonpool dock/undock cycle (see the BZ verification section).
- ~~Scanner-room camera drive on BZ from a real scanner room screen~~ — VERIFIED on BZ
  through the real screen click (see the BZ verification section).
- ~~Change Better Lights options from the Nautilus Mods pane in-game~~ — VERIFIED on BZ with
  real pointer input (Seatruck pane): slider OnChange fires, the Advanced RGB color picker
  propagates to the live `Light.color`. The keybinds sit outside the per-vehicle panes on both
  games: on SN they are the game's Mod Input tab rows (custom `GameInput` buttons — VERIFIED
  live: rebind from the tab, toggle on the rebound key while piloting, persistence across
  restart); on BZ they are the "Better Lights" rows of the "Better Subnautica - Input" page
  (rebind write-back verified live, the in-menu key capture stays a user check); the SN pane
  slider/color flows remain.
