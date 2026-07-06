"""BetterRemote MCP server (stdio, JSON-RPC 2.0).

Thin bridge between an MCP client (Claude Code) and the BetterRemote BepInEx plugin running
inside Subnautica / Below Zero, which serves HTTP on http://localhost:<port>/ in the game
process. Standard-library only: the MCP stdio transport is newline-delimited JSON-RPC,
implemented here directly, so the server has no third-party dependencies and no build step.

Every tool is a uniform HTTP GET against one bridge endpoint, so a single declarative table
(TOOLS) is the source for both the MCP inputSchema and the HTTP query mapping.

Configuration via environment:
  BETTERREMOTE_PORT  HTTP port of the in-game bridge (default 2600)
"""
import json
import os
import sys
import threading
import urllib.error
import urllib.parse
import urllib.request

# MCP stdio is UTF-8 JSON-RPC; on Windows the default text stdio decodes with the ANSI
# codepage, which double-encodes any non-ASCII tool argument.
sys.stdin.reconfigure(encoding="utf-8")
sys.stdout.reconfigure(encoding="utf-8")

PORT = os.environ.get("BETTERREMOTE_PORT", "2600")
BASE = f"http://localhost:{PORT}"

# Single source for both the MCP inputSchema and the HTTP query mapping.
# Param fields: type/description (schema), required (schema + always sent),
# query (HTTP name when it differs from the property name), zero (0 is a
# meaningful value and is sent; elsewhere 0 falls back to the endpoint default).
TOOLS = [
    {
        "name": "game_ping",
        "description": "Check that the BetterRemote bridge inside the running game answers.",
        "path": "/ping",
        "params": {},
    },
    {
        "name": "game_status",
        "description": "Live game state: inGame, loading, player position/depth/biome, "
                       "current vehicle, day scalar.",
        "path": "/status",
        "params": {},
    },
    {
        "name": "game_console",
        "description": "Run a Subnautica DevConsole command in the game (e.g. 'spawn "
                       "seatruck', 'warp -120 -9 340', 'daynightspeed 1').",
        "path": "/console",
        "params": {
            "command": {"query": "cmd", "type": "string",
                        "description": "DevConsole command line", "required": True},
        },
    },
    {
        "name": "game_find",
        "description": "Find active GameObjects by (partial, case-insensitive) name, sorted "
                       "by distance from the player. Returns name, hierarchy path, distance, "
                       "position and component list.",
        "path": "/find",
        "params": {
            "name": {"type": "string",
                     "description": "Substring of the GameObject name (empty = any)"},
            "radius": {"type": "number",
                       "description": "Max distance from player in meters (0 = unlimited)"},
            "limit": {"type": "number", "description": "Max results (default 25)"},
        },
    },
    {
        "name": "game_spawn",
        "description": "Spawn an entity near the player via CraftData (bypasses DevConsole "
                       "and botbenson's multiplayer command gates). E.g. tech='seatruck', "
                       "'hoverbike', 'seaglide'.",
        "path": "/spawn",
        "params": {
            "tech": {"type": "string", "description": "TechType name (case-insensitive)",
                     "required": True},
            "count": {"type": "number", "description": "How many (default 1)"},
            "dist": {"type": "number", "description": "Max spawn distance in meters (default 12)"},
        },
    },
    {
        "name": "game_dump",
        "description": "Dump the components of the first GameObject matching a name, with "
                       "simple field values (reflection, public + private).",
        "path": "/dump",
        "params": {
            "name": {"type": "string", "description": "Substring of the GameObject name",
                     "required": True},
        },
    },
    {
        "name": "game_log",
        "description": "Tail the BepInEx log from inside the game, optionally filtered by a "
                       "substring.",
        "path": "/log",
        "params": {
            "lines": {"type": "number", "description": "Number of lines (default 50)"},
            "match": {"type": "string", "description": "Only lines containing this substring"},
        },
    },
    {
        "name": "game_teleport",
        "description": "Teleport the player to coordinates or next to a named GameObject "
                       "(bypasses the console, works in multiplayer).",
        "path": "/teleport",
        "params": {
            "x": {"type": "number", "description": "World X", "zero": True},
            "y": {"type": "number", "description": "World Y (negative = below sea level)",
                  "zero": True},
            "z": {"type": "number", "description": "World Z", "zero": True},
            "to": {"type": "string",
                   "description": "GameObject name substring to teleport next to "
                                  "(alternative to x/y/z)"},
        },
    },
    {
        "name": "game_inventory",
        "description": "Add an item to the player's inventory via "
                       "CraftData.AddToInventoryAsync (e.g. tech='titanium').",
        "path": "/inventory",
        "params": {
            "tech": {"type": "string", "description": "TechType name (case-insensitive)",
                     "required": True},
            "count": {"type": "number", "description": "How many (default 1)"},
        },
    },
    {
        "name": "game_equip",
        "description": "Install an upgrade module into a vehicle's module equipment (e.g. "
                       "tech='VehicleStorageModule' target='SeaMoth(Clone)'); picks the first "
                       "free compatible slot unless slot= is given.",
        "path": "/equip",
        "params": {
            "tech": {"type": "string",
                     "description": "Module TechType name (case-insensitive)", "required": True},
            "target": {"type": "string", "description": "Vehicle GameObject name substring",
                       "required": True},
            "slot": {"type": "string",
                     "description": "Equipment slot name (default: first free compatible slot)"},
        },
    },
    {
        "name": "game_invoke",
        "description": "Call any C# method in the game via reflection. target: omit for "
                       "static, 'main' for the type's main/instance singleton, or a "
                       "GameObject name substring (component lookup). args: comma-separated "
                       "primitives/enums; Vector3 as 'x|y|z'. Full generic control lever.",
        "path": "/invoke",
        "params": {
            "type": {"type": "string",
                     "description": "Type name or FullName (e.g. 'Player', 'DayNightCycle')",
                     "required": True},
            "member": {"type": "string",
                       "description": "Method name, or a dotted path (a.b.method) to invoke "
                                      "on a nested instance", "required": True},
            "target": {"type": "string",
                       "description": "Instance: empty/static, 'main', or GameObject name "
                                      "substring"},
            "args": {"type": "string",
                     "description": "Comma-separated arguments (empty for none)"},
        },
    },
    {
        "name": "game_get",
        "description": "Read any C# field or property in the game via reflection (same "
                       "type/member/target addressing as game_invoke). member accepts a "
                       "dotted path walked hop by hop (e.g. "
                       "'Core.GlobalSettings.LinkedStorage').",
        "path": "/get",
        "params": {
            "type": {"type": "string", "description": "Type name or FullName",
                     "required": True},
            "member": {"type": "string",
                       "description": "Field or property name, or dotted path (a.b.c)",
                       "required": True},
            "target": {"type": "string",
                       "description": "Instance: empty/static, 'main', or GameObject name "
                                      "substring"},
        },
    },
    {
        "name": "game_set",
        "description": "Write any C# field or property in the game via reflection "
                       "(primitives, enums, strings, Vector3 as 'x|y|z'). member accepts a "
                       "dotted path walked hop by hop (e.g. "
                       "'Core.GlobalSettings.LinkedStorage').",
        "path": "/set",
        "params": {
            "type": {"type": "string", "description": "Type name or FullName",
                     "required": True},
            "member": {"type": "string",
                       "description": "Field or property name, or dotted path (a.b.c)",
                       "required": True},
            "target": {"type": "string",
                       "description": "Instance: empty/static, 'main', or GameObject name "
                                      "substring"},
            "value": {"type": "string", "description": "New value", "required": True},
        },
    },
    {
        "name": "game_ui",
        "description": "List active clickable UI elements, or press one by name via the uGUI "
                       "event system (replaces OS-cursor click harness for menus).",
        "path": "/ui",
        "params": {
            "press": {"type": "string",
                      "description": "Name substring of the element to click (omit to list)"},
            "index": {"type": "number",
                      "description": "Which match to press when several share the name "
                                     "(0-based, default 0)"},
            "limit": {"type": "number", "description": "Max elements when listing (default 40)"},
        },
    },
    {
        "name": "game_input",
        "description": "Virtual input inside the game process: press a key or mouse button "
                       "with real down/held/up semantics (visible to legacy Input and, on SN, "
                       "the new InputSystem), or feed a mouse-look delta for N frames. No "
                       "arguments returns the active virtual-input state.",
        "path": "/input",
        "params": {
            "key": {"type": "string",
                    "description": "KeyCode name to press (e.g. F5, Tab, Alpha6, LeftControl, "
                                   "Mouse1)"},
            "mouse": {"type": "string",
                      "description": "Mouse button to press: left, right or middle "
                                     "(alternative to key)"},
            "look": {"type": "string",
                     "description": "Mouse look delta per frame as dx|dy (e.g. 40|0)"},
            "hold": {"type": "number",
                     "description": "Hold duration in ms before release (default 80)"},
            "frames": {"type": "number",
                       "description": "Frames to sustain the look delta (default 30)"},
        },
    },
    {
        "name": "game_hierarchy",
        "description": "Dump the scene hierarchy tree (all loaded scenes, or a subtree by "
                       "root name substring).",
        "path": "/hierarchy",
        "params": {
            "root": {"type": "string",
                     "description": "Root GameObject name substring (omit for scene roots)"},
            "depth": {"type": "number", "description": "Tree depth (default 2)"},
            "limit": {"type": "number", "description": "Max nodes (default 300)"},
        },
    },
    {
        "name": "game_screenshot",
        "description": "Capture an in-game screenshot to a PNG file (written at end of "
                       "frame; poll for the file).",
        "path": "/screenshot",
        "params": {
            "file": {"type": "string",
                     "description": "Absolute output path (default <game>\\BetterRemote.png)"},
        },
    },
]


