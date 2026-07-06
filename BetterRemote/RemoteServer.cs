using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace BetterRemote
{
    public enum Interface
    {
        Local,  // loopback only
        All,    // every interface; on HTTP.SYS needs elevation or a urlacl
    }

    /// <summary>
    /// Local HTTP bridge into the game process. Unity APIs are main-thread-only, so every
    /// game-touching handler is queued and executed by <see cref="Pump"/> from the plugin's
    /// Update() loop; the HTTP worker thread awaits the result.
    /// </summary>
    public class RemoteServer
    {
        private readonly HttpListener listener = new HttpListener();
        private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
        private Thread serverThread = null;
        private volatile bool running = false;

        public int Port { get; }

        public Interface Bind { get; }

        public RemoteServer(int port, Interface bind = Interface.Local)
        {
            Port = port;
            Bind = bind;
        }

        // localhost alone binds ::1 only on some hosts; 127.0.0.1 covers IPv4 clients.
        private void AddLoopbackPrefixes()
        {
            listener.Prefixes.Add($"http://localhost:{Port}/");
            listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            listener.Prefixes.Add($"http://[::1]:{Port}/");
        }

        public void Start()
        {
            if (Bind == Interface.All)
            {
                // "+" requires a urlacl or an elevated process on HTTP.SYS only; Mono's
                // HttpListener is fully managed and binds it unconditionally. The catch still
                // falls back to loopback.
                listener.Prefixes.Add($"http://+:{Port}/");

                try
                {
                    listener.Start();
                }
                catch (HttpListenerException ex)
                {
                    Plugin.Core.Logger.LogWarning($"[http] all-interfaces bind failed ({ex.Message}) - "
                        + $"falling back to loopback; run elevated or add a urlacl for http://+:{Port}/");

                    listener.Prefixes.Clear();
                    AddLoopbackPrefixes();
                    listener.Start();
                }
            }
            else
            {
                AddLoopbackPrefixes();
                listener.Start();
            }

            running = true;

            serverThread = new Thread(AcceptLoop) { IsBackground = true, Name = "BetterRemote" };
            serverThread.Start();
        }

        public void Stop()
        {
            running = false;

            try
            {
                listener.Stop();
                listener.Close();
            }
            catch
            {
                // Ignored: shutting down.
            }
        }

        /// <summary>Executes queued handlers; called from the plugin's Update() on the main thread.</summary>
        public void Pump()
        {
            while (mainThreadQueue.TryDequeue(out var action))
            {
                action();
            }
        }

        private void AcceptLoop()
        {
            while (running)
            {
                HttpListenerContext context;

                try
                {
                    context = listener.GetContext();
                }
                catch
                {
                    break; // Listener stopped.
                }

                ThreadPool.QueueUserWorkItem(_ => Handle(context));
            }
        }

        private void Handle(HttpListenerContext context)
        {
            object result;
            var code = 200;

            try
            {
                var path = context.Request.Url.AbsolutePath.TrimEnd('/').ToLowerInvariant();
                var query = context.Request.QueryString;

                switch (path)
                {
                    case "/ping":
                        result = new { ok = true, plugin = MyPluginInfo.PLUGIN_NAME, version = MyPluginInfo.PLUGIN_VERSION };
                        break;

                    case "/status":
                        result = AwaitMain(GetStatus);
                        break;

                    case "/console":
                    {
                        var cmd = query["cmd"];

                        if (string.IsNullOrEmpty(cmd) && context.Request.HasEntityBody)
                        {
                            using (var reader = new StreamReader(context.Request.InputStream))
                            {
                                cmd = reader.ReadToEnd();
                            }
                        }

                        if (string.IsNullOrEmpty(cmd))
                        {
                            code = 400;
                            result = new { error = "missing cmd (query ?cmd=... or POST body)" };
                            break;
                        }

                        var accepted = AwaitMain(() => (object)DevConsole.SendConsoleCommand(cmd));
                        result = new { command = cmd, accepted };
                        break;
                    }

                    case "/find":
                    {
                        var name = query["name"] ?? "";
                        var radius = TryParseFloat(query["radius"], out var r) ? r : 0f;
                        var limit = int.TryParse(query["limit"], out var l) ? l : 25;

                        result = AwaitMain(() => FindObjects(name, radius, limit));
                        break;
                    }

                    case "/dump":
                    {
                        var name = query["name"];

                        if (string.IsNullOrEmpty(name))
                        {
                            code = 400;
                            result = new { error = "missing name" };
                            break;
                        }

                        result = AwaitMain(() => DumpObject(name));
                        break;
                    }

                    case "/spawn":
                    {
                        // Direct spawn via CraftData, bypassing DevConsole (whose commands are
                        // gated by botbenson in multiplayer). Same sequence as SpawnConsoleCommand.
                        var tech = query["tech"];
                        var count = int.TryParse(query["count"], out var c) ? c : 1;
                        var dist = TryParseFloat(query["dist"], out var d) ? d : 12f;

                        if (string.IsNullOrEmpty(tech))
                        {
                            code = 400;
                            result = new { error = "missing tech (TechType name, e.g. seatruck)" };
                            break;
                        }

                        result = AwaitCoroutine(tcs => SpawnRoutine(tech, count, dist, tcs));
                        break;
                    }

                    case "/log":
                    {
                        var lines = int.TryParse(query["lines"], out var n) ? n : 50;
                        var match = query["match"];

                        result = TailLog(lines, match);
                        break;
                    }

                    case "/teleport":
                    {
                        result = AwaitMain(() => Teleport(query["x"], query["y"], query["z"], query["to"]));
                        break;
                    }

                    case "/inventory":
                    {
                        var tech = query["tech"];
                        var count = int.TryParse(query["count"], out var c) ? c : 1;

                        if (string.IsNullOrEmpty(tech))
                        {
                            code = 400;
                            result = new { error = "missing tech (TechType name, e.g. titanium)" };
                            break;
                        }

                        result = AwaitCoroutine(tcs => InventoryRoutine(tech, count, tcs));
                        break;
                    }

                    case "/equip":
                    {
                        var tech = query["tech"];
                        var target = query["target"];

                        if (string.IsNullOrEmpty(tech) || string.IsNullOrEmpty(target))
                        {
                            code = 400;
                            result = new { error = "missing tech (module TechType) or target (vehicle GameObject name)" };
                            break;
                        }

                        result = AwaitCoroutine(tcs => EquipRoutine(tech, target, query["slot"], tcs));
                        break;
                    }

                    case "/invoke":
                    {
                        var type = query["type"];
                        var member = query["member"];

                        if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(member))
                        {
                            code = 400;
                            result = new { error = "missing type or member" };
                            break;
                        }

                        result = AwaitMain(() => InvokeMember(type, member, query["target"], query["args"]));
                        break;
                    }

                    case "/get":
                    case "/set":
                    {
                        var type = query["type"];
                        var member = query["member"];
                        var value = query["value"];

                        if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(member))
                        {
                            code = 400;
                            result = new { error = "missing type or member" };
                            break;
                        }

                        if (path == "/set" && value == null)
                        {
                            code = 400;
                            result = new { error = "missing value" };
                            break;
                        }

                        result = AwaitMain(() => AccessMember(type, member, query["target"], path == "/set" ? value : null));
                        break;
                    }

                    case "/ui":
                    {
                        var press = query["press"];
                        var index = int.TryParse(query["index"], out var idx) ? idx : 0;
                        var max = int.TryParse(query["limit"], out var m) ? m : 40;

                        result = AwaitMain(() => string.IsNullOrEmpty(press) ? ListClickables(max) : PressClickable(press, index));
                        break;
                    }

                    case "/input":
                    {
                        var key = query["key"];
                        var mouse = query["mouse"];
                        var look = query["look"];
                        var hold = int.TryParse(query["hold"], out var h) ? h : 80;
                        var frames = int.TryParse(query["frames"], out var fr) ? fr : 30;

                        result = AwaitMain(() => HandleInput(key, mouse, look, hold, frames));
                        break;
                    }

                    case "/hierarchy":
                    {
                        var root = query["root"];
                        var depth = int.TryParse(query["depth"], out var dd) ? dd : 2;
                        var max = int.TryParse(query["limit"], out var m) ? m : 300;

                        result = AwaitMain(() => Hierarchy(root, depth, max));
                        break;
                    }

                    case "/screenshot":
                    {
                        result = AwaitMain(() => Screenshot(query["file"]));
                        break;
                    }

                    default:
                        code = 404;
                        result = new
                        {
                            error = "unknown endpoint",
                            endpoints = new[]
                            {
                                "/ping", "/status", "/console?cmd=<DevConsole command>",
                                "/find?name=&radius=&limit=", "/dump?name=", "/log?lines=&match=",
                                "/spawn?tech=&count=&dist=", "/inventory?tech=&count=",
                                "/equip?tech=&target=&slot=", "/teleport?x=&y=&z=|to=<name>",
                                "/invoke?type=&member=&target=&args=",
                                "/get?type=&member=&target=", "/set?type=&member=&target=&value=",
                                "/ui?press=&index=&limit=", "/input?key=|mouse=|look=&hold=&frames=",
                                "/hierarchy?root=&depth=&limit=", "/screenshot?file="
                            }
                        };
                        break;
                }
            }
            catch (TimeoutException)
            {
                code = 504;
                result = new { error = "main thread did not answer within 10s (loading screen or heavy hitch?)" };
            }
            catch (Exception e)
            {
                code = 500;
                result = new { error = e.GetType().Name, message = e.InnerException?.Message ?? e.Message };
            }

            try
            {
                var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result, Formatting.Indented));

                context.Response.StatusCode = code;
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.Close();
            }
            catch
            {
                // Ignored: client disconnected.
            }
        }

        private object AwaitMain(Func<object> fn)
        {
            var tcs = new TaskCompletionSource<object>();

            mainThreadQueue.Enqueue(() =>
            {
                // A request abandoned by the HTTP thread (timeout below, client already
                // answered 504) is cancelled: running it late would apply the side effect
                // after the client saw a failure, and a stall's whole backlog would flush
                // in one frame on resume.
                if (tcs.Task.IsCompleted)
                {
                    return;
                }

                try
                {
                    tcs.TrySetResult(fn());
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            });

            if (!tcs.Task.Wait(TimeSpan.FromSeconds(10)))
            {
                tcs.TrySetCanceled();
                throw new TimeoutException();
            }

            return tcs.Task.Result;
        }

        /// <summary>Runs a coroutine on the main thread and waits for it to complete the TCS.</summary>
        private object AwaitCoroutine(Func<TaskCompletionSource<object>, IEnumerator> routine)
        {
            var tcs = new TaskCompletionSource<object>();

            mainThreadQueue.Enqueue(() =>
            {
                try
                {
                    Plugin.Core.StartCoroutine(routine(tcs));
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });

            if (!tcs.Task.Wait(TimeSpan.FromSeconds(30)))
            {
                throw new TimeoutException();
            }

            return tcs.Task.Result;
        }

        private static IEnumerator SpawnRoutine(string techName, int count, float dist, TaskCompletionSource<object> tcs)
        {
            if (!UWE.Utils.TryParseEnum<TechType>(techName, out var techType))
            {
                tcs.SetResult(new { error = $"unknown TechType '{techName}'" });
                yield break;
            }

            var request = CraftData.GetPrefabForTechTypeAsync(techType);
            yield return request;

            var prefab = request.GetResult();

            if (prefab == null)
            {
                tcs.SetResult(new { error = $"no prefab for TechType {techType}" });
                yield break;
            }

            var spawned = new List<object>();

            for (var i = 0; i < count; i++)
            {
                var instance = global::Utils.CreatePrefab(prefab, dist, i > 0);
                LargeWorldEntity.Register(instance);
                CrafterLogic.NotifyCraftEnd(instance, techType);
                instance.SendMessage("StartConstruction", SendMessageOptions.DontRequireReceiver);

                var position = instance.transform.position;
                spawned.Add(new { name = instance.name, position = new { x = position.x, y = position.y, z = position.z } });
            }

            tcs.SetResult(new { techType = techType.ToString(), spawned });
        }

        private static IEnumerator InventoryRoutine(string techName, int count, TaskCompletionSource<object> tcs)
        {
            if (!UWE.Utils.TryParseEnum<TechType>(techName, out var techType))
            {
                tcs.SetResult(new { error = $"unknown TechType '{techName}'" });
                yield break;
            }

            var request = new TaskResult<GameObject>();
            yield return CraftData.AddToInventoryAsync(techType, request, count);

            var item = request.Get();
            tcs.SetResult(new { techType = techType.ToString(), added = item != null ? item.name : null });
        }

        /// <summary>
        /// Installs an upgrade module into a vehicle's module equipment: creates the item in the
        /// player inventory, then equips it through the vanilla Equipment.AddItem transfer.
        /// </summary>
        private static IEnumerator EquipRoutine(string techName, string targetName, string slot, TaskCompletionSource<object> tcs)
        {
            if (!UWE.Utils.TryParseEnum<TechType>(techName, out var techType))
            {
                tcs.SetResult(new { error = $"unknown TechType '{techName}'" });
                yield break;
            }

            var transform = FindTransform(targetName);

            if (transform == null)
            {
                tcs.SetResult(new { error = $"no active GameObject matching '{targetName}'" });
                yield break;
            }

            var vehicle = transform.GetComponentInParent<Vehicle>();

            if (vehicle == null)
            {
                tcs.SetResult(new { error = $"'{transform.name}' has no Vehicle component" });
                yield break;
            }

            var equipment = vehicle.upgradesInput.equipment;

            if (string.IsNullOrEmpty(slot) && !equipment.GetFreeSlot(TechData.GetEquipmentType(techType), out slot))
            {
                tcs.SetResult(new { error = $"no free slot for {techType} on '{vehicle.name}'" });
                yield break;
            }

            var request = new TaskResult<GameObject>();
            yield return CraftData.AddToInventoryAsync(techType, request, 1, true);

            var created = request.Get();
            var pickupable = created != null ? created.GetComponent<Pickupable>() : null;

            if (pickupable == null || pickupable.inventoryItem == null)
            {
                tcs.SetResult(new { error = $"could not create item {techType}" });
                yield break;
            }

            var equipped = equipment.AddItem(slot, pickupable.inventoryItem);

            tcs.SetResult(new { equipped, techType = techType.ToString(), slot, vehicle = vehicle.name });
        }

        /// <summary>
        /// Virtual input: a key or mouse-button press with down/held/up semantics, or a mouse
        /// look delta sustained for N frames. No parameters returns the current virtual state.
        /// </summary>
        private static object HandleInput(string key, string mouse, string look, int holdMs, int frames)
        {
            if (!string.IsNullOrEmpty(mouse))
            {
                switch (mouse.ToLowerInvariant())
                {
                    case "left": case "0": key = "Mouse0"; break;
                    case "right": case "1": key = "Mouse1"; break;
                    case "middle": case "2": key = "Mouse2"; break;
                    default: return new { error = $"unknown mouse button '{mouse}' (left/right/middle)" };
                }
            }

            if (!string.IsNullOrEmpty(key))
            {
                if (!UWE.Utils.TryParseEnum<KeyCode>(key, out var keyCode))
                {
                    return new { error = $"unknown KeyCode '{key}'" };
                }

                VirtualInput.Press(keyCode, holdMs);
                return new { pressed = keyCode.ToString(), holdMs };
            }

            if (!string.IsNullOrEmpty(look))
            {
                var parts = look.Split('|');

                if (parts.Length != 2 || !TryParseFloat(parts[0], out var dx) || !TryParseFloat(parts[1], out var dy))
                {
                    return new { error = "look must be dx|dy (e.g. 40|0)" };
                }

                VirtualInput.Look(dx, dy, frames);
                return new { look = new { dx, dy }, frames };
            }

            return VirtualInput.Status();
        }

        private static object Teleport(string x, string y, string z, string to)
        {
            var player = Player.main;

            if (player == null)
            {
                return new { error = "not in game" };
            }

            Vector3 destination;

            if (!string.IsNullOrEmpty(to))
            {
                var target = FindTransform(to);

                if (target == null)
                {
                    return new { error = $"no active GameObject matching '{to}'" };
                }

                destination = target.position + target.forward * 2f;
            }
            else if (TryParseFloat(x, out var px) && TryParseFloat(y, out var py) && TryParseFloat(z, out var pz))
            {
                destination = new Vector3(px, py, pz);
            }
            else
            {
                return new { error = "missing coordinates (x, y, z) or target (to)" };
            }

            player.SetPosition(destination);

            var position = player.transform.position;
            return new { teleported = true, position = new { x = position.x, y = position.y, z = position.z } };
        }

        private const BindingFlags MEMBER_FLAGS =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private static object InvokeMember(string typeName, string member, string target, string rawArgs)
        {
            var type = ResolveType(typeName);

            if (type == null)
            {
                return new { error = $"unknown type '{typeName}'" };
            }

            var instance = ResolveInstance(type, target, out var targetError);

            if (targetError != null)
            {
                return targetError;
            }

            // Dotted member paths walk intermediate fields/properties before the leaf method
            // (e.g. member=quickSlots.SelectImmediate from Inventory target=main), mirroring
            // AccessMember so methods on nested instances become invokable.
            var segments = member.Split('.');
            var ownerType = type;
            var owner = instance;

            for (var i = 0; i < segments.Length - 1; i++)
            {
                if (!TryGetAccessor(ownerType, segments[i], out var hopField, out var hopProperty))
                {
                    return new { error = $"{ownerType.Name} has no field or property '{segments[i]}'" };
                }

                var hopStatic = hopField != null ? hopField.IsStatic : (hopProperty.GetGetMethod(true) ?? hopProperty.GetSetMethod(true)).IsStatic;

                if (!hopStatic && owner == null)
                {
                    return new { error = $"{ownerType.Name}.{segments[i]} is an instance member: pass target=main or target=<GameObject name>" };
                }

                owner = hopField != null ? hopField.GetValue(hopStatic ? null : owner) : hopProperty.GetValue(hopStatic ? null : owner);

                if (owner == null)
                {
                    return new { error = $"{ownerType.Name}.{segments[i]} is currently null" };
                }

                ownerType = owner.GetType();
            }

            var leaf = segments[segments.Length - 1];
            var pieces = string.IsNullOrEmpty(rawArgs) ? new string[0] : rawArgs.Split(',');

            // Fewest parameters first, so an exact arg-count match wins over an optional-tail fill.
            foreach (var method in ownerType.GetMethods(MEMBER_FLAGS).Where(m => m.Name == leaf).OrderBy(m => m.GetParameters().Length))
            {
                var parameters = method.GetParameters();

                // Unsupplied trailing parameters are legal when optional (Play(string, int = 0)
                // invoked with one arg): they take the default the signature declares.
                if (pieces.Length > parameters.Length || parameters.Skip(pieces.Length).Any(p => !p.IsOptional))
                {
                    continue;
                }

                var args = new object[parameters.Length];
                var convertible = true;

                for (var i = 0; i < parameters.Length && convertible; i++)
                {
                    if (i < pieces.Length)
                    {
                        convertible = TryConvert(pieces[i], parameters[i].ParameterType, out args[i]);
                    }
                    else
                    {
                        args[i] = parameters[i].DefaultValue;
                    }
                }

                if (!convertible)
                {
                    continue;
                }

                if (!method.IsStatic && owner == null)
                {
                    return new { error = $"{ownerType.Name}.{leaf} is an instance method: pass target=main or target=<GameObject name>" };
                }

                var value = method.Invoke(method.IsStatic ? null : owner, args);
                var signature = string.Join(", ", parameters.Select(p => p.ParameterType.Name));

                return new { invoked = $"{ownerType.Name}.{method.Name}({signature})", result = Describe(value) };
            }

            return new { error = $"no {ownerType.Name}.{leaf} overload taking {pieces.Length} parseable argument(s)" };
        }

        private static object AccessMember(string typeName, string member, string target, string value)
        {
            var type = ResolveType(typeName);

            if (type == null)
            {
                return new { error = $"unknown type '{typeName}'" };
            }

            var instance = ResolveInstance(type, target, out var targetError);

            if (targetError != null)
            {
                return targetError;
            }

            // Dotted member paths walk intermediate fields/properties before the leaf
            // (e.g. member=Core.GlobalSettings.LinkedStorage starting from a plugin type).
            var segments = member.Split('.');
            var ownerType = type;
            var owner = instance;

            for (var i = 0; i < segments.Length - 1; i++)
            {
                if (!TryGetAccessor(ownerType, segments[i], out var hopField, out var hopProperty))
                {
                    return new { error = $"{ownerType.Name} has no field or property '{segments[i]}'" };
                }

                var hopStatic = hopField != null ? hopField.IsStatic : (hopProperty.GetGetMethod(true) ?? hopProperty.GetSetMethod(true)).IsStatic;

                if (!hopStatic && owner == null)
                {
                    return new { error = $"{ownerType.Name}.{segments[i]} is an instance member: pass target=main or target=<GameObject name>" };
                }

                owner = hopField != null ? hopField.GetValue(hopStatic ? null : owner) : hopProperty.GetValue(hopStatic ? null : owner);

                if (owner == null)
                {
                    return new { error = $"{ownerType.Name}.{segments[i]} is currently null" };
                }

                ownerType = owner.GetType();
            }

            var leaf = segments[segments.Length - 1];

            if (!TryGetAccessor(ownerType, leaf, out var field, out var property))
            {
                return new { error = $"{ownerType.Name} has no field or property '{leaf}'" };
            }

            var memberType = field != null ? field.FieldType : property.PropertyType;
            var isStatic = field != null ? field.IsStatic : (property.GetGetMethod(true) ?? property.GetSetMethod(true)).IsStatic;

            if (!isStatic && owner == null)
            {
                return new { error = $"{ownerType.Name}.{leaf} is an instance member: pass target=main or target=<GameObject name>" };
            }

            var leafOwner = isStatic ? null : owner;

            if (value == null)
            {
                var current = field != null ? field.GetValue(leafOwner) : property.GetValue(leafOwner);
                return new { member = $"{ownerType.Name}.{leaf}", type = memberType.Name, value = Describe(current) };
            }

            if (!TryConvert(value, memberType, out var converted))
            {
                return new { error = $"cannot convert '{value}' to {memberType.Name}" };
            }

            if (field != null)
            {
                field.SetValue(leafOwner, converted);
            }
            else
            {
                property.SetValue(leafOwner, converted);
            }

            return new { member = $"{ownerType.Name}.{leaf}", set = Describe(converted) };
        }

        private static bool TryGetAccessor(Type type, string name, out FieldInfo field, out PropertyInfo property)
        {
            // Walk the base chain explicitly so private members declared on ancestors resolve too.
            for (var current = type; current != null; current = current.BaseType)
            {
                field = current.GetField(name, MEMBER_FLAGS | BindingFlags.DeclaredOnly);

                if (field != null)
                {
                    property = null;
                    return true;
                }

                property = current.GetProperty(name, MEMBER_FLAGS | BindingFlags.DeclaredOnly);

                if (property != null)
                {
                    return true;
                }
            }

            field = null;
            property = null;
            return false;
        }

        private static Type ResolveType(string name)
        {
            // Full-name matches win over short-name matches across ALL assemblies, so a
            // same-named type in an earlier assembly (e.g. UWE.PlatformUtils) cannot shadow
            // the requested one (PlatformUtils).
            Type shortMatch = null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch
                {
                    continue; // Ignored: unloadable assembly.
                }

                var match = types.FirstOrDefault(t => t.FullName == name);

                if (match != null)
                {
                    return match;
                }

                shortMatch = shortMatch ?? types.FirstOrDefault(t => t.Name == name);
            }

            return shortMatch;
        }

        /// <summary>
        /// Resolves the instance a member is invoked on: null for static, the type's static
        /// main/instance singleton, or a component on a GameObject found by name substring.
        /// </summary>
        private static object ResolveInstance(Type type, string target, out object error)
        {
            error = null;

            if (string.IsNullOrEmpty(target) || target.Equals("static", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (target.Equals("main", StringComparison.OrdinalIgnoreCase) || target.Equals("instance", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var name in new[] { "main", "Main", "instance", "Instance", "_main", "_instance" })
                {
                    var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    if (property != null)
                    {
                        var value = property.GetValue(null);

                        if (value != null)
                        {
                            return value;
                        }

                        error = new { error = $"{type.Name}.{name} is currently null" };
                        return null;
                    }

                    var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    if (field != null)
                    {
                        var value = field.GetValue(null);

                        if (value != null)
                        {
                            return value;
                        }

                        error = new { error = $"{type.Name}.{name} is currently null" };
                        return null;
                    }
                }

                error = new { error = $"{type.Name} has no static main/instance member" };
                return null;
            }

            var transform = FindTransform(target);

            if (transform == null)
            {
                error = new { error = $"no active GameObject matching '{target}'" };
                return null;
            }

            var component = transform.GetComponent(type);

            if (component == null)
            {
                error = new { error = $"'{transform.name}' has no {type.Name} component" };
                return null;
            }

            return component;
        }

        private static bool TryConvert(string raw, Type type, out object value)
        {
            value = null;
            raw = raw?.Trim();

            try
            {
                // Nullable parameters convert through their underlying type; the literal null
                // keeps them null.
                var underlying = Nullable.GetUnderlyingType(type);

                if (raw == "null" && (!type.IsValueType || underlying != null))
                {
                    // Literal null for reference-type and nullable parameters (e.g. an unused
                    // HandTargetEventData).
                    return true;
                }

                if (underlying != null)
                {
                    return TryConvert(raw, underlying, out value);
                }

                if (type == typeof(string))
                {
                    value = raw;
                }
                else if (type.IsEnum)
                {
                    value = Enum.Parse(type, raw, true);
                }
                else if (type == typeof(bool))
                {
                    value = bool.Parse(raw);
                }
                else if (type == typeof(int))
                {
                    value = int.Parse(raw);
                }
                else if (type == typeof(long))
                {
                    value = long.Parse(raw);
                }
                else if (type == typeof(float) && TryParseFloat(raw, out var f))
                {
                    value = f;
                }
                else if (type == typeof(double) && TryParseDouble(raw, out var d))
                {
                    value = d;
                }
                else if (type == typeof(Vector3))
                {
                    // Pipe-separated so it survives inside the comma-separated args list.
                    var parts = raw.Split('|');

                    if (TryParseFloat(parts[0], out var vx) && TryParseFloat(parts[1], out var vy) && TryParseFloat(parts[2], out var vz))
                    {
                        value = new Vector3(vx, vy, vz);
                    }
                }

                return value != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Invariant culture first (dot decimals, what scripts and the MCP layer emit), then the
        /// OS culture (comma decimals on an Italian host). NumberStyles.Float excludes thousands
        /// separators, so the wrong-culture decimal mark fails the parse instead of silently
        /// scaling the number (e.g. "0.87" read as 87).
        /// </summary>
        private static bool TryParseFloat(string raw, out float value)
        {
            value = 0f;

            return raw != null
                && (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                    || float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value));
        }

        private static bool TryParseDouble(string raw, out double value)
        {
            value = 0d;

            return raw != null
                && (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                    || double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value));
        }

        private static object Describe(object value)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                if (value is UnityEngine.Object obj)
                {
                    return $"{obj.GetType().Name}:{obj.name}";
                }

                return value.ToString();
            }
            catch (Exception e)
            {
                return $"{value.GetType().Name} (unreadable: {e.GetType().Name})";
            }
        }

        private static IEnumerable<GameObject> ActiveClickables()
        {
            return UnityEngine.Object.FindObjectsOfType<MonoBehaviour>()
                .Where(mb => mb is IPointerClickHandler && mb.gameObject.activeInHierarchy)
                .Select(mb => mb.gameObject)
                .Distinct();
        }

        private static object ListClickables(int limit)
        {
            return ActiveClickables()
                .Take(limit)
                .Select(go => new { name = go.name, path = GetPath(go.transform) })
                .ToArray();
        }

        private static object PressClickable(string name, int index)
        {
            var matches = ActiveClickables()
                .Where(go => go.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(go => go.name.Length) // Prefer the closest name match.
                .ThenBy(go => GetPath(go.transform), StringComparer.OrdinalIgnoreCase)
                .ThenBy(go => go.transform.GetSiblingIndex()) // Same-named siblings in UI order.
                .ToArray();

            if (matches.Length == 0)
            {
                return new { error = $"no active clickable matching '{name}'" };
            }

            if (index < 0 || index >= matches.Length)
            {
                return new { error = $"index {index} out of range: {matches.Length} clickables match '{name}'" };
            }

            var target = matches[index];
            var eventData = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };

            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerEnterHandler);
            var handled = ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerClickHandler);

            return new { pressed = target.name, path = GetPath(target.transform), index, matches = matches.Length, handled };
        }

        private static object Hierarchy(string root, int depth, int limit)
        {
            var budget = new[] { limit };

            if (!string.IsNullOrEmpty(root))
            {
                var target = FindTransform(root);

                if (target == null)
                {
                    return new { error = $"no active GameObject matching '{root}'" };
                }

                return Node(target, depth, budget);
            }

            var scenes = new List<object>();

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (!scene.isLoaded)
                {
                    continue;
                }

                scenes.Add(new
                {
                    scene = scene.name,
                    roots = scene.GetRootGameObjects()
                        .TakeWhile(_ => budget[0] > 0)
                        .Select(go => Node(go.transform, depth, budget))
                        .ToArray()
                });
            }

            return scenes;
        }

        private static object Node(Transform transform, int depth, int[] budget)
        {
            budget[0]--;

            if (depth <= 0 || transform.childCount == 0 || budget[0] <= 0)
            {
                return new { name = transform.name, children = transform.childCount };
            }

            return new
            {
                name = transform.name,
                children = Enumerable.Range(0, transform.childCount)
                    .TakeWhile(_ => budget[0] > 0)
                    .Select(i => Node(transform.GetChild(i), depth - 1, budget))
                    .ToArray()
            };
        }

        private static object Screenshot(string file)
        {
            var path = string.IsNullOrEmpty(file)
                ? Path.Combine(BepInEx.Paths.GameRootPath, "BetterRemote.png")
                : file;

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            ScreenCapture.CaptureScreenshot(path);

            return new { capturing = path, note = "written at end of frame; poll for the file" };
        }

        private static Transform FindTransform(string name)
        {
            if (name.IndexOf('/') >= 0)
            {
                // Path lookup: match the leaf name exactly, then each ancestor segment (suffix match).
                var segments = name.Split('/');

                return UnityEngine.Object.FindObjectsOfType<Transform>()
                    .Where(t => MatchesPath(t, segments))
                    .FirstOrDefault();
            }

            return UnityEngine.Object.FindObjectsOfType<Transform>()
                .Where(t => t.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(t => t.name.Length) // Prefer the closest name match.
                .FirstOrDefault();
        }

        private static bool MatchesPath(Transform transform, string[] segments)
        {
            var current = transform;

            for (var i = segments.Length - 1; i >= 0; i--)
            {
                if (current == null || !current.name.Equals(segments[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                current = current.parent;
            }

            return true;
        }

        private static object GetStatus()
        {
            var player = Player.main;

            if (player == null)
            {
                return new { inGame = false, loading = WaitScreen.IsWaiting };
            }

            var position = player.transform.position;

            string biome;
            try { biome = player.GetBiomeString(); } catch { biome = "unknown"; }

            string vehicle = null;
            try
            {
                var v = player.GetVehicle();
                vehicle = v != null ? $"{v.GetType().Name}:{v.gameObject.name}" : null;
            }
            catch
            {
                // Ignored: no vehicle info available.
            }

            float dayScalar = 0f;
            try { dayScalar = DayNightCycle.main != null ? DayNightCycle.main.GetDayScalar() : 0f; } catch { }

            return new
            {
                inGame = true,
                loading = WaitScreen.IsWaiting,
                position = new { x = position.x, y = position.y, z = position.z },
                depth = player.GetDepth(),
                biome,
                vehicle,
                dayScalar
            };
        }

        private static object FindObjects(string name, float radius, int limit)
        {
            var origin = Player.main != null ? Player.main.transform.position : Vector3.zero;
            var matches = new List<(float distance, Transform transform)>();

            foreach (var transform in UnityEngine.Object.FindObjectsOfType<Transform>())
            {
                if (!string.IsNullOrEmpty(name) && transform.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var distance = Vector3.Distance(origin, transform.position);

                if (radius > 0f && distance > radius)
                {
                    continue;
                }

                matches.Add((distance, transform));
            }

            return matches
                .OrderBy(m => m.distance)
                .Take(limit)
                .Select(m => new
                {
                    name = m.transform.name,
                    path = GetPath(m.transform),
                    distance = Math.Round(m.distance, 1),
                    position = new
                    {
                        x = Math.Round(m.transform.position.x, 1),
                        y = Math.Round(m.transform.position.y, 1),
                        z = Math.Round(m.transform.position.z, 1)
                    },
                    components = m.transform.GetComponents<Component>()
                        .Where(c => c != null)
                        .Select(c => c.GetType().Name)
                        .ToArray()
                })
                .ToArray();
        }

        private static object DumpObject(string name)
        {
            var target = FindTransform(name);

            if (target == null)
            {
                return new { error = $"no active GameObject matching '{name}'" };
            }

            return new
            {
                name = target.name,
                path = GetPath(target),
                components = target.GetComponents<Component>()
                    .Where(c => c != null)
                    .Select(c => new
                    {
                        type = c.GetType().Name,
                        fields = DumpFields(c)
                    })
                    .ToArray()
            };
        }

        private static Dictionary<string, string> DumpFields(Component component)
        {
            var fields = new Dictionary<string, string>();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (var field in component.GetType().GetFields(flags).Take(40))
            {
                if (!IsSimple(field.FieldType))
                {
                    continue;
                }

                try
                {
                    fields[field.Name] = field.GetValue(component)?.ToString() ?? "null";
                }
                catch
                {
                    // Ignored: unreadable field.
                }
            }

            return fields;
        }

        private static bool IsSimple(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string)
                || type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4)
                || type == typeof(Color) || type == typeof(Quaternion);
        }

        private static string GetPath(Transform transform)
        {
            var path = transform.name;

            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }

        private static object TailLog(int lines, string match)
        {
            var path = Path.Combine(BepInEx.Paths.BepInExRootPath, "LogOutput.log");

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                var all = new List<string>();

                while (reader.ReadLine() is { } line)
                {
                    if (string.IsNullOrEmpty(match) || line.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        all.Add(line);
                    }
                }

                return new { lines = all.Skip(Math.Max(0, all.Count - lines)).ToArray() };
            }
        }
    }
}
