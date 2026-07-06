# BetterRemote — single-player feature verification

Development-only in-game HTTP bridge (architecture and endpoint reference:
`docs/betterremote.md`). The mod is born on branch `sp-verify` at `4750be1` and has no
`d51c6e9` baseline: the verification inventory is the endpoint table of `docs/betterremote.md`
itself, checked endpoint by endpoint against the live server on BOTH games (DLLs built at the
branch tip, SN.STABLE 19 warnings / 0 errors, BZ.STABLE 31 warnings / 0 errors). The bridge is
the campaign's own test harness, so every claim below is proven with control experiments —
each response cross-checked against an independent observation (a second endpoint, the BepInEx
log, a screenshot, or the game state itself).

## Plugin bootstrap

`Plugin.cs` (`[BepInPlugin]`, `Plugin : SubnauticaPlugin`) with
`[BepInDependency(BetterSubnautica.MyPluginInfo.PLUGIN_GUID)]` — the same dependency shape as
every sibling plugin, so BepInEx orders BetterSubnautica first deterministically instead of by
assembly-name luck. Verified live on both games: the load sequence starts with
`Loading [BetterSubnautica]` followed by the other mods, zero errors, and the bridge answers
`/ping` with plugin name and version. `Config.Bind("Server", "Port", 2600)` selects the port;
the listener binds to `localhost` only. `Update()` pumps the main-thread queue; `OnDestroy()`
stops the listener.

## Fixed — culture-invariant numeric parsing

`RemoteServer` parses every float/double query parameter and reflection argument
invariant-culture first (dot decimals — what scripts and the Node MCP layer emit, since
JavaScript stringifies numbers with a dot), falling back to the OS culture (comma decimals on
an Italian host). Both attempts use `NumberStyles.Float`, which excludes thousands separators,
so the wrong-culture decimal mark fails its parse and falls through instead of silently
scaling the number (`float.Parse("0.87")` under it-IT reads 87). Covered call sites:
`TryConvert` (float, double, each `Vector3` component), `/teleport` x/y/z, `/find` radius,
`/spawn` dist.

Verified live on both games, both notations on the same host:

- `/teleport?x=554.5&y=-125.5&z=404.5` → position exactly `(554.5, -125.5, 404.5)`;
  `x=554,5&y=-125,5&z=404,5` → the same point (SN, mirrored on BZ with local coordinates).
- `/invoke Player.SetPosition args=555.5|-126.5|405.5` → `/status` reports exactly that point
  on both games (dotted decimals inside a `Vector3` argument).
- `/set UnityEngine.Time.timeScale value=0.5` and `value=0,25` → `/get` confirms 0.5 / 0.25;
  restored to 1 afterwards.
- Through the MCP layer: `game_find radius=3.5` returns only hits at 2.2 m (SN) and
  `game_spawn dist=2.5` / `game_find radius=30.5` behave to the meter (BZ) — the full
  JS-number → query-string → invariant-parse chain works end to end.

Reflected values in responses (`/get`, `/set` echo, `/dump`, `/invoke` results) are
`ToString()` strings rendered in the OS culture (`"0,5"` on this host); JSON numbers emitted
by the server itself (positions, distances, depth) are culture-invariant. Treat the string
fields as display values, the numeric fields as data.

## Endpoint verification (15/15, both games)

