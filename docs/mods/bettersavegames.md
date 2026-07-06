# BetterSavegames — single-player feature verification

Quicksave (F5) and quickload (F9) on dedicated slots, interval autosave, optional autoload of
the most recent savegame from the main menu, slot-name labels in the load panel, and a
framerate unlock while a loading screen is active. Both games. Verified live on SN1 and BZ
from branch `sp-verify` (SN.STABLE 19 warnings / BZ.STABLE 31 warnings, 0 errors) through the
BetterRemote bridge with screenshot evidence. Old-feature baseline: `d51c6e9` (QModManager +
SMLHelper, `mod.json` `Game: "Both"`); current tree: BepInEx + Nautilus.

## Plugin bootstrap

`Plugin.cs` (`[BepInPlugin]`, `Plugin : SubnauticaPlugin`) replaces the QMod `Core.cs` entry
point; `Using.cs` exposes the static `Core` instance unqualified.
`[BepInDependency(BetterSubnautica.MyPluginInfo.PLUGIN_GUID)]` declares the dependency the old
`mod.json` carried in `VersionDependencies` (restored in this verification, matching the other
mods). Verified live on BOTH games: `Plugin BetterSavegames v0.0.3.7 is Awake!`, `[Settings]
Found 6 options`, BetterSubnautica loads first, all 6 patches applied (`IngameMenu::
SaveGameAsync`, `MainMenuLoadPanel::UpdateLoadButtonState`, `Player::Awake`,
`uGUI_MainMenu::Start`, `WaitScreen::Awake`, `WaitScreen::Update`), zero errors.

## Settings (`Settings.cs`)

Nautilus options pane "Better Savegames", persisted at
`BepInEx/config/BetterSavegames/config.json`. Six options, identical to the old SMLHelper set:
`Quicksave` (Keybind, F5), `Quickload` (Keybind, F9), `EnableAutosave` (Toggle, true),
`AutosaveInterval` (Slider 1-60 min, 10), `AutoloadLatestSavegame` (Toggle, false),
`MaximizeLoadingSpeed` (Toggle, true). Non-default values are applied through config edits +
relaunch during this verification (autosave interval 1 min, autoload on for the BZ launch
test) and restored to defaults afterwards. On SN `Quicksave`/`Quickload` are custom
`GameInput` buttons in the game's Mod Input tab ("Better Savegames" category, registered in
`Buttons.cs`); the config `KeyCode`s seed the default bindings (F5 quicksave verified live
through the custom button, SN 2026-07-07).

## MonoBehaviours

### `SavegameController` (on the Player GameObject, `Player.Awake` postfix)

`AbstractAwakeSingleton`; `AsyncStart()` waits on the repo-wide singleton availability chain
(`LightmappedPrefabs`, `PAXTerrainController`, `uGUI`, `WaitScreen.IsWaiting`,
`HandReticle`), then snapshots `OriginalSlot`/`PreviousSlot`/`LatestSlot` from
`SaveLoadManager.currentSlot`, resolves the original slot name from the `slotname.bin`
marker (Base64, core `StringExtensions`) and rewrites the marker into the loaded slot.

Verified live on BOTH games. On SN1, loading the `slotautosave` save maps
`OriginalSlot="slot0000"` while `currentSlot="slotautosave"` — the identity chain the load
panel labels and the save reroute depend on.

- **Autosave**: `Update()` accumulates `AutosaveTimer` only while `!FreezeTime.HasFreezers()`
  and fires `SaveToSlot("slotautosave", resetTimer: true)` at the configured interval.
  Verified live on BOTH games (interval 1 min): repeated cycles on SN1; on BZ the FIRST
  autosave created `slotautosave` from scratch — the full `ClearSlot` + `CreateSlot` +
  `CopySlot(slot0000 → slotautosave)` + save composition (the same path a quicksave takes),
  `slotname.bin` written, `currentSlot` restored to `slot0000` after every cycle, timer reset.
- **`SaveToSlot`**: swaps `SaveLoadManager.currentSlot` to the target slot around
  `IngameMenu.SaveGameAsync` and restores `OriginalSlot` afterwards; opens the menu with
  `mainPanel` hidden while the save runs (the game's own `SaveGameAsync` re-shows the panel
  and closes the menu when done).
- **`LoadLatestSlot`** (quickload body): `SaveLoadManager.LoadSlotsAsync()` →
  `uGUIMainMenuStartPatch.ForceAutoload = true` → `IngameMenu.QuitGame(false)`. Verified live
  on BOTH games (started via `StartCoroutine("LoadLatestSlot")`, the exact coroutine the F9
  keybind starts): quit to menu, menu `Start` prefix consumes `ForceAutoload`
  (`FirstRun`/`ForceAutoload` both false afterwards) and reloads the most recent save —
  scene chain in the log: StartScreen scene load → menu slot listing → world tiles reload.
