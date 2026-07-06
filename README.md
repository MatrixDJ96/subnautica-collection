# BetterSubnautica

A suite of BepInEx plugins for **Subnautica** and **Subnautica: Below Zero**, designed to work
in single-player and multiplayer (Nitrox for Subnautica, botbenson for Below Zero).

## Building

```bash
dotnet build BetterSubnautica.sln -c BZ.MULTI   # or SN.STABLE / SN.MULTI / BZ.STABLE
```

Create `GameDir.targets` from `GameDir.targets.example` first (machine-local game paths).
Every build deploys the plugins straight into the game's `BepInEx\plugins\MatrixDJ96` folder.

Agent-facing instructions live in [AGENTS.md](AGENTS.md); deeper documentation in
[docs/](docs/).

## License

See [LICENSE.md](LICENSE.md).
