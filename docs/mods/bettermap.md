# BetterMap — single-player feature verification

Integration shim for the external **SubnauticaMap** mod. The single-player surface is one
Harmony patch that reroutes SubnauticaMap's on-screen logging to console-only output; the map
save/ping controllers are multiplayer components and live outside this branch (they arrive
with the multiplayer commit series). Verified live on both games from branch `sp-verify`
(DLLs built at the branch tip, SN.STABLE 19 warnings / 0 errors, BZ.STABLE 31 warnings /
0 errors), through the BetterRemote bridge with screenshot evidence. Old-feature baseline:
`origin/refactor` (BetterMap does not exist at `d51c6e9`; the mod entered the tree during the
BepInEx era).

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

The deployed SP `BetterMap.dll` contains exactly three mod types on both games — `Plugin`,
`MyPluginInfo`, `LoggerPrintPatch` (verified with `ilspycmd -l c` on the deployed DLLs). The
multiplayer components (`SaveController`, `PingController`, `FakeStorage`, the
Controller/Server/Storage patches) are `#if MULTI`/`#if BELOWZERO_MULTI` code and sit in the
multiplayer commits after this branch point, so SP builds carry no trace of them.

## External-mod notes (not BetterMap issues)

- **SN ships a pre-existing `FileNotFoundException` for `lang\Italian.json`** raised from
  `SubnauticaMap.Lang.LoadLanguageFile` on save load ("Italian localization not found"): the
  SN release of the external mod lacks the Italian language file. BZ resolves its localization
  cleanly. SubnauticaMap keeps working after the exception (map loads, `Started` logged).

## Restored / dropped

- **Restored: nothing** — the SP surface is byte-identical to the old tree (`LoggerPatches.cs`
  unchanged; `Plugin.cs` equals the old file minus its `#if MULTI` blocks).
- **Dropped: nothing.** Every old single-player behavior survives.

## NEEDS-USER checklist

- None. The whole SP surface (one logger patch) is verified end-to-end via the bridge.

## Multiplayer verification (branch `mp-verify`)

The MP surface arrives with commit `9339a84` ("Added BetterMap multiplayer save and pings"):
in a botbenson session, SubnauticaMap's map I/O is rerouted to a per-server client save path
and a map save fires when the server disposes. Audited code-vs-intentions against the
`06a354e` experiment baseline (`docs/analysis/lights-and-map.md` §BetterMap) and the
decompiled botbenson sources (`D:\Projects\Subnautica\Botbenson\`); builds SN.MULTI
21 warnings / BZ.MULTI 33 warnings, 0 errors.

### Components and patches (BZ.MULTI)

- **`SaveController`** (attached to SubnauticaMap's `Controller` by the `Controller.Run`
  postfix): self-destroys without an active server session
  (`Network.Session.Current?.ServerId` empty — API verified,
  `Botbenson\Subnautica.API\...\NetworkUtility\Session.cs`). In a session it builds
  `SavePath = Paths.GetMultiplayerClientSavePath()` (verified, `Paths.cs:194`) and
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
  (signature verified, `Botbenson\Subnautica.Server\...\Server.cs:306`) — forces a final
  `PerformSave()` when the hosted server shuts down.
- **Bootstrap**: under BZ.MULTI, `Plugin.ApplyPatches` defers the patch pass to BetterNitrox's
  `NitroxLoaded` event; the `[BepInDependency]` on BetterNitrox holds in both MULTI flavors.
- SubnauticaMap symbols (`Controller.Run/ReloadMaps/isStarted`, `Storage.slot/userStorage`,
  `Map`/`Fogmap`, icon/note load-save members) are metadata-proven against both publicized
  DLLs (analysis §4); SubnauticaMap ships no sources, so build success is the signature check.

### Fixed in this audit

- **FIXED — SN.MULTI null-path `SaveController`**: at `06a354e` the save controller and its
  attach patch compiled only under `#if BELOWZERO_MULTI`; the port broadened the attach to
  `#if MULTI` while the Subnautica branch of `Awake` stayed a TODO placeholder, so on SN.MULTI
  the controller went live with `SavePath`/`MapPath` null — `new UserStoragePC(null)` +
  `CreateContainerAsync(null)` enqueue a `Path.Combine(null, …)` failure on the game's I/O
  worker thread, and the 10-second autosave drove SubnauticaMap's real SP storage through
  unrerouted `Storage` getters. `SaveController.Awake` now nulls the controller in the
  Subnautica branch (the same self-destroy path a session-less BZ launch takes), keeping
  SN.MULTI inert until the Nitrox-side implementation lands. `PingController` stays active in
  both MULTI flavors by design (icon rebuild is game-local and idempotent).

### Verified live (2026-07-06 two-client regression)

Hosted BZ session, host MatrixDJ96 + client WDESKTOP-MATRIX over LAN; every check below holds
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
