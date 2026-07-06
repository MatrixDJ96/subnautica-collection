# BetterNitrox — multiplayer verification

The multiplayer compatibility layer: it lets the BepInEx/Nautilus plugin suite coexist with
the botbenson Below Zero multiplayer mod (internal namespace `Subnautica.*`, injected by
overwriting the game's `Assembly-CSharp.dll`). Compiled only in the MULTI configurations;
every patch file is `#if BELOWZERO_MULTI`. Audited code-vs-intentions on branch `mp-verify`
(= `develop` `732dbe5`) across the commit series `1b3f956..76d1d3c`, cross-checked against the
decompiled botbenson sources (`D:\Projects\Subnautica\Botbenson\`, build of 2026-07-03) and
the botbenson-patched game tree (`D:\Projects\Subnautica\BelowZero\`). Live evidence: the
2026-07-06 two-client environment smoke (BZ.MULTI 33 warnings / 0 errors, host MatrixDJ96 +
client WDESKTOP-MATRIX over LAN) plus the 2026-07-05 first-pass verifications.

## Build layer (`1b3f956`)

`Directory.Build.props` defines the `SN.MULTI`/`BZ.MULTI` configurations (`MULTI`,
`SUBNAUTICA_MULTI`/`BELOWZERO_MULTI`, `IsMultiplayer=true`). Under
`IsBelowZero && IsMultiplayer`, `Directory.Build.targets:23-28` references the botbenson
assemblies (`$(NitroxDir)\Subnautica.*.dll`, `Dependencies\*.dll`, `Plugins\Subnautica.*.dll`);
`Directory.Build.targets:32` gives every other plugin a `ProjectReference` to `BetterNitrox` in
the MULTI flavors, and `BetterLights/Plugin.cs` adds the matching `[BepInDependency]` under
`#if MULTI` so BepInEx loads the bridge first.

## Plugin bootstrap (`Plugin.cs`, `f88d6f9`)

`ApplyPatches()` subscribes the deferred patch pass (`PatchAll` + `PostPatchAll` + the
light-sync bridge init) to the `NitroxLoaded` event, then runs `LoadNitrox()`. On Below Zero,
`LoadNitrox()` byte-loads `Subnautica.Loader.dll`/`Subnautica.API.dll` from the `.botbenson`
install, invokes the private `Loader.LoadDependencies()`, pre-loads every DLL in
`Game\Plugins`, applies the `[PrePatch]` classes, invokes `Loader.Run()` (the real plugin
activation), and marks `SubnauticaBootstrap.IsLoaded = true` so the game's own
`StartScreen`-driven bootstrap becomes a no-op. The manual `Game\Plugins` pre-load only warms
the assembly cache — `Loader.Run()` performs the activating load
(`docs/analysis/core-and-multiplayer.md` §2.2). The non-BZ path is a passthrough that raises
`NitroxLoaded` immediately, so `MULTI`-aware plugins work unchanged in single-player;
Subnautica-side Nitrox support is the open `Plugin.cs:11` TODO (P2 in
`docs/analysis/porting-status.md`).

Verified live (2026-07-06 smoke): `Loading Subnautica Bootstrap...` → `Subnautica Bootstrap
Patched!` on both machines, all 11 plugins load, zero errors.

## Pre-patches (`[PrePatch]`, applied before `Loader.Run()`)

- **`StartScreenPatches`** — Prefix skip of `StartScreen.TryToShowDisclaimer`, botbenson's
  anti-tamper walk that recurses forever when it sees BepInEx's `winhttp.dll` doorstop
  (main-thread `StackOverflowException` = black screen before the menu). Verified live: the
  patch logs on startup and the menu renders.
- **`ToolsPatches`** — forces `Tools.IsBepinexInstalled()` to `false`
  (`Botbenson\Subnautica.API\...\Tools.cs` checks for `doorstop_config.ini`/`winhttp.dll`/a
  `BepInEx` folder and refuses to run otherwise).
- **`LoggerPatches`** — Prefix on `Log.Send(string, LogLevel)`/`Log.SendRaw(string)`
  redirects every botbenson log line into the BepInEx log (`Info`/`Warn`/`Error` mapped 1:1).
  Verified live: botbenson lines (server start, "si è connesso") appear in the BepInEx log.

## Machine-derived identity (`IdentityPatches.cs`, `76d1d3c`)

`Tools.GetLoggedId()` returns `null` when the platform reports user id `0` or none
(`Botbenson\...\Tools.cs:195-203` — Steam offline on a joining client), and the server's
`JoiningProcessor` disconnects a client whose `UserName`/`UserId` is null, whose name matches
an existing player, or whose id hash is already connected
(`Botbenson\Subnautica.Server\...\JoiningProcessor.cs:24-50`). `MachineIdentity` thus
activates only when the platform id is missing: postfixes on `Tools.GetLoggedInName`/
`GetLoggedId` then return `<name>@<machine>` and a stable SteamID64-shaped id derived from
`MD5(deviceUniqueIdentifier/user)`; the `Probing` flag keeps the `Active` check (which itself
calls `GetLoggedId()`) from being rewritten by its own postfix. With a platform id available
the identity passes through untouched — verified live on the host
(`GetLoggedInName` = `MatrixDJ96`).

Verified live (2026-07-06): the offline client joins as `UnityEditorPlayer@WDESKTOP-MATRIX` /
`76561201039306588` and `JoiningProcessor` accepts it (host log "si è connesso").

## Light-sync (`4fa99a7`)

BetterLights controllers apply light state directly (`lightsParent.SetActive`,
`lightsActive` field writes) — vanilla `ToggleLights.SetLightsActive` is never called, so
botbenson's own send patch on that method observes nothing. The bridge closes the gap:

- **Send**: `AbstractToggleLightsController.SetLightsActive` raises
  `ToggleLightsEvents.NotifyLightsChanged` on every real state change (core
  `ToggleLightsEvents`, guarded so forced re-applies of the same state stay silent).
  `ToggleLightsBridge` (initialized from `NitroxLoaded`) listens, gates on
  `Network.IsMultiplayerActive`, and for SeaTruck controllers raises
  `Handlers.Vehicle.OnLightChanged(new LightChangedEventArgs(identityId, active,
  TechType.SeaTruck))` — botbenson's `LightProcessor.OnVehicleLightChanged` then sends the
  `VehicleLight` packet only when the local player owns the entity (`entity.IsMine`,
  `Botbenson\Subnautica.Client\...\LightProcessor.cs:65-80`). All signatures match the
  decompiled sources (`Handlers.Vehicle.OnLightChanged(LightChangedEventArgs)`,
  ctor `(string, bool, TechType)`, `Network.IsMultiplayerActive`, `GetIdentityId()`).
- **Receive**: incoming packets land in `LightProcessor.OnProcessCompleted`.
  `LightProcessorPatches` prefixes that caller and routes the MapRoomCamera AND SeaTruck cases
  through the local `IToggleLightsController` directly — the SeaTruck hop
  `ZeroGame.SetLightsActive(SeaTruckLights, bool)` is a single field write (`ZeroGame.cs:247`)
  that the Mono JIT inlines into `OnProcessCompleted`, so a Harmony patch on the `ZeroGame`
  overload never fires from this call site; intercepting the caller is the reliable point. A
  module's `SeaTruckLights` carries no controller, so the SeaTruck branch bridges through the
  main cab via `SeaTruckSegment.GetHead`. Hoverbike receives ride
  `ZeroGame.SetLightsActive(ToggleLights, bool, bool)` (`ZeroGame.cs:233`, a full-bodied
  method that stays patchable) into `ZeroGamePatches`. The `ZeroGamePatches` SeaTruck-overload
  prefix still covers the non-inlined call sites (join/spawn initial sync). With no controller
  present the patches log a warning and fall through to the vanilla behavior.

Verified live (2026-07-05 first pass, regressed 2026-07-06): SeaTruck light round-trip PASS in
both directions (ownership via `OnClickSteeringWheel`, `ToggleLightsBridge` →
`VehicleLight` packets; receive via `SetSeaTruckLightsActivePatch` →
`IToggleLightsController`).

## Audit result (code vs original intentions)

Every Harmony target and botbenson API consumed by BetterNitrox matches the decompiled
build; the full per-target table for the `f88d6f9` surface lives in
`docs/analysis/core-and-multiplayer.md` §3, and the `4fa99a7`/`76d1d3c` additions are verified
above. All changes since the `06a354e` baseline are additive (analysis §2.6) — nothing from
the original multiplayer experiment was dropped, and no fix was needed in this audit. The
breakage risk stays external: every target is botbenson-owned and moves with its next release.

## Coverage notes (full-suite regression, 2026-07-06 two-client session)

Suite-wide result: the full Harmony patch set is identical on host and client (88 patched
methods, diffed from the two BepInEx logs) with zero mod errors on either side — the only
log noise is vanilla (`CellManager.RegisterGlobalEntity` NREs on stray `Signal` entities
during world load and inactive-plant coroutine lines at scene teardown, identical on both
machines). Machine-derived identity and the SeaTruck light round-trip hold in the same
session.

- **Hoverbike — gap confirmed live**: a BetterLights-driven toggle flips the local state and
  sends NO `VehicleLight` packet (the bridge forwards SeaTruck only; the native send patch
  watches the vanilla `ToggleLights.SetLightsActive` that BetterLights avoids), so the light
  stays local to the toggling client. The receive side stays code-verified (the
  `ToggleLights` overload patch is registered): a CraftData-spawned vehicle is local-only in
  botbenson (never replicated to the other client), so a cross-client observation needs a
  server-known Hoverbike. Bridging Hoverbike sends is DONE in Part 2 (send-bridge branch,
  two-client PASS — see the Part 2 parity round below).
- **MapRoomCamera**: botbenson syncs it outside the patched overloads — send from raw input
  in a `MapRoomCamera.Update` prefix, receive via direct `lightsParent.SetActive` — so the
  BetterLights controller state can diverge on the receiving client. Checked live on the MP
  creative world (2026-07-08, scanner room + docked drones): see the creative-world section
  below — the send leg works, the receive leg was silently inert until the Part 2
  `LightProcessorPatches` fix (two-client PASS — see the Part 2 parity round below).
- **SeaTruck native send coexistence — no duplicates**: with both prefixes live
  (botbenson's `SeaTruckLights_Update` send + BetterLights' `SeaTruckLights.Update`
  suppression, `SeatruckToggleLightsController.Update` mirroring `lightsActive` into the
  vanilla component each frame), one light change produces exactly ONE `VehicleLight` packet
  in each direction (host→client and client→host round-trip counted in the server log). The
  raw right-hand toggle while piloted (real RMB input) is the remaining NEEDS-USER residue.

## Coverage notes (creative-world round, 2026-07-08 two-client session)

Run on the creative MP world (scanner room + 2 docked drones, Seatruck Moonpool Expansion,
classic Moonpool with a docked Exosuit, seatruck, hoverbike — all near spawn). Suite smoke
stays clean: zero `Better*`-tagged errors on either side across the whole round.

- **MapRoomCamera drive syncs; its light toggle does not reach the client.** Controlling a
  drone through the real screen path (virtual left click on the screen's `input` hand
  target) mirrors the drone transform EXACTLY on the client (a ~20 m drive lands on
  identical coordinates). A `Mouse1` toggle while controlling flips the host state and
  produces a `VehicleLight` packet the server accepts, enriches with `TechType` from its
  dynamic-entity registry and relays (`Packet Received`/`Packet Sent` pairs in the server
  log), and the client-side chain exists end-to-end (`ProcessType.VehicleLight` router
  entry, `TechType.MapRoomCamera` case doing `lightsParent.SetActive`, matching
  `UniqueIdentifier.id` on both sides) — yet the receiving client never applies it
  (`lightsParent.activeSelf`, `lightState` and the controller's `LightsActive` all stay
  False for 21+ s, with no `Not Found` and no queue exception in its log). The break sits
  silently inside the desktop client's receive path; same observable family as the
  Hoverbike gap above. FIXED in Part 2 by routing the MapRoomCamera receive through the mod
  controller (`LightProcessorPatches`, two-client PASS — see the Part 2 parity round below).
- **Seatruck Moonpool Expansion dock AND undock propagate through the real paths.** The
  undock event exists only in botbenson's `Entering.SeaTruckOnClickSteeringWheel` prefix
  and fires only when `dockable.isDocked && bay != null` AT CLICK TIME, with
  `AllowedToUseSteeringWheel()` requiring `Player.main.IsInside()` on the main cab. The
  working MP undock is: enter the dock-room interior (the `entranceTriggerDocked`
  `BaseEntranceTrigger` fires on `OnTriggerStay`, so standing in its volume runs the honest
  `EnterInterior` path), then `OnClickSteeringWheel` on the still-docked cab —
  `isFullyDocked` drops to False on BOTH sides within 4 s, the cab exits to identical
  coordinates on both sides, and the host rigidbody is released (`isKinematic` False).
  Calling `SetPiloting(true)` first instead starts the LOCAL undock, the click lands after
  release, falls into the `VehicleEntering` branch and NO undock packet exists — the host
  is left half-undocked (`isKinematic` stuck True: `bay.OnUndockingComplete` never runs)
  while the client stays docked. That sequencing error — not only the warp-in — is what
  kept the delta-round undock local. Recovery from the desync: menu-quit both sides (the
  server never saw an undock, so its authoritative save still holds the dock) and
  re-host/rejoin. The dock leg: driving the cab into the bay trigger with `LookAt` + short
  `W` pulses docks through the full vanilla chain and propagates `isFullyDocked`/`isDocked`
  True to the client within 4 s, identical dock coordinates on both sides, repair invoke
  armed on the exact `Expansion/Launchbay_cinematic/DockBottom`; a second wheel-click
  undock from that driven dock propagates again within 4 s, proving the drive-in leaves a
  complete docking timeline.

## Coverage notes (Part 2 parity round, 2026-07-08 two-client session)

Two-client BZ.MULTI round on the creative MP world confirming the Part 2 changes over the
network (host MatrixDJ96 laptop + client WDESKTOP-MATRIX, identical hash-verified binaries).
Suite smoke clean: zero `Better*`-tagged errors on either side.

- **MapRoomCamera receive-leg — CLOSED (PASS, bidirectional).** The 2026-07-08 creative-world
  gap (send worked, receive silently inert) is fixed by `LightProcessorPatches.cs`. Driving a
  drone from the host (`MapRoomScreen.OnHandClick` → `ControlCamera`, ownership taken) and
  toggling its light with a raw `RightHand` press (botbenson's `MapRoomCamera.Update` send)
  now REACHES the client: the receiving client logs `[LightProcessor.OnProcessCompletedPatch]
  MapRoomCamera IToggleLightsController called (True/False)`, its
  `MapRoomCameraToggleLightsController` runs `SetLightsActive`, and `lightsParent` flips to
  match (`FIX … lightsParent False->True` then `True->False` on the OFF toggle) — the same
  networked drone id on both sides, both directions.
- **Hoverbike send-bridge — CLOSED (PASS, send + apply).** A mod-driven Hoverbike light toggle
  now raises the bridge's Hoverbike branch (`[ToggleLightsBridge] Hoverbike lights changed`
  → `Vehicle.OnLightChanged(…, TechType.Hoverbike)`) and, with the host owning a server-known
  Hoverbike, botbenson sends one `VehicleLight` packet (server `Packet Received`/`Packet Sent`).
  The client applies it through the existing `ZeroGame.SetLightsActivePatch` receive
  (`IToggleLightsController called (False)` → `FIX lightsParent True->False`,
  `LightsActive` follows). Ownership path: the world Hoverbike is docked to a hoverpad, so its
  `EnterTrigger` colliders are disabled — `UndockFromHoverpad()` re-enables them, then
  `HoverbikeEnterTrigger.OnHandClick` fires botbenson's `Vehicle.OnEntering` and the server
  transfers ownership. The scope-6 "Hoverbike toggle not synchronized" gap is closed.
- **SeaTruck piloted-RMB coexistence (§B.8, re-measured 2026-07-10).** With the SeaTruck
  piloted (ownership via `OnClickSteeringWheel`) and the mod's SeaTruck lights key bound to
  `MouseButtonRight` (the shipped default), one real `RightHand` press produces TWO
  `VehicleLight` packets with the IDENTICAL payload: the mod toggle (`CheckLightToggle` →
  controller → `[ToggleLightsBridge] SeaTruck` send) and botbenson's native
  `SeaTruckLights.Update` prefix send, both computed from the same pre-press state. The
  receiving client applies the same value twice — no flicker, net state correct on both
  sides.
- **SeaTruck light receive — component-lifecycle race, fixed in `SeatruckToggleLightsController`.**
  botbenson receives a `VehicleLight` packet, queues it (`LightProcessor.OnDataReceived` →
  `Entity.ProcessToQueue`) and runs `OnProcessCompleted` as soon as
  `Network.Identifier.GetComponentByGameObject<SeaTruckLights>(uid)` resolves — i.e. as soon as the
  vanilla `SeaTruckLights` exists on the streamed-in entity. That moment is BEFORE the
  `SeaTruckSegment.Start` hook attaches `SeatruckToggleLightsController`, so the receive patch's
  `GetComponentInParent<IToggleLightsController>` finds no controller and falls through to
  botbenson's `ZeroGame.SetLightsActive(SeaTruckLights, isActive)`, which sets only
  `seaTruckLights.lightsActive`. When the controller then attaches with its default
  `lightsActive = false`, `Start` forces the flood light back off and the received state is lost.
  A two-client round (host toggle → desktop receive) confirmed this against the receiving client:
  the received `SeaTruckLights` is the visible cab's own instance (same `goId`/`lightsId`), a
  single core assembly is loaded (`BetterSubnautica`×1, `BetterLights`×1 — not an assembly-identity
  or stray-instance problem), and `GetComponents` on the received object at receive time lists the
  39 game components with none of the BetterLights controllers yet attached. The fix: on attach,
  `SeatruckToggleLightsController` adopts `seaTruckLights.lightsActive` (the value botbenson's
  fallthrough already wrote) as its initial state, so `Start` preserves the received lights instead
  of resetting them. This closes both orderings — controller-after-sync (adopt) and
  controller-before-sync (the receive resolves the controller directly) — and keeps host and client
  consistent. `Network.Identifier.GetComponentByGameObject<T>` = `uid.GetComponentInChildren<T>()`
  (`Botbenson\...\NetworkUtility\Identifier.cs:86`).
- **SeaTruck LIVE toggle receive — JIT inlining, fixed in `LightProcessorPatches` (2026-07-10
  two-client round).** The adopt-on-attach fix covers the join/spawn ordering, but a live
  toggle received AFTER the controller attached still left the receiving client dark: the
  packet arrives (`OnDataReceived` handled) and the queue runs `OnProcessCompleted`, whose
  compiled body carries `ZeroGame.SetLightsActive(SeaTruckLights, bool)` INLINED by the Mono
  JIT (the method is a single field write), so the `ZeroGamePatches` prefix on that overload
  never fires from this call site and only the raw `seaTruckLights.lightsActive` field
  changes — controller state and `floodLight` stay put. Instrumented proof on the receiving
  desktop: `TryGetIdentifier=True`, the uid resolves to the visible cab, and
  `GetComponentInChildren<SeaTruckLights>` finds the component, yet no
  `SetSeaTruckLightsActivePatch` line follows the process. The fix routes the SeaTruck case
  through the mod controller at the caller (`LightProcessorPatches` prefix on
  `OnProcessCompleted`, same pattern as the MapRoomCamera case, `GetHead` fallback included).
  Verified live in both transitions: host ON → desktop logs `[LightProcessor.
  OnProcessCompletedPatch] SeaTruck IToggleLightsController called (True)`,
  `LightsActive=True`, `floodLight.activeSelf=True`; host OFF → the same chain lands False on
  both, host and client states identical throughout.

## Fold-time notes

- `docs/analysis/core-and-multiplayer.md` §2 predates `4fa99a7`/`76d1d3c`: it covers a single
  `ZeroGamePatches` class (the second overload patch, `ToggleLightsBridge`,
  `ToggleLightsEvents` and `IdentityPatches` are absent) and its decompiled-source citations
  use the pre-reorganization root (`D:\Projects\Subnautica\BelowZero\Subnautica.API\...`; the
  botbenson projects live under `D:\Projects\Subnautica\Botbenson\` since 2026-07-05). The doc
  is a dated snapshot; this file carries the current-state coverage.