| Endpoint | Verified behavior |
|---|---|
| `/ping` | Answers ~5-7 s after the exe launch on both games — before the menu (SN menu ~17 s, BZ ~7 s after launch) — with `ok`, plugin name, version. Handled on the worker thread, so it never waits on the main-thread pump. |
| `/status` | Menu: `{inGame:false, loading:false}`. The full load transition is observable live (BZ: `loading:true` → `inGame:true, loading:true` → `inGame:true, loading:false`). In game: position, depth, biome (`mushroomForest` SN / `twistyBridges_Shallow` BZ), vehicle, `dayScalar`. |
| `/console` | `warp 560 -110 410` (SN) and `warp -330 -10 -240` (BZ) report `accepted:true` and the effect is confirmed by `/status` (exact coordinates). POST body works as the `cmd` alternative: `daynightspeed 1` posted raw executes and raises the vanilla toast (captured in the SN screenshot). |
| `/find` | `name` substring + `radius` + `limit` honored together; results sorted by distance with full component lists (the spawned `Flashlight(Clone)` shows its BetterLights controllers attached). |
| `/dump` | Player dump lists every component with simple-typed public AND private fields (`GUIHand.cachedEnergyHudText`, `Oxygen.oxygenAvailable=45`) on both games. |
| `/spawn` | `flashlight&count=2&dist=3.5` (SN) and `seaglide&dist=2.5` (BZ) spawn named clones at reported positions; TechType is case-insensitive; unknown tech → `unknown TechType` error. |
| `/log` | Filter-then-tail semantics: `match` filters first, `lines` keeps the last N matching lines (so the head of a long capture falls off, not the tail). Used throughout for load-order and error checks. |
| `/teleport` | Coordinate form exact on both games (see the culture section); `to=` lands 2 m in front of the named object (`CyclopsCollision` SN, `seatruck` BZ). |
| `/inventory` | `titanium` lands as `Titanium(Clone)`; count confirmed via `/invoke Inventory.GetPickupCount target=main args=titanium`. |
| `/invoke` | All five addressing/argument modes verified per game: static (`UnityEngine.Application.get_version` → 1.22.83031 SN / 1.22.53872 BZ), `main` singleton + enum argument (`Inventory.GetPickupCount(TechType)`), GameObject-name component target (`Oxygen.GetOxygenAvailable target=Player` → 45), literal `null` for reference parameters (`UnityEngine.Object.op_Equality(null, null)` → True), `Vector3` pipe argument. Overload picked by name + arg count + parseability, signature echoed back. |
| `/get` | Static property (`WaitScreen.IsWaiting`), instance member via `main`, and the `/`-path target (`Transform.childCount target=Player/camPivot` → 1) on both games. |
| `/set` | `UnityEngine.Time.timeScale` set/read/restore cycle on both games (static property with both decimal notations). |
| `/ui` | List mode returns active `IPointerClickHandler` objects with paths. Press mode drives the full menu flow on both games (ButtonPlay → save panel → LoadButton) with `handled:true`; `index` picks among same-named matches and the response's `matches` count plus echoed `path` identify exactly what was pressed (SN save picked by `index=1` out of `matches:2`). Matches order by name-match closeness (shortest name first), then path, then sibling index — the press order is NOT the listing order. |
| `/hierarchy` | Scene list (SN in-game: Main, Essentials, Cyclops, EscapePod, Aurora…) and `root=Player&depth=1` subtree with child counts on both games; `limit` caps the node budget. |
| `/screenshot` | PNG written at the game root ~1-2 s after the call on both games (SN capture shows the live frame incl. the console-command toast raised one call earlier). |

## Error handling

Verified on the live server: unknown endpoint → 404 with the full endpoint list; missing
required parameter → 400 (`/dump` without `name`, `/set` without `value`); unknown type,
unknown TechType, no matching clickable, and `index N out of range: M clickables match` all
return structured `error` JSON with HTTP 200; handler exceptions map to 500 with exception
type and message, and a main-thread timeout maps to 504 (10 s, 30 s for coroutines — never
triggered during the sweep: the pump answered even mid-load).

## MCP layer (`tools/betterremote_mcp.py`)

The 17 `game_*` tools map 1:1 onto the endpoints (same names as the table in
`docs/betterremote.md`); `game_ping`, `game_status`, `game_find`, `game_log` exercised live
from Claude Code against both games during the sweep, fractional numeric params included. The
client timeout (35 s) exceeds the server's coroutine timeout (30 s), so the server always
answers first. `game_teleport` marks x/y/z as `zero`-meaningful, so coordinate 0 is
transmitted; other numeric params fall back to the endpoint default when 0/absent.

## Restored / dropped

- **Restored: nothing** — the mod is original to this branch; there is no older tree to
  restore from.
- **Dropped: nothing.** Every endpoint in `docs/betterremote.md` exists and behaves as
  documented (the two behavior deltas found — press-match ordering and numeric parsing — are
  fixed in the doc and in the code respectively, this session).

## NEEDS-USER checklist

- None. The bridge's whole surface is HTTP-driven by design, so every feature is verifiable
  through the bridge itself plus control experiments.
