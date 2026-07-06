# BetterNitrox — multiplayer verification

The multiplayer compatibility layer: it lets the BepInEx/Nautilus plugin suite coexist with
the botbenson Below Zero multiplayer mod (internal namespace `Subnautica.*`, injected by
overwriting the game's `Assembly-CSharp.dll`). Compiled only in the MULTI configurations;
every patch file is `#if BELOWZERO_MULTI`. Audited code-vs-intentions against the decompiled
botbenson sources and the botbenson-patched Below Zero game tree. Live evidence: two-client
BZ.MULTI rounds (a host and a joining client over LAN).

## Build layer

`Directory.Build.props` defines the `SN.MULTI`/`BZ.MULTI` configurations (`MULTI`,
`SUBNAUTICA_MULTI`/`BELOWZERO_MULTI`, `IsMultiplayer=true`). Under
`IsBelowZero && IsMultiplayer`, `Directory.Build.targets` references the botbenson
assemblies (`$(NitroxDir)\Subnautica.*.dll`, `Dependencies\*.dll`, `Plugins\Subnautica.*.dll`);
`Directory.Build.targets` gives every other plugin a `ProjectReference` to `BetterNitrox` in
the MULTI flavors, and `BetterLights/Plugin.cs` and `BetterMap/Plugin.cs` add the matching
`[BepInDependency]` under `#if MULTI` so BepInEx loads the bridge first.

## Plugin bootstrap (`Plugin.cs`)

`ApplyPatches()` subscribes the deferred patch pass (`PatchAll` + `PostPatchAll` + the
light-sync bridge init) to the `NitroxLoaded` event, then runs `LoadNitrox()`; the
subscription comes first because `LoadNitrox()` raises the event synchronously on success.
On Below Zero, `LoadNitrox()` byte-loads `Subnautica.Loader.dll`/`Subnautica.API.dll` from
the `.botbenson` install (`Assembly.Load(File.ReadAllBytes(…))`, which keeps the files
unlocked), invokes the private `Loader.LoadDependencies()` (every DLL under
`Game\Dependencies` except `DiscordRPCNativeNamedPipe` and `Subnautica.API`), pre-loads every
DLL in `Game\Plugins`, applies the `[PrePatch]` classes, invokes `Loader.Run()` (which runs
`LoadDependencies()` again, then `LoadPlugins()`, the real plugin activation) and marks
`SubnauticaBootstrap.IsLoaded = true` so the game's own `StartScreen`-driven bootstrap
becomes a no-op. The manual `Game\Plugins` pre-load only warms the assembly cache.
`SubnauticaBootstrap` is a class botbenson injects into the game's `Assembly-CSharp.dll`;
its `IsLoaded`, `GetLoaderPath` and `GetAPIFilePath` are private and reachable through the
publicizer. When `IsLoaded` is already true, a bootstrap file is missing, the `Loader` type
does not resolve or a bootstrap call throws, `LoadNitrox()` logs a warning or error and
returns without raising `NitroxLoaded`, so the deferred patch pass does not run (a failed
`Game\Plugins` pre-load is only logged). The SN.MULTI path (`#else`; BetterNitrox builds only
in the two MULTI configurations) applies the `[PrePatch]` classes and raises `NitroxLoaded`
immediately, so `MULTI`-aware plugins work unchanged there; Subnautica-side Nitrox support
is the open `Plugin.cs` TODO.

Verified live (two-client smoke): `Loading Subnautica Bootstrap...` → `Subnautica Bootstrap
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
  redirects every botbenson log line into the BepInEx log (`Info`/`Warn`/`Error` mapped 1:1;
  `SendRaw` lines land as warnings prefixed `[Log.SendRaw]`).
  Verified live: botbenson lines (server start, "si è connesso") appear in the BepInEx log.

## Machine-derived identity (`IdentityPatches.cs`)

`Tools.GetLoggedId()` returns `null` when the platform reports user id `0` or none
(`Botbenson\...\Tools.cs` — Steam offline on a joining client), and the server's
`JoiningProcessor` disconnects a client whose `UserName`/`UserId` is null, whose name matches
an existing player, or whose id hash is already connected
(`Botbenson\Subnautica.Server\...\JoiningProcessor.cs`). `MachineIdentity` thus
activates only when the platform id is missing: postfixes on `Tools.GetLoggedInName`/
`GetLoggedId` then return `<name>@<machine>` and a stable SteamID64-shaped id derived from
`MD5(deviceUniqueIdentifier/user)`; the `Probing` flag keeps the `Active` check (which itself
calls `GetLoggedId()`) from being rewritten by its own postfix. With a platform id available
the identity passes through untouched — verified live on the host
(`GetLoggedInName` = `MatrixDJ96`).

Verified live: the offline client joins as `UnityEditorPlayer@WDESKTOP-MATRIX` /
`76561201039306588` and `JoiningProcessor` accepts it (host log "si è connesso").

## Light-sync

BetterLights controllers apply light state directly (`lightsParent.SetActive`,
`lightsActive` field writes) — vanilla `ToggleLights.SetLightsActive` is never called, so
botbenson's own send patch on that method observes nothing. The bridge closes the gap:

- **Send**: `AbstractToggleLightsController.SetLightsActive` raises
  `ToggleLightsEvents.NotifyLightsChanged` on every real state change (core
  `ToggleLightsEvents`, guarded so forced re-applies of the same state stay silent).
  `ToggleLightsBridge` (initialized from `NitroxLoaded`) listens, gates on
  `Network.IsMultiplayerActive`, and for SeaTruck and Hoverbike controllers raises
  `Handlers.Vehicle.OnLightChanged(new LightChangedEventArgs(identityId, active, techType))`
  (`TechType.SeaTruck` / `TechType.Hoverbike`) — botbenson's
  `LightProcessor.OnVehicleLightChanged` then sends the `VehicleLight` packet only when the
  local player owns the entity (`entity.IsMine`,
  `Botbenson\Subnautica.Client\...\LightProcessor.cs`). All signatures match the
  decompiled sources (`Handlers.Vehicle.OnLightChanged(LightChangedEventArgs)`,
  ctor `(string, bool, TechType)`, `Network.IsMultiplayerActive`, `GetIdentityId()`).
- **Receive**: incoming packets land in `LightProcessor.OnProcessCompleted`.
  `LightProcessorPatches` prefixes that caller and routes the MapRoomCamera AND SeaTruck cases
  through the local `IToggleLightsController` directly — the SeaTruck hop
  `ZeroGame.SetLightsActive(SeaTruckLights, bool)` is a single field write (`ZeroGame.cs`)
  that the Mono JIT inlines into `OnProcessCompleted`, so a Harmony patch on the `ZeroGame`
  overload never fires from this call site; intercepting the caller is the reliable point. A
  module's `SeaTruckLights` carries no controller, so the SeaTruck branch bridges through the
  main cab via `SeaTruckSegment.GetHead`. Hoverbike receives ride
  `ZeroGame.SetLightsActive(ToggleLights, bool, bool)` (`ZeroGame.cs`, a full-bodied
  method that stays patchable) into `ZeroGamePatches`. The `ZeroGamePatches` SeaTruck-overload
  prefix still covers the non-inlined call sites (join/spawn initial sync). With no controller
  present the patches log a warning and fall through to the vanilla behavior.

Verified live (two-client rounds): SeaTruck light round-trip PASS in both directions
(ownership via `OnClickSteeringWheel`, `ToggleLightsBridge` → `VehicleLight` packets; live
receives via the `LightProcessorPatches` caller prefix → `IToggleLightsController`).

## Audit result (code vs original intentions)

Every Harmony target and botbenson API consumed by BetterNitrox matches the decompiled
botbenson build and the botbenson-patched Below Zero game tree:

| Patch | Target | Decompiled source |
|---|---|---|
| `StartScreenPatches.cs` | `StartScreen.TryToShowDisclaimer` (private instance, no parameters) | `Assembly-CSharp/StartScreen.cs` |
| `ToolsPatches.cs` | `Subnautica.API.Features.Tools.IsBepinexInstalled` (public static, no parameters) | `Subnautica.API/Subnautica.API.Features/Tools.cs` |
| `IdentityPatches.cs` | `Tools.GetLoggedInName()` / `Tools.GetLoggedId()` (public static, no parameters) | `Subnautica.API/Subnautica.API.Features/Tools.cs` |
| `LoggerPatches.cs` (`LogSendPatch`) | `Subnautica.API.Features.Log.Send(string, LogLevel)` | `Subnautica.API/Subnautica.API.Features/Log.cs` |
| `LoggerPatches.cs` (`LogSendRawPatch`) | `Subnautica.API.Features.Log.SendRaw(string)` | `Subnautica.API/Subnautica.API.Features/Log.cs` |
| `ZeroGamePatches.cs` (`ZeroGameSetLightsActivePatch`) | `ZeroGame.SetLightsActive(ToggleLights, bool, bool)` | `Subnautica.API/Subnautica.API.Features/ZeroGame.cs` |
| `ZeroGamePatches.cs` (`ZeroGameSetSeaTruckLightsActivePatch`) | `ZeroGame.SetLightsActive(SeaTruckLights, bool)` | `Subnautica.API/Subnautica.API.Features/ZeroGame.cs` |
| `LightProcessorPatches.cs` | `Subnautica.Client.Synchronizations.Processors.Vehicle.LightProcessor.OnProcessCompleted(ItemQueueProcess)` (public instance) | `Subnautica.Client/Subnautica.Client.Synchronizations.Processors.Vehicle/LightProcessor.cs` |
| `Plugin.cs` (`LoadNitrox`) | `Subnautica.Loader.Loader` (`LoadDependencies` private static, `Run` public static) | `Subnautica.Loader/Subnautica.Loader/Loader.cs` |
| `Plugin.cs` (`LoadNitrox`) | `SubnauticaBootstrap.IsLoaded` / `GetLoaderPath` / `GetAPIFilePath` (game-injected, private, publicized) | `Assembly-CSharp/SubnauticaBootstrap.cs` |
| `Plugin.cs` (`Game\Plugins` load loop) | `Subnautica.API.Features.Paths.GetGamePluginsPath()` | `Subnautica.API/Subnautica.API.Features/Paths.cs` |

The breakage risk stays external: every target is botbenson-owned and moves with its next
release.

## Coverage notes (full-suite regression, two-client session)

Suite-wide result: the full Harmony patch set is identical on host and client (88 patched
methods, diffed from the two BepInEx logs) with zero mod errors on either side — the only
log noise is vanilla (`CellManager.RegisterGlobalEntity` NREs on stray `Signal` entities
during world load and inactive-plant coroutine lines at scene teardown, identical on both
machines). Machine-derived identity and the SeaTruck light round-trip hold in the same
session.

- **Hoverbike**: a BetterLights-driven toggle bypasses the vanilla
  `ToggleLights.SetLightsActive` that botbenson's native send patch watches, so the send
  goes through the bridge's Hoverbike branch (two-client PASS — see the Part 2 parity round
  below); the receive rides the `ZeroGame.SetLightsActive(ToggleLights, …)` overload patch.
  A CraftData-spawned vehicle is local-only in botbenson (never replicated to the other
  client), so a cross-client observation needs a server-known Hoverbike.
- **MapRoomCamera**: botbenson syncs it outside the patched overloads — send from raw input
  in a `MapRoomCamera.Update` prefix, receive via direct `lightsParent.SetActive` — so
  `LightProcessorPatches` routes the receive through the BetterLights controller (see the
  creative-world section and the Part 2 parity round below).
- **SeaTruck native send coexistence — no duplicates**: with both prefixes live
  (botbenson's `SeaTruckLights_Update` send + BetterLights' `SeaTruckLights.Update`
  suppression, `SeatruckToggleLightsController.Update` mirroring `lightsActive` into the
  vanilla component each frame), one light change produces exactly ONE `VehicleLight` packet
  in each direction (host→client and client→host round-trip counted in the server log). The
  raw right-hand toggle while piloted is measured in the Part 2 parity round below.

## Coverage notes (creative-world round, two-client session)

Run on the creative MP world (scanner room + 2 docked drones, Seatruck Moonpool Expansion,
classic Moonpool with a docked Exosuit, seatruck, hoverbike — all near spawn). Suite smoke
stays clean: zero `Better*`-tagged errors on either side across the whole round.

- **MapRoomCamera drive syncs; its light toggle did not reach the client before the Part 2
  fix.** Controlling a
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

## Coverage notes (Part 2 parity round, two-client session)

Two-client BZ.MULTI round on the creative MP world confirming the Part 2 changes over the
network (host MatrixDJ96 laptop + client WDESKTOP-MATRIX, identical hash-verified binaries).
Suite smoke clean: zero `Better*`-tagged errors on either side.

- **MapRoomCamera receive-leg — CLOSED (PASS, bidirectional).** The creative-world
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
  transfers ownership. The "Hoverbike toggle not synchronized" gap is closed.
- **SeaTruck piloted-RMB coexistence.** With the SeaTruck
  piloted (ownership via `OnClickSteeringWheel`) and the mod's SeaTruck lights key bound to
  `MouseButtonRight` (the shipped default), one real `RightHand` press produces TWO
  `VehicleLight` packets with the IDENTICAL payload: the mod toggle (the controller's
  `Update` key check → `SetLightsActive` → `[ToggleLightsBridge] SeaTruck` send) and
  botbenson's native `SeaTruckLights.Update` prefix send, both computed from the same
  pre-press state. The
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
  (`Botbenson\...\NetworkUtility\Identifier.cs`).
- **SeaTruck LIVE toggle receive — JIT inlining, fixed in `LightProcessorPatches`.** The
  adopt-on-attach fix covers the join/spawn ordering, but a live
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
