# BetterSubnautica — istruzioni operative

Istruzioni canoniche e tool-agnostiche del workspace (i coding agent le leggono nativamente;
Claude Code le raggiunge dal bridge `CLAUDE.md`).

> Lingua di lavoro: **italiano**. Termini tecnici e identificatori in inglese.

Suite di plugin BepInEx (mod di gioco) per Subnautica e Subnautica: Below Zero, costruita per
girare sia in single player sia in multiplayer (Nitrox per Subnautica, la mod botbenson per
Below Zero).

## Stack

- Linguaggio principale: C# (`net48`, `LangVersion latest`), una sola solution MSBuild
  (`BetterSubnautica.sln`)
- Framework: plugin BepInEx 5 + patching a runtime con HarmonyLib
- Assembly di gioco: referenziati da `Dependencies\<Configuration>` e publicizzati in fase di
  build (`BepInEx.AssemblyPublicizer.MSBuild` su `Assembly-CSharp` /
  `Assembly-CSharp-firstpass`, così i membri privati del gioco sono accessibili direttamente)
- Gestore pacchetti: NuGet (`NuGet.config`), pacchetti pinnati in `Directory.Build.targets`
- Struttura di build (standard della famiglia): `Directory.Build.props` per le proprietà
  condivise e le costanti per configurazione, `Directory.Build.targets` per reference e passi
  di build — MSBuild li importa da sé, il primo in testa e il secondo in coda. Ogni `.csproj`
  porta la sola identità del progetto. `Nullable` non è impostato a livello di progetto: si
  abilita per file con `#nullable enable`.

## Architettura

Una solution a convenzioni condivise; ogni directory qui sotto è un progetto plugin BepInEx.

```
BetterSubnautica/   # shared core: utilities, debuggers, base controllers
BetterGraphics/     # graphics options: full-screen mode, sky/material appliers, settings panels
BetterHUD/          # HUD tweaks
BetterLights/       # lights controllers and toggling
BetterMap/          # map ping and save controllers (integrates SubnauticaMap)
BetterNitrox/       # multiplayer compatibility layer (botbenson / Nitrox patches)
BetterPDA/          # PDA pause, eat/use keybind, inventory tab and tooltip patches
BetterQuickSlots/   # quick-slot tweaks
BetterRemote/       # DEV-ONLY in-game HTTP bridge for live verification (docs/betterremote.md)
BetterSavegames/    # quicksave/quickload, autosave interval, autoload of the latest savegame
BetterVehicles/     # Cyclops, Seatruck, Seamoth, Exosuit, Seaglide and docking-bay controllers
docs/               # knowledge: betterremote.md (bridge reference), analysis/ (porting notes)
tools/              # betterremote_mcp.py — MCP stdio bridge to the BetterRemote HTTP server
```

Le configurazioni di build selezionano gioco e modalità via `DefineConstants`
(`Directory.Build.props`): `SN.STABLE` / `SN.MULTI` (Subnautica), `BZ.STABLE` / `BZ.MULTI` (Below
Zero). Il codice è gated con `#if SUBNAUTICA` / `#if BELOWZERO` / `#if MULTI` e le costanti per
combinazione.

Per i dettagli: vedi `docs/`.

## Convenzioni

- **Commit**: subject brevi al passato ("Added…", "Reworked…", "Re-enabled…"), di lunghezza
  grossomodo allineata, senza body se non serve, senza trailer di attribuzione AI.
- **Doc**: affermativi — descrivono cosa il codice È, al presente (comportamento corrente, non
  la storia, non ciò che non è). I nomi dei file di doc sono in inglese.
- **Stile del codice**: `.editorconfig` è autoritativo; allinearsi al codice circostante.

## Insidie

- `GameDir.targets` è locale alla macchina e gitignorato; va creato da
  `GameDir.targets.example`. Senza, la build FALLISCE.
- Ogni build fa auto-deploy di ciascun plugin dentro il gioco vivo
  (`$(GameDir)\BepInEx\plugins\MatrixDJ96\<Plugin>` — `Directory.Build.targets`). Il gioco deve essere
  CHIUSO quando si ricompila: un gioco in esecuzione tiene lockate le DLL dei plugin e la copia
  di deploy fallisce.
- Le due configurazioni dello STESSO gioco fanno deploy nella STESSA cartella plugin: il gioco
  esegue l'ULTIMA configurazione compilata (una build `*.MULTI` sostituisce in silenzio le DLL
  `*.STABLE`, col codice `#if STABLE` compilato via). Compilando più configurazioni in blocco,
  compilare per ULTIMA quella che si sta per lanciare.
- `BZ.MULTI` compila contro l'`Assembly-CSharp.dll` patchata da botbenson (la mod multiplayer
  sostituisce l'assembly managed del gioco; le firme differiscono dalla vanilla). In Below Zero
  il multiplayer gestisce alcuni comandi console lato server — verificare gli effetti in gioco,
  non i flag di ritorno.
- `Dependencies\<Configuration>` contiene le reference assembly per configurazione; una
  directory mancante per la configurazione che si compila rompe la risoluzione delle reference.

## Comandi comuni

```bash
# Build + deploy nel gioco (scegli la configurazione che ti serve)
dotnet build BetterSubnautica.sln -c BZ.MULTI

# Controllo di sintassi del bridge MCP dopo averlo modificato
python -m py_compile tools/betterremote_mcp.py
```

## Verifica

Non ci sono unit test: la verifica è dal vivo, in gioco, attraverso il bridge HTTP BetterRemote
su `http://localhost:2600` (endpoint, tool MCP e flusso tipico sono documentati in
`docs/betterremote.md`). Avviare il gioco, pilotare il menu con `/ui?press=…`, fare polling di
`/status` finché `inGame && !loading`, poi esercitare la modifica e leggere `/log`.

## Strumenti disponibili

- **Server MCP BetterRemote** (`.mcp.json`, project scope): tool `game_*` che rispecchiano gli
  endpoint HTTP; il gioco deve girare solo quando un tool viene chiamato. `betterremote-local`
  punta al gioco locale (porta 2600); `betterremote-remote` punta al gioco di una seconda
  macchina attraverso un tunnel SSH sulla porta 2601 (vedi `docs/betterremote.md`).

## Cose da NON fare

- Non modificare i file sotto `Output/` o `obj/`/`bin/` — artefatti di build, rigenerati a ogni
  compilazione.
- Non committare `GameDir.targets` (locale alla macchina) né `CLAUDE.local.md` (personale,
  gitignorato).
- Non alzare `Version` in `Directory.Build.props` come effetto collaterale di lavoro non
  correlato.

## Come lavorare in questo repo

- **Verifica il tuo lavoro**: compila la configurazione che hai toccato ed esercita la modifica
  in gioco via BetterRemote. Mai dichiarare "fatto" senza evidenza.
- **Affronta le cause, non i sintomi**: quando una patch sbaglia il bersaglio, correggi la
  causa invece di sopprimere l'errore.

---

*Last update: 2026-07-28*
*Next review: 2027-01-03*