def to_schema(tool):
    properties = {}
    required = []
    for prop, p in tool["params"].items():
        properties[prop] = {"type": p["type"], "description": p["description"]}
        if p.get("required"):
            required.append(prop)
    schema = {"type": "object", "properties": properties}
    if required:
        schema["required"] = required
    schema["additionalProperties"] = False
    return schema


TOOL_LIST = [{"name": t["name"], "description": t["description"], "inputSchema": to_schema(t)}
             for t in TOOLS]
TOOL_BY_NAME = {t["name"]: t for t in TOOLS}

# Versions this bridge actually implements (initialize/ping/tools/list/tools/call,
# text-only tool results). Newest first: the fallback offered to unknown versions.
SUPPORTED_PROTOCOL_VERSIONS = ["2025-06-18", "2025-03-26", "2024-11-05"]


def js_typeof(value):
    """JavaScript typeof name for a JSON-decoded value (validation error messages)."""
    if isinstance(value, bool):
        return "boolean"
    if isinstance(value, (int, float)):
        return "number"
    if isinstance(value, str):
        return "string"
    return "object"


def js_string(value):
    """JavaScript String() for query values: integral floats print without the '.0'."""
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, float) and value.is_integer():
        return str(int(value))
    return str(value)


# Server-side check of tools/call arguments against the tool's declared schema:
# required present, declared types respected, no undeclared properties.
def validate_args(tool, args):
    errors = []
    for prop, p in tool["params"].items():
        value = args.get(prop)
        if p.get("required") and (value is None or value == ""):
            errors.append(f"missing required argument '{prop}'")
        elif value is not None and js_typeof(value) != p["type"]:
            errors.append(f"argument '{prop}' must be a {p['type']}, got {js_typeof(value)}")
    for prop in args:
        if prop not in tool["params"]:
            errors.append(f"unknown argument '{prop}'")
    return errors


