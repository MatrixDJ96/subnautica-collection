# BetterRemote — in-game debug bridge

`BetterRemote` is a development-only BepInEx plugin that runs a local HTTP server inside the
game process, so tooling (and Claude Code via MCP) can inspect and drive the live game without
screenshots or simulated input. It builds and deploys like every other project in the solution.

## Architecture

- `BetterRemote/RemoteServer.cs` — `HttpListener` on port 2600 (configurable via the BepInEx
  config `Server.Port`). Unity APIs are main-thread-only: every game-touching handler is queued
  on a `ConcurrentQueue` and executed by `Pump()` from the plugin's `Update()`; the HTTP worker
  thread waits on a `TaskCompletionSource` (10 s timeout, 30 s for coroutines).
- On Mono, `HttpListener` is fully managed — no HTTP.SYS URL ACL, no admin rights needed.
- `Server.Interface` selects what the listener binds:
  - `Local` (default) registers three loopback prefixes — `localhost`, `127.0.0.1` and `[::1]`.
    `localhost` resolves to `::1` alone on some hosts, so the explicit `127.0.0.1` prefix is what
    keeps IPv4 clients working.
  - `All` registers `http://+:{port}/`, reachable from other machines (over NetBird too, which
    makes the SSH tunnel below optional). A `HttpListenerException` here logs a warning and falls
    back to the loopback prefixes.
- A request whose `Host` header is a bracketed IPv6 literal (`curl http://[::1]:2600/ping`) gets
  `400 Bad Request (Invalid url: http://[:2600/…)`: Mono's request parser splits that header on
  the first `:` inside the brackets. The `::1` socket itself serves normally — reach it with a
  hostname, e.g. `curl --resolve localhost:2600:[::1] http://localhost:2600/ping`.

## Endpoints

