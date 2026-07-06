# BetterMap — single-player feature verification

Integration shim for the external **SubnauticaMap** mod. The single-player surface is one
Harmony patch that reroutes SubnauticaMap's on-screen logging to console-only output; the map
save/ping controllers serve multiplayer sessions only (see "Multiplayer verification").
Verified live on both games through the BetterRemote bridge with screenshot evidence. BetterMap
does not exist at the old-feature baseline `d51c6e9`; the mod entered the tree in the BepInEx
era.

## Plugin bootstrap

`Plugin.cs` (`[BepInPlugin]`, `Plugin : SubnauticaPlugin`) with
`[BepInDependency(BetterSubnautica.MyPluginInfo.PLUGIN_GUID)]` — the dependency is original to
the mod (the old `Plugin.cs` already declared it). `Using.cs` exposes the static `Core`
instance unqualified. Verified: both games log `Plugin BetterMap v0.0.3.7 is Awake!` followed
by exactly one patch line (`Patched DMD<SubnauticaMap.Logger::Print>`), zero errors.

## Build wiring (`BetterMap.csproj`)

The project references the external mod assembly per game and publicizes it:
`<Publicize Include="SubnauticaMap"/>` for SN, `<Publicize Include="SubnauticaMap_BZ"/>` for
BZ. The publicizer is functional on BZ: `SubnauticaMap.Logger` is `internal` in
`SubnauticaMap_BZ.dll`, so `typeof(Logger)` compiles only against the publicized reference
(on SN the class is `public`; the same csproj shape covers both). The assemblies resolve from
the machine-local `Dependencies\<Configuration>` directories. Installed external-mod versions:
SubnauticaMap 1.5.12 (SN), SubnauticaMap_BZ 1.3.0 (BZ).

## Patches (`LoggerPatches.cs`)

- **`LoggerPrintPatch` — prefix on `SubnauticaMap.Logger.Print`, both games.** Vanilla
  `Print(text)` writes `[SubnauticaMap] <text>` to the console AND raises an on-screen toast
  via `ErrorMessage.AddMessage`. The prefix calls `Logger.Write(text)` (console line only) and
  returns `false`, so SubnauticaMap keeps its log trail but stops spamming the HUD.
  `Logger.Write` and `Logger.Show` stay unpatched (`Show` is console-only in vanilla despite
  the name).

  Verified live on BOTH games with a control experiment: back-to-back
  `Logger.Print("SUPPRESSED-TOAST…")` + `ErrorMessage.AddMessage("VISIBLE-CONTROL-TOAST…")`
  bridge invokes, then a same-frame screenshot. Only the control toast renders on screen while
  `[SubnauticaMap] SUPPRESSED-TOAST…` appears in the BepInEx log — the suppression is the
  patch's doing, the detection path provably works. In-game `Print` callers are rare
  ("Unable to make a new save while saving is in progress", "Unable to load while saving");
  routine SubnauticaMap traffic (`Save`, `Started`, `Map switched to …`) already uses `Write`
  and lands in both games' logs untouched.

## Single-player build surface

The SP `BetterMap.dll` applies one patch, `LoggerPrintPatch`. `SaveController`,
`PingController` and `FakeStorage` carry no compile guard and are built into every
configuration, but only the `#if MULTI` `Controller.Run` postfix attaches the two controllers
(`SaveController` creates the `FakeStorage`), and the `Storage`/`Server` patches are
`#if BELOWZERO_MULTI`, so SP builds never instantiate them.

## External-mod notes (not BetterMap issues)

- **SN ships a pre-existing `FileNotFoundException` for `lang\Italian.json`** raised from
  `SubnauticaMap.Lang.LoadLanguageFile` on save load ("Italian localization not found"): the
  SN release of the external mod lacks the Italian language file. BZ resolves its localization
  cleanly. SubnauticaMap keeps working after the exception (map loads, `Started` logged).

## Restored / dropped

- **Restored: nothing** — the SP surface (`LoggerPatches.cs`) is unchanged from the old tree.
- **Dropped: nothing.** Every old single-player behavior survives.

## NEEDS-USER checklist

- None. The whole SP surface (one logger patch) is verified end-to-end via the bridge.

## Multiplayer verification