def include_param(p, value):
    if value is None or value == "":
        return False
    if (isinstance(value, (int, float)) and not isinstance(value, bool)
            and value == 0 and not p.get("zero")):
        return False
    return True


def call_bridge(tool, args):
    query = {}
    for prop, p in tool["params"].items():
        value = args.get(prop)
        if p.get("required") or include_param(p, value):
            query[p.get("query", prop)] = js_string(value)
    qs = urllib.parse.urlencode(query)
    url = BASE + tool["path"] + (f"?{qs}" if qs else "")
    try:
        with urllib.request.urlopen(url, timeout=35) as response:
            return response.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        # An HTTP error status still carries the bridge's response body (fetch semantics).
        return e.read().decode("utf-8", "replace")


# ---- JSON-RPC / MCP plumbing ------------------------------------------------------------

_write_lock = threading.Lock()


def write_message(obj):
    with _write_lock:
        sys.stdout.write(json.dumps(obj, ensure_ascii=False, separators=(",", ":")) + "\n")
        sys.stdout.flush()


def reply(msg_id, result):
    write_message({"jsonrpc": "2.0", "id": msg_id, "result": result})


def reply_error(msg_id, message, code=-32000):
    write_message({"jsonrpc": "2.0", "id": msg_id, "error": {"code": code, "message": message}})