| Endpoint | What it does |
|---|---|
| `GET /ping` | Liveness check (answers as soon as BepInEx loads plugins, i.e. before the menu). |
| `GET /status` | `inGame`, `loading` (`WaitScreen.IsWaiting`), player position/depth/biome, current vehicle, day scalar. |
| `GET /console?cmd=…` | Runs a DevConsole command (`warp x y z`, …); the command line can also be sent as the raw POST body instead of `cmd`. **Caveat:** the returned `accepted` flag is unreliable in multiplayer — botbenson prefixes `DevConsole.Submit`, so `warp` reports `false` yet executes. Verify the effect (e.g. via `/status`), not the flag. |
| `GET /spawn?tech=…&count=&dist=` | Spawns an entity near the player via `CraftData` directly (`CreatePrefab → LargeWorldEntity.Register → NotifyCraftEnd → StartConstruction`), bypassing the console — botbenson's `PlayerUsingCommand` event gate denies `spawn` in multiplayer. |
| `GET /find?name=&radius=&limit=` | Active GameObjects matching a name substring, sorted by distance from the player, with their hierarchy `path` and their full component list. |
| `GET /dump?name=` | Components of the first matching GameObject with simple field values (reflection, public + private). |
| `GET /log?lines=&match=` | Tails the BepInEx log, optionally filtered. |
| `GET /teleport?x=&y=&z=` or `?to=<name>` | Moves the player via `Player.main.SetPosition` — to coordinates, or 2 m in front of the first GameObject matching `to`. Bypasses the console, so it works under botbenson's multiplayer command gate. |
| `GET /inventory?tech=&count=` | Adds items to the player inventory via `CraftData.AddToInventoryAsync`. |
| `GET /equip?tech=&target=&slot=` | Installs an upgrade module into a vehicle's module equipment: creates the item, then equips it through the vanilla `Equipment.AddItem` transfer (compatibility checks included). `target` is the vehicle GameObject name; `slot` defaults to the first free compatible slot. |
| `GET /invoke?type=&member=&target=&args=` | Calls any C# method via reflection. `target`: empty/`static` for static methods, `main` for the type's static `main`/`instance` singleton, a GameObject name substring (component lookup), or a `/`-separated path suffix for exact addressing among same-named objects (e.g. `target=GAME_INVITE_CODE/InputField`). `args`: comma-separated; primitives, enums, strings, `Vector3` as `x\|y\|z`, and the literal `null` for any reference-type parameter (e.g. an unused `HandTargetEventData`). `member` accepts a dotted path walked hop by hop through intermediate fields/properties before the leaf method (e.g. `quickSlots.SelectImmediate` from `Inventory` `target=main`), so methods on nested instances are invokable. Overload chosen by name + arg count + parseability. |
| `GET /get?type=&member=&target=` | Reads any field or property via reflection (same `target` addressing as `/invoke`). `member` accepts a dotted path walked hop by hop through fields/properties (e.g. `Core.GlobalSettings.LinkedStorage` from a plugin type, `transform.position` from a component); member lookup covers private members declared on base classes. |
| `GET /set?type=&member=&target=&value=` | Writes any field or property via reflection (same dotted `member` paths as `/get` — the leaf is written, intermediate hops are read). |
| `GET /ui?press=&index=&limit=` | Without `press`: lists active clickable UI elements (anything with an `IPointerClickHandler`). With `press`: clicks the best name match through `ExecuteEvents` — drives menus without OS-cursor clicks or window focus. `index` (0-based) picks among matches ordered by name-match closeness (shortest matching name first), then path, then sibling order — NOT the listing order; the response reports the total `matches` and the pressed `path`. |
| `GET /input?key=\|mouse=\|look=&hold=&frames=` | Virtual input inside the game process. `key=<KeyCode>` (e.g. `F5`, `Tab`, `Alpha6`, `LeftControl`, `Mouse1`) presses a key for `hold` ms (default 80) with real down/held/up frame semantics; `mouse=left\|right\|middle` is a mouse-button alias; `look=dx\|dy` feeds a per-frame mouse-look delta for `frames` frames (default 30). Without parameters: the active virtual-input state plus the list of patched `Input` methods. Delivery: Harmony postfixes on legacy `UnityEngine.Input` polling (`GetKey*`, `GetMouseButton*`, `GetAxis`/`GetAxisRaw` — what the plugins and Below Zero's `GameInput` read) and, on Subnautica, device state events queued into the new InputSystem (`KeyboardState`/`MouseState` — what `GameInputSystem` reads for game actions). The KeyCode-to-InputSystem `Key` mapping is case-insensitive, so a key whose Unity name differs from the InputSystem member only by casing (`BackQuote` -> `Backquote`) still reaches InputSystem-only bindings. A virtual press is visible to both stacks, like a physical key. |
| `GET /hierarchy?root=&depth=&limit=` | Scene hierarchy tree: all loaded scenes' roots, or the subtree of the first GameObject matching `root`. |
| `GET /screenshot?file=` | `ScreenCapture.CaptureScreenshot` to `file` (default `<game>\BetterRemote.png`); the PNG is written at end of frame — poll for the file. |

`/invoke`, `/get` and `/set` together are the generic control plane: anything reachable through
a static member, a `main`/`Instance` singleton, or a component on a named GameObject can be
read, written, or called without a dedicated endpoint. `type=` accepts a namespace-qualified or
bare type name; a full-name match wins over a bare-name match across all loaded assemblies.

Numeric query parameters and reflection arguments parse invariant-culture first (dot decimals,
what scripts and the MCP layer emit), then fall back to the OS culture (comma decimals on an
Italian host); thousands separators are never accepted, so the wrong-culture decimal mark fails
over cleanly instead of silently scaling the value. Reflected values in responses are
`ToString()` strings rendered in the OS culture; the JSON numbers the server emits itself
(positions, distances) are culture-invariant.

## MCP integration

`.mcp.json` (project scope) registers `tools/betterremote_mcp.py`, a standard-library-only
Python stdio bridge exposing the endpoints as MCP tools. The bridge negotiates the MCP protocol
version against its supported list (unknown requests get the newest supported version),
validates `tools/call` arguments against the declared `inputSchema` (required, types, no
undeclared properties) before any HTTP call, and processes JSON-RPC batch arrays entry by
entry. Tools: `game_ping`, `game_status`, `game_console`,
`game_spawn`, `game_find`, `game_dump`, `game_log`, `game_teleport`, `game_inventory`,
`game_equip`, `game_invoke`, `game_get`, `game_set`, `game_ui`, `game_input`,
`game_hierarchy`, `game_screenshot`. The MCP
server loads at session start; the game only needs to be running when a tool is actually
called.

Two server entries share the same script, selected by the `BETTERREMOTE_PORT` env var:
`betterremote-local` targets the local game on port 2600, `betterremote-remote` targets port
2601 — an SSH tunnel to a second machine's game
(`ssh -N -L 2601:localhost:2600 <host>`) for multiplayer verification.

## Typical verification flow

1. Launch the game; from the main menu on, drive the UI with `/ui?press=<button>` (no window
   focus needed). In-world hand targets (e.g. the SeaTruck steering wheel) are driven with
   `/invoke` on their handler, passing `args=null` for the unused `HandTargetEventData` — all
   interaction goes through the bridge, never through OS-level input synthesis.
2. Poll `/status` until `inGame && !loading` (~25 s after pressing the save slot).
3. Drive the test: `/spawn?tech=seatruck` next to the player instead of waiting for world
   streaming, `/teleport` to reach a site, `/find`/`/dump`/`/get` to read state, `/invoke` and
   `/set` to manipulate it, `/log?match=<tag>` for instrumentation, `/screenshot` for the rare
   visual check.