The MP surface (multiplayer map save and pings): in a botbenson session, SubnauticaMap's map
I/O is rerouted to a per-server client save path and a map save fires when the server
disposes. Audited code-vs-intentions against the decompiled botbenson sources.

### Components and patches (BZ.MULTI)

- **`SaveController`** (attached to SubnauticaMap's `Controller` by the `Controller.Run`
  postfix): self-destroys without an active server session
  (`Network.Session.Current?.ServerId` empty — API verified,
  `Botbenson\Subnautica.API\...\NetworkUtility\Session.cs`). In a session it builds
  `SavePath = Paths.GetMultiplayerClientSavePath()` (verified, `Paths.cs`) and
  `MapPath = <serverId>\SubnauticaMap`, creates the container through its own
  `UserStoragePC`, waits for `Controller.isStarted`, then clears `Map`/`Fogmap` state and
  reloads icons/notes/maps; a 10-second `Timerwatch` autosaves
  (`Fogmap.SaveAll` + `SaveMapIcons` + `SaveNotes`) under a lock.
- **`FakeStorage`**: null-object `UserStorage` (every operation pre-completed as `NotFound`);
  served by the `Storage.userStorage` getter postfix until the controller's first load, so
  SubnauticaMap never touches the real container during startup. The `Storage.slot` getter
  postfix reroutes the slot to `MapPath` once started.
- **`PingController`** (+ `Controller.ReloadMaps` postfix): rebuilds the ping map icons from
  the game's `PingManager` on every map reload.
- **`ServerDisposePatch`**: Prefix on the botbenson `Server.Dispose(bool isEndGame)`
  (signature verified, `Botbenson\Subnautica.Server\...\Server.cs`) — forces a final
  `PerformSave()` when the hosted server shuts down.
- **Bootstrap**: under BZ.MULTI, `Plugin.ApplyPatches` defers the patch pass to BetterNitrox's
  `NitroxLoaded` event; the `[BepInDependency]` on BetterNitrox holds in both MULTI flavors.
- SubnauticaMap symbols (`Controller.Run/ReloadMaps/isStarted`, `Storage.slot/userStorage`,
  `Map`/`Fogmap`, icon/note load-save members) are metadata-proven against both publicized
  DLLs; SubnauticaMap ships no sources, so build success is the signature check.

### SN.MULTI guard

- **`SaveController` stays inert on SN.MULTI**: the attach postfix compiles under `#if MULTI`,
  but outside BZ.MULTI `SaveController.Awake` nulls the controller and self-destroys (the same
  path a session-less BZ launch takes) until the Nitrox-side implementation lands. A live
  controller there would run with `SavePath`/`MapPath` null — `new UserStoragePC(null)` +
  `CreateContainerAsync(null)` fail on the game's I/O worker thread and the autosave would
  drive SubnauticaMap's real SP storage through unrerouted `Storage` getters.
  `PingController` stays active in both MULTI flavors by design (icon rebuild is game-local
  and idempotent).

### Verified live (two-client regression)

Hosted BZ session, host + one client over LAN; every check below holds
on both machines.

- **Per-server save path**: `fogmap.bin`/`mapicons.bin`/`notes.bin` land under
  `<multiplayer client save>\<serverId>\SubnauticaMap` on each machine (the 10-second
  autosave writes on disk, mtimes tracked live) and reload at join from the files of the
  previous sessions ("Loading map..." → "Map loaded!" in both logs).
- **`FakeStorage` startup window**: the first map I/O in the log is the map-settings read at
  `Controller` start, and the SP slot container
  (`SNAppData\SavedGames\slot0000\SubnauticaMap`) takes no writes during the MP session.
- **Ping rebuild**: `Controller.ReloadMaps` with the second player connected logs
  "Reloading pings..." → "Pings reloaded!", zero errors.
- **`ServerDisposePatch`**: the host menu-quit logs "Server is disposing, performing save..."
  followed by a completed map save, before the client-disconnect teardown.
- **Session-less BZ.MULTI launch** (SP world from the same binary): `SaveController`
  self-destroys — the map object carries `Controller` + `PingController` only.
- **SN.MULTI smoke**: after a world load `SaveController` is absent from the map object, the
  log holds zero BetterMap save lines and zero map I/O errors (the only exception is the
  known external `Italian.json` FileNotFoundException).