def handle_message(msg):
    if not isinstance(msg, dict):
        return
    msg_id = msg.get("id")
    method = msg.get("method")
    params = msg.get("params") or {}

    # Notifications (no id) need no response.
    if msg_id is None:
        return

    try:
        if method == "initialize":
            requested = params.get("protocolVersion")
            reply(msg_id, {
                "protocolVersion": (requested if requested in SUPPORTED_PROTOCOL_VERSIONS
                                    else SUPPORTED_PROTOCOL_VERSIONS[0]),
                "capabilities": {"tools": {}},
                "serverInfo": {"name": "betterremote", "version": "1.0.0"},
            })

        elif method == "ping":
            reply(msg_id, {})

        elif method == "tools/list":
            reply(msg_id, {"tools": TOOL_LIST})

        elif method == "tools/call":
            name = params.get("name")
            args = params.get("arguments") or {}
            tool = TOOL_BY_NAME.get(name)
            if tool is None:
                reply(msg_id, {"content": [{"type": "text", "text": f"unknown tool: {name}"}],
                               "isError": True})
                return
            errors = validate_args(tool, args)
            if errors:
                reply(msg_id, {
                    "content": [{"type": "text",
                                 "text": f"invalid arguments for {name}: " + "; ".join(errors)}],
                    "isError": True})
                return
            try:
                text = call_bridge(tool, args)
                reply(msg_id, {"content": [{"type": "text", "text": text}]})
            except Exception as e:
                reply(msg_id, {
                    "content": [{"type": "text",
                                 "text": f"Bridge unreachable or failed: {e}. Is the game "
                                         "running with BetterRemote loaded?"}],
                    "isError": True})

        else:
            reply_error(msg_id, f"method not supported: {method}", -32601)
    except Exception as e:  # noqa: BLE001 — surface as a JSON-RPC error, never crash the loop
        reply_error(msg_id, str(e))


def handle_line(msg):
    # JSON-RPC batch: handle each entry in order (responses are emitted per line).
    if isinstance(msg, list):
        if not msg:
            reply_error(None, "invalid request: empty batch", -32600)
            return
        for entry in msg:
            handle_message(entry)
        return
    handle_message(msg)


def main():
    # Requests are handled concurrently (one thread per stdin line), so a slow bridge call
    # never blocks a parallel one; responses are matched by id, order is not guaranteed.
    workers = []
    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue
        try:
            msg = json.loads(line)
        except json.JSONDecodeError:
            continue
        worker = threading.Thread(target=handle_line, args=(msg,))
        worker.start()
        workers = [w for w in workers if w.is_alive()]
        workers.append(worker)
    for worker in workers:
        worker.join()


if __name__ == "__main__":
    main()
