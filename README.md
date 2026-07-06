# BetterSubnautica

A suite of BepInEx plugins for **Subnautica** and **Subnautica: Below Zero**, designed to work
in single-player on both games and in Below Zero multiplayer (botbenson).

## Building

```bash
dotnet build BetterSubnautica.sln -c BZ.MULTI   # or SN.STABLE / SN.MULTI / BZ.STABLE
```

Create `GameDir.targets` from `GameDir.targets.example` first (game install paths, gitignored).
Every build deploys the plugins straight into the game's `BepInEx\plugins\MatrixDJ96` folder.
Reference game builds: Subnautica changeset 83031 and Below Zero 1.22.53872 (with botbenson's
`Assembly-CSharp.dll` for multiplayer).

Agent-facing instructions live in [AGENTS.md](AGENTS.md); deeper documentation in
[docs/](docs/).

## License

See [LICENSE.md](LICENSE.md).
