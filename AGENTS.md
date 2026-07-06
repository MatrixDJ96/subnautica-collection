# Project instructions for coding agents

<!-- The one cross-agent instructions file of this repo, read natively by coding agents.
     Verified, present-tense facts only: commands an agent cannot guess, non-default
     conventions, environment quirks. Nothing inferable from the code. Stays under 200 lines
     — split by subject into referenced docs beyond that. -->

## Build & run

```bash
# Build + deploy one configuration: SN.STABLE, SN.MULTI, BZ.STABLE or BZ.MULTI
dotnet build BetterSubnautica.sln -c BZ.MULTI

# Release build: strips the DEBUG_LOGS diagnostic light-state logging
dotnet build BetterSubnautica.sln -c BZ.MULTI -p:DebugLogs=false

# Syntax check of the MCP bridge after editing it
python -m py_compile tools/betterremote_mcp.py
```

- Create `GameDir.targets` from `GameDir.targets.example` before the first build and set
  `GameDir` (and, for the MULTI configurations, `NitroxDir`) to the local install folders.
  The file is gitignored; `Directory.Build.props` imports it unconditionally, so without it
  the build FAILS.
- Every build deploys each plugin it builds into the live game, at
  `$(GameDir)\BepInEx\plugins\MatrixDJ96\<Plugin>` (`CopyToGameFolder` in
  `Directory.Build.targets`, which empties that folder first). Close the game before
  building: a running game holds the plugin DLLs locked and the deploy fails.
- Both configurations of the SAME game deploy into the SAME plugin folders, so the game runs
  the LAST configuration built: a `*.MULTI` build silently replaces the `*.STABLE` DLLs with
  its `MULTI` code compiled in (and, in `BetterLights` and `BetterMap`, a hard dependency on
  `BetterNitrox`). When building several configurations in a row, build LAST the one about
  to be launched.
- Populate `Dependencies\<Configuration>` (gitignored) with the SubnauticaMap DLL that
  `BetterMap` compiles against: `SubnauticaMap.dll` for the SN configurations,
  `SubnauticaMap_BZ.dll` for the BZ ones, copied from the SubnauticaMap mod installed in the
  game (`BepInEx\plugins\SubnauticaMap\` and `BepInEx\plugins\SubnauticaMap_BZ\`). Without
  the folder of the configuration being built, `BetterMap` does not resolve the SubnauticaMap
  types.

## Verification

No test project exists: a change is verified live, in the game, through the BetterRemote HTTP
bridge on `http://localhost:2600`; its endpoints, the MCP servers registered in `.mcp.json` and
the verification flow are in `docs/betterremote.md`.

## Conventions

- Language: everything in this repository is written in English — docs, code comments,
  identifiers, file names and commit messages.
- Commits carry no AI attribution trailer.
- Docs are affirmative: they describe what the code IS, in the present tense (current
  behaviour — not history, not what it is not). Per-plugin design notes, known limitations
  and verification records live in `docs/mods/<plugin>.md` (`bettersubnautica.md` for the
  core); the bridge reference lives in `docs/betterremote.md`.
- Code is gated per game and mode with the constants each configuration defines in
  `Directory.Build.props`: `SUBNAUTICA` / `BELOWZERO`, `STABLE` / `MULTI`, and their
  combinations (`BELOWZERO_MULTI`, …).
- `Assembly-CSharp` and `Assembly-CSharp-firstpass` are publicized at build time
  (`BepInEx.AssemblyPublicizer.MSBuild`), so the code uses private game members directly.
- `Nullable` is not set at project level: enable it per file with `#nullable enable`.
- Attach components with `EnsureComponent` and wait for a load by polling
  `WaitScreen.IsWaiting`, like the rest of the code.
- `.editorconfig` is authoritative for code style; match the surrounding code.
- Never bump `Version` in `Directory.Build.props` as a side effect of unrelated work.
- Never edit `Output/` or `obj/`: build artifacts, regenerated on every build.

## Gotchas

- The BZ configurations compile against the `Assembly-CSharp.dll` in the installed game's
  `Managed` folder; on a botbenson install that is botbenson's patched build (the mod
  replaces the game's managed assembly; signatures differ from vanilla), and `BZ.MULTI` also
  references the botbenson DLLs under `$(NitroxDir)`.
- botbenson's `Assembly-CSharp.dll` recurses forever in `StartScreen.TryToShowDisclaimer`
  when it finds the BepInEx doorstop `winhttp.dll` next to the game executable, and the game
  never reaches the main menu; the `BZ.MULTI` `BetterNitrox` skips that method
  (`StartScreenPatches`). Keep that `BetterNitrox` deployed for every Below Zero run on a
  botbenson install, STABLE runs included: the STABLE configurations do not build
  `BetterNitrox`, so a STABLE build leaves its deployed DLL in place.
- `SN.MULTI` runs no Nitrox code: every `BetterNitrox` patch is `#if BELOWZERO_MULTI`, and on
  Subnautica `LoadNitrox()` only applies the plugin's own `[PrePatch]` classes and raises
  `NitroxLoaded`.
- `HarmonyUtility` applies patch classes one at a time without catching: a class that fails to
  apply (a missing target, a patch parameter named unlike the target's) ends that plugin's
  patch pass, and every later class of the plugin stays unpatched. After a change, check the
  plugin's ` - Patched <method> method` lines in the BepInEx log.
- The current Subnautica build shares many Below Zero signatures (`uGUI_OptionsPanel`
  `OnResolutionChanged(int applyIndex)` and `OnVSyncChanged`, TMPro `HandReticle.compTextHand`,
  the single `FreezeTime.Begin(Id)` overload): compare both games' decompiled sources before
  adding or keeping an `#if SUBNAUTICA` / `#if BELOWZERO` split.