- **`CanSaveGame`**: gates on `Started`, `Saving`, `Copying`, `IngameMenu.GetAllowSaving()`
  and permadeath — `GameModeUtils.IsPermadeath()` on SN1, `GameModeManager.GetOption<bool>
  (GameOption.PermanentDeath)` on BZ (`#if SUBNAUTICA`, the project's only game gate).
  Verified live true on both SP saves; momentarily false right after a world load (transient
  vanilla gates), which only delays a pending quick/auto save.
- Slot failures (`ClearSlot`/`CreateSlot`/`CopySlot`) raise `DebuggerUtility.ShowWarning` —
  an always-visible `ErrorMessage.AddWarning` HUD toast (restored in this verification, see
  below). Toast path verified live on BOTH games (screenshots).

### `WaitScreenController` (on the WaitScreen GameObject, `WaitScreen.Awake` postfix)

`OnWaitingChanged(bool)` tracks the waiting transition fed by the `WaitScreen.Update`
postfix; while waiting and `MaximizeLoadingSpeed` is on, `UnlockFramerate` stores the current
`Application.targetFrameRate`/`QualitySettings.vSyncCount` and forces `-1`/`0`;
`RestoreFramerate` re-applies the stored values when the wait ends.

Verified live on BOTH games by polling during a world load: vSyncCount `1 → 0` (load in
progress) `→ 1` (restored after load).

## Patches

- **`IngameMenu.SaveGameAsync` prefix**: when the latest save lives in a quick/auto slot
  (`currentSlot != LatestSlot`), replaces the save with `UpdateSlotCoroutine()` — wait until
  saving is allowed, `ClearSlot`/`CreateSlot`/`CopySlot(LatestSlot → currentSlot)` (the
  incremental batch files of the newest state must pre-exist in the target slot),
  `SyncLatestSlotName`, then a passthrough `SaveGameAsync`. The skip populates `__result`
  with the replacement coroutine (fixed in this verification, see below). Verified live on
  BOTH games: after an autosave, the menu Save button reroutes and re-syncs
  `LatestSlot=slot0000`, zero errors.
- **`MainMenuLoadPanel.UpdateLoadButtonState` postfix**: adds a "SaveGameSlot" TMP label
  ("Slot 0000", "Slot Autosave", …) plus a mirrored background image to every save row
  (`Text` aliases `TMPro.TextMeshProUGUI` unconditionally — both games are TMP).
  Verified live on BOTH games with screenshots of the Play panel.
- **`uGUI_MainMenu.Start` prefix**: autoloads the most recent savegame when
  `AutoloadLatestSavegame` is on (first menu only, `FirstRun`) or when quickload forces it
  (`ForceAutoload`). Verified live: BZ launched with the toggle on goes from boot to in-game
  with zero UI input; the quickload round trip exercises the `ForceAutoload` branch on both
  games.
- **`WaitScreen.Awake`/`WaitScreen.Update` postfixes**: attach `WaitScreenController` and
  feed it the private `isWaiting` flag every frame.

## Restored / fixed / dropped

- **RESTORED — `[BepInDependency]` on `Plugin.cs`**: the BetterSubnautica dependency from the
  old `mod.json` (commit `e6feace`).
- **RESTORED — slot failure warning toasts** (commit `a5f7793`): the old
  `DebuggerUtility.ShowWarning` → `DebuggerController.ShowWarning` →
  `ErrorMessage.AddWarning` chain (an always-visible HUD toast) is back in the core and in
  the three `SavegameController` failure paths. The porting had downgraded these calls to
  `ShowMessage`, which renders on the IMGUI debug overlay only (`ShowDebugInfo`, default
  false) — slot copy/create/delete failures had become invisible to a normal player.
- **FIXED — save reroute vs Nautilus** (commit `aa3483b`): the reroute prefix returned
  `false` while starting `UpdateSlotCoroutine` manually, leaving `__result = null`; Nautilus
  wraps `IngameMenu.SaveGameAsync` with a passthrough postfix
  (`SaveUtilsPatcher.InvokeSaveEvents`) that iterates the returned enumerator, so every
  rerouted save threw a `NullReferenceException` inside Nautilus and killed its save events
  for that call. The prefix now returns the replacement coroutine through `__result`; the
  caller (and Nautilus) drive it. Verified live on BOTH games: rerouted saves complete with
  zero Nautilus exceptions.
- **DROPPED (game API removed) — `IngameMenu.SetPleaseWaitVisible` calls**: the method is
  gone from both games; the current `SaveGameAsync` closes the menu itself and signals
  progress via `SaveLoadManager.NotifySaveInProgress` (vanilla save indicator).
- **DROPPED (game API removed) — `WaitScreen.Show`/`Hide` prefix pair**: both methods are
  gone; the waiting flag flips inside `WaitScreen.Update`, hence the Update-postfix +
  transition tracking redesign.
- **DROPPED (game API removed) — `PAXTerrainController.isWorking` + `uGUI.isLoading`** in the
  `AsyncStart` wait chain: both members are gone; `WaitScreen.IsWaiting` carries the job
  (repo-wide substitution, same as BetterHUD/BetterQuickSlots).
- `FreezeTime.freezers.Count == 0` → `FreezeTime.HasFreezers()`: the list is private in the
  released games; the accessor is the public equivalent (`UWE.FreezeTime`, firstpass `:271`).

## Known interactions (documented, not fixed)

- **BZ autoload vs flashing-lights disclaimer**: with `AutoloadLatestSavegame` on, the world
  load races the disclaimer's minimum show time; `StartScreen.HideDisclaimerAfterMinShowTime
  Reached` then touches the destroyed disclaimer object and logs a single vanilla
  `NullReferenceException` (`FlashingLightsDisclaimer.StartHidingDisclaimer`, no
  BetterSubnautica frames). Harmless — the load completes normally; a fast human click on a
  save produces the same race.
- Nautilus save events fire once per rerouted save from the passthrough wrap of the inner
  `SaveGameAsync`; the outer wrap around the replacement coroutine can add a second
  `OnSaveEvents` firing after the copy completes. No subscriber in the current mod set
  observes the difference (SubnauticaMap logs a single `Save` per cycle).

## NEEDS-USER checklist

Real-input-only checks (the bridge sends no synthetic key/pointer events):

- ~~Press F5 in game~~ — VERIFIED on SN1 2026-07-06 via BetterRemote virtual input: a
  virtual F5 creates `slotquicksave` from scratch (save composition runs, batches written)
  and `currentSlot` is restored to the original slot afterwards. ~~BZ~~ — VERIFIED
  2026-07-08 by real keypress (`slotquicksave` written on disk; interval autosave writes
  `slotautosave` too).
- ~~Press F9 in game~~ — VERIFIED on SN1 2026-07-06 via BetterRemote virtual input, full
  cycle: quit to menu, `ForceAutoload` consumed, the most recent save (`slotquicksave`)
  reloads and `currentSlot` follows it. ~~BZ~~ — VERIFIED 2026-07-08 by real keypress: the
  quickload reloads the LATEST slot (the newer `slotautosave` won over the quicksave —
  user-approved semantics).
- ~~Rebind the two keys from Options → Mods → Better Savegames~~ — VERIFIED 2026-07-08
  (F5→F6→F5, F9→F10; new keys fire). This check exposed the capture bug fixed below: the
  hotkeys now sit behind an `AvatarInputHandler.main.IsEnabled()` gate, so a key pressed
  while a UI owns the input (in-game menu, PDA, keybind capture) no longer fires the
  save/load — re-verified live (capture press leaves the slot files untouched). The row
  label updates immediately on rebind: the core DLL's `ModKeybindOptionPatches` wraps the
  Nautilus `bindCallback` so the label regenerates from the new key string (see
  `docs/mods/bettersubnautica.md` §Patches; verified live 2026-07-07, V→F7→V from the
  main-menu Mods tab).

## Fold-time notes

- `docs/analysis/savegames-and-vehicles.md` (develop-only) §BetterSavegames is accurate
  against this tree for architecture and API notes; it predates the restored
  `[BepInDependency]`, the restored `ShowWarning` toasts (it does not flag the
  ShowMessage downgrade) and the `__result` reroute fix (the NRE was undiscovered).
- The verification saves overwrote the test copies of both games' saves; the session-start
  states are preserved in `SNAppData/SavedGames-backup-spverify` in each game directory and
  restored at session end.

## Multiplayer verification (branch `mp-verify`)

The MP surface is one guard, commit `6b4a82c` ("Added BetterSavegames multiplayer guard"): a
`#if BELOWZERO_MULTI` `Awake` override on `SavegameController` that destroys the controller
when a botbenson server session is active (`Network.Session.Current?.ServerId` non-empty —
API verified against `Botbenson\Subnautica.API\...\NetworkUtility\Session.cs`, the same
session probe BetterMap's `SaveController` uses). Saving is server-managed in a multiplayer
session and the vanilla slot directories do not exist there, so quicksave/quickload/autosave
stay inert; without a session (a BZ.MULTI build playing a normal single-player world) the
controller initializes and the full SP behavior above applies. `WaitScreenController` (the
loading-screen framerate unlock) carries no guard: it is client-local and save-agnostic.
SN.MULTI carries no guard either — Subnautica-side Nitrox sessions are the open
`BetterNitrox/Plugin.cs:11` TODO, so SP behavior applies everywhere on that flavor. Audited
code-vs-intentions on `mp-verify`: the guard is additive, nothing else in the mod diverges
from the SP tree (builds SN.MULTI 21 warnings / BZ.MULTI 33 warnings, 0 errors).

### Verified live (2026-07-06 two-client regression)

- **Hosted BZ session**: the Player GameObject carries no `SavegameController` (the guard
  destroys it) and the vanilla slot directories take no writes while playing (slot0000
  untouched for the whole session).
- **Session-less BZ.MULTI launch**: the controller initializes with the full SP state —
  `Started` true, the autosave timer accumulating, and the
  `OriginalSlot`/`PreviousSlot`/`LatestSlot` chain snapshotted from the loaded slot.
