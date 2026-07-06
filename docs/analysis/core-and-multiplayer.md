# BetterSubnautica core framework & BetterNitrox multiplayer bridge

*Snapshot: 2026-07-03 — `develop`, game build BZ 1.22.53872, botbenson build of the same date.
Line references are valid for this snapshot.*

Technical analysis of the two foundational projects in the `BetterSubnautica.sln` solution, as of
the tip of `develop`, compared against the merge-base commit `06a354e` ("Updated projects for new
versions"). Scope: **`BetterSubnautica`** (shared plugin framework) and **`BetterNitrox`**
(multiplayer bridge towards the "botbenson" mod). Sibling docs: `lights-and-map.md`
(BetterLights/BetterMap) and `hud-and-quickslots.md` (BetterHUD/BetterQuickSlots).

Build matrix (`Directory.Build.props:2-22`): four configurations combine
`SUBNAUTICA`/`BELOWZERO` with `STABLE`/`MULTI`, defining preprocessor symbols `SUBNAUTICA`,
`BELOWZERO`, `STABLE`, `MULTI`, and the combined
`SUBNAUTICA_STABLE` / `SUBNAUTICA_MULTI` / `BELOWZERO_STABLE` / `BELOWZERO_MULTI`.
Decompiled game + botbenson sources used for verification live in `D:\Projects\Subnautica\BelowZero\`
(read-only reference, Below Zero 1.22.53872).

---

## 1. BetterSubnautica — core framework

### 1.1 Purpose and architecture

`BetterSubnautica` is the shared foundation every other plugin (`BetterLights`, `BetterMap`,
`BetterNitrox`, ...) builds on: a `BepInEx` plugin base class, a Harmony patch-ordering system, a
singleton/MonoBehaviour toolkit, and a debug-overlay subsystem for vehicles and tools.

**Plugin bootstrap**

- `Plugins/SubnauticaPlugin.cs:10-55` — abstract base `SubnauticaPlugin : BaseUnityPlugin`. The
  constructor (`SubnauticaPlugin.cs:22-40`) resolves `Name`/`Version`/`Location` from the
  concrete plugin's assembly and creates a dedicated `Harmony` instance named after it. `Awake()`
  (`SubnauticaPlugin.cs:42-47`) logs and calls the virtual `ApplyPatches()`
  (`SubnauticaPlugin.cs:49-54`), whose default implementation runs
  `HarmonyUtility.PrePatchAll` → `PatchAll` → `PostPatchAll` in sequence. `BetterSubnautica/Plugin.cs:8-19`
  and `BetterNitrox/Plugin.cs:17-29` both derive from it; `BetterNitrox` overrides `ApplyPatches()`
  to interleave botbenson loading between the pre- and normal/post patch phases (see §2.3).
- `Using.cs:1` — `global using static BetterSubnautica.Plugin;` (and the equivalent per-project
  file, e.g. `BetterNitrox/Using.cs:1`) exposes the plugin's static `Core` property unqualified
  project-wide, which is why patch classes can write `Core.Logger`/`Core.Settings`/`Core.Harmony`
  directly.

**Patch ordering: `[PrePatch]`/`[PostPatch]` + `HarmonyUtility`**

- `Attributes/PrePatchAttribute.cs:6-9` and `Attributes/PostPatchAttribute.cs:6-9` — empty marker
  attributes (`AttributeTargets.Class`, `Inherited = false`) with no behaviour of their own; they
  only exist to be detected by `HarmonyUtility`.
- `Utility/HarmonyUtility.cs:10-27` (`PrePatchAll`), `:29-46` (`PatchAll`), `:48-65`
  (`PostPatchAll`) — each scans `AccessTools.GetTypesFromAssembly(assembly)` and calls
  `harmony.CreateClassProcessor(type).Patch()` on: types carrying `[PrePatch]` (PrePatchAll),
  types carrying neither attribute (PatchAll, the default case), or types carrying `[PostPatch]`
  (PostPatchAll). This lets a plugin patch some Harmony targets *before* the rest of its own
  patches are applied — used by `BetterNitrox` to neutralize botbenson's anti-tamper/log/tools
  hooks before invoking botbenson's own bootstrap (see §2).

**Singleton/MonoBehaviour toolkit**

- `MonoBehaviours/AbstractAwakeSingleton.cs` — generic `MonoBehaviour` singleton: exposes
  `Instance` (auto-property), assigns it in `Awake()` (destroying duplicate instances), and — new
  since `06a354e` — clears it in the added `OnDestroy()` override so a destroyed instance doesn't
  leak a stale `Instance` reference.
- `MonoBehaviours/AbstractGenericSingleton.cs:5-19` — lazily spawns its own `GameObject` the first
  time `Instance` is accessed (`AbstractGenericSingleton.cs:12-16`), for singletons that are not
  attached to an existing game object by a patch.
- `MonoBehaviours/AbstractSingletonContainer.cs:6-28` — adds a `Dict` keyed collection on top of
  `AbstractGenericSingleton`, with a periodic `Update()` sweep (every `updateInterval` seconds,
  default 60s, `AbstractSingletonContainer.cs:13-27`) that prunes entries whose value went `null`
  (destroyed Unity objects).

**Debug overlay subsystem**

- `MonoBehaviours/DebuggerController.cs:8-121` — the on-screen overlay itself: a
  `AbstractSingletonContainer<DebuggerController, string, Message>` whose `Dict` holds keyed
  messages and whose `List` holds unkeyed ones (`DebuggerController.cs:18-19`). `OnGUI()`
  (`:70-73`) draws the aggregated `Text` (`:25-44`) with `GUI.Label`. `Update()` (`:60-68`) scales
  the font to screen resolution and clears everything on `KeyCode.Delete`. `ShowWarning`
  (`:74-77`) raises an always-visible HUD toast via `ErrorMessage.AddWarning` on top of the
  keyed overlay message. Exposed through `Utility/DebuggerUtility.cs:7-20`
  (`ShowWarning`/`ShowMessage`/`RemoveMessage`), a thin static façade over
  `DebuggerController.Instance`.
- `MonoBehaviours/Debug/AbstractDebuggerController.cs:7-135` — generic
  `AbstractDebuggerController<T> where T : Component`: caches the target `Component`
  (`:12-23`), and exposes the abstract contract every concrete controller implements —
  `LightsType`, `LightsActive`, `Capacity`, `Charge` — plus computed `EnergyPerSecond`
  (delta between updates, `:30-42`) and `PercentCharge` (`:52`). `Update()` (`:75-97`) shows/hides
  the overlay lines based on `Core.Settings.ShowDebugInfo && ShowDebugInfo`, refreshing energy
  stats once per second (`UpdateInfo`, `:99-109`). `ShowMessages()`/`DeleteMessages()`
  (`:111-133`) push/remove the four overlay lines (`LightsStatus`, `EnergyPerSecond`,
  `AvailableEnergy`, a blank `ZZZ` separator) through `DebuggerUtility`, gated by the virtual
  `ShowLights` flag (`:25`, default `true`).
- Concrete controllers under `MonoBehaviours/Debug/`: `CyclopsDebuggerController` (SUBNAUTICA
  only), `ExosuitDebuggerController`, `FlashlightDebuggerController`,
  `FlashlightHelmetDebuggerController` (BELOWZERO only), `HoverbikeDebuggerController`
  (BELOWZERO only), `MapRoomCameraDebuggerController`, `SeaglideDebuggerController`,
  `SeamothDebuggerController` (SUBNAUTICA only), `SeatruckDebuggerController` (BELOWZERO only),
  `SubRootDebuggerController`. Each resolves its own energy source (`EnergyMixin` /
  `EnergyInterface` / `PowerRelay`) and lights parent (via `ComponentExtensions.GetLightsParent`,
  `GetEnergySource`, etc.) and is attached by a matching `Patches/Debug/*.cs` Postfix on the
  target component's `Awake`/`Start` (e.g. `Patches/Debug/ExosuitPatches.cs:10-17`,
  `Patches/Debug/HoverbikePatches.cs:11-18`, `Patches/Debug/SeatruckPatches.cs:11-20` which only
  attaches the controller to the segment where `SeaTruckSegment.GetHead(instance) == instance`,
  i.e. the main segment). **Since `06a354e`, `CyclopsDebuggerController` inherits from
  `SubRootDebuggerController`** instead of `AbstractDebuggerController<SubRoot>` directly
  (`MonoBehaviours/Debug/CyclopsDebuggerController.cs:7`), reusing its `Capacity`/`Charge`
  (`PowerRelay`-based) implementation instead of duplicating it, re-overriding `ShowLights` to
  `true` (`CyclopsDebuggerController.cs:11`) so the Cyclops keeps its lights-status overlay
  line while plain `SubRoot` bases stay hidden.
- `Settings.cs:7-49` — the debug menu (`ConfigFile`/`[Menu]` from Nautilus): a global
  `ShowDebugInfo` toggle plus one per-vehicle/tool toggle, several gated by `#if
  SUBNAUTICA`/`#elif BELOWZERO`/`#if BELOWZERO` (e.g. `CyclopsInfo` SUBNAUTICA-only,
  `SeamothInfo`/`SeatruckInfo` mutually exclusive, `HoverbikeInfo`/`FlashlightHelmetInfo`
  BELOWZERO-only).

**Utility and Extensions**

- `Utility/CoroutineUtility.cs:9-17` — the `WaitUntil` coroutine helper with an optional
  completion callback (consumed by `BetterQuickSlots`).
- `Utility/GraphicsUtility.cs:5-11` — forwards to `GraphicsUtil.onQualityLevelChanged`.
- `Utility/HarmonyUtility.cs` — see above.
- `Utility/KeyCodeUtility.cs` — key-binding display/lookup helpers (`GetName`, `SetKeyCode`)
  consumed by `BetterQuickSlots/Utility/SlotsUtility.cs`, split per game
  (`#if SUBNAUTICA`/`#elif BELOWZERO`): the SN branch converts `KeyCode`s to InputSystem
  control paths for `GameInput.SetBinding`/`GetDisplayText`, the BZ branch rides the legacy
  `GameInput.inputs` list and `SetBindingInternal`.
- `Utility/uGUIUtility.cs:5-8` — caches the tab index of the Accessibility/General/Graphics/
  Keyboard option panels, populated by `Patches/uGUIPatches.cs`
  (`uGUI_TabbedControlsPanel.AddTab` Postfix; the keyboard case matches the tab labeled
  "Keyboard" on BZ and "Input" on SN).
- `Extensions/ComponentExtensions.cs` — the most-used extension set: `GetLightsInChildren`
  (`:42-45`, new since `06a354e`), `GetToggleLights` (`:47-60`, now also checks children, not just
  a `SeaMoth`-specific special case), `GetLightsParent` (`:62-85`, BELOWZERO branch added for
  `SeaTruckSegment.seatruckLights.floodLight`), `GetEnergyMixin`/`GetEnergyInterface`/
  `GetPowerRelay`/`GetEnergySource` (wraps the first matching energy source into an
  `IEnergySource`), and `CopyValues<T>` (`:122-156`, Harmony `Traverse`-based field/property copy
  between two components of the same type, used for component "reinstantiation" patterns).
- `Extensions/CyclopsExternalCamsExtensions.cs:8-19` (SUBNAUTICA only) — `GetLightState`/
  `SetLightState` via `Traverse` on the private `lightState` field.
- `Extensions/DockableExtensions.cs:6-14` (BELOWZERO only) — `Dockable.HasEnergySource()`.
- `Extensions/IntExtensions.cs:5-8` — `int.ToHex()`.
- `Extensions/SeaTruckSegmentExtensions.cs:7-10` (BELOWZERO only) — `IsMainSegment()`.
- `Extensions/StringExtensions.cs:8-23` — `IsNullOrWhiteSpace`, `Base64Encode`/`Base64Decode`.
- `Extensions/VFXVolumetricLightExtensions.cs:7-26` — `IsInitialized()`/`UpdateMaterial()` helpers
  for `VFXVolumetricLight` (used by `BetterLights`' volumetric-light controllers).
- `Extensions/VehicleExtensions.cs:5-35` — `HasEnergySource`, `GetEnergyScalar`
  (charge/capacity ratio).
- `Components/AbstractEnergySource.cs:5-24` + `EnergyMixinSource`/`EnergyInterfaceSource`/
  `PowerRelaySource`/`IEnergySource` — a small adapter layer unifying the three different native
  energy-holding components (`EnergyMixin`, `EnergyInterface`, `PowerRelay`) behind one
  `HasEnergy()`/`GetValues(out charge, out capacity)`/`ConsumeEnergy(amount)` interface.
- `Helpers/Timerwatch.cs:6-24` — a `Stopwatch` subclass with a seconds-based `IsFinished()`
  check and a `ForceFinished` escape hatch; used by `BetterMap`'s `SaveController` and
  `BetterLights`' `AbstractLightsController`.
- `Patches/WeatherManagerPatches.cs:6-14` (BELOWZERO only) — Prefix that disables
  `WeatherManager.DebugPrintAll` (returns `false`, suppressing a noisy debug log spam).
- `Patches/uGUIPatches.cs` — see `uGUIUtility` above.
- `Enums/CopyType.cs` (`Fields`/`Properties`/`All`, `[Flags]`) and `Enums/LightsType.cs`
  (`None`/`Internal`/`External`/`Camera`, `[Flags]`) — small flag enums consumed by
  `ComponentExtensions.CopyValues` and the debugger controllers respectively.

### 1.2 Features and behaviors offered

- On-screen debug overlay (`DebuggerController`) showing per-entity light state, energy
  delta/second, and charge/capacity for the currently loaded vehicles/tools.
- Per-vehicle/tool debug toggles configurable from the in-game Nautilus options menu
  (`Settings.cs`), gated to the vehicles that exist in the active game (SN vs BZ).
- Automatic debug-controller attachment via Harmony Postfix patches on each vehicle/tool's
  `Awake`/`Start` (Cyclops, Exosuit, Flashlight, Flashlight Helmet, Hoverbike, Map Room Camera,
  Seaglide, Seamoth, Seatruck, SubRoot).
- Unified energy-source abstraction (`IEnergySource` + adapters) so debug/utility code does not
  need to special-case `EnergyMixin` vs `EnergyInterface` vs `PowerRelay`.
- Harmony patch pre/post ordering via `[PrePatch]`/`[PostPatch]` + `HarmonyUtility`, enabling
  deterministic patch sequencing across plugins (critical for the multiplayer bridge).
- `WeatherManager.DebugPrintAll` noise suppression (Below Zero).
- Options-panel tab-index tracking (`uGUIUtility`) for other plugins to jump to a specific
  settings tab.
- The `WaitUntil` coroutine helper and a seconds-based stopwatch helper (`Timerwatch`).
- `Traverse`-based generic component field/property copying (`CopyValues`) for component
  swap/reinstantiation patterns used elsewhere in the mod family.

### 1.3 Changes since `06a354e`

Disabled / removed:

- **`Utility/PDAUtility.cs`** — deleted entirely (was: `FreezeBegin`/`FreezeEnd` +
  coroutine-delayed variants, pausing gameplay via `FreezeTime.Begin/End(FreezeTimeUtility.PDAId)`
  while the PDA is open), together with `FreezeTimeUtility` and
  `CoroutineUtility.WaitForMilliseconds`: the released games pause the PDA natively via
  `MiscSettings.pdaPause` (see `graphics-and-pda.md` §BetterPDA), so the manual freeze stack
  has no role and no callers.
- Most `MonoBehaviours/Debug/*DebuggerController.cs` files were **simplified, not emptied**: the
  verbose `if (x != null) { ... } return y;` property bodies for `LightsActive`/`Capacity`/
  `Charge` were rewritten as ternary/LINQ expression-bodied members (e.g.
  `MapRoomCameraDebuggerController.cs`, `SeaglideDebuggerController.cs`,
  `SeamothDebuggerController.cs`, `FlashlightDebuggerController.cs`,
  `FlashlightHelmetDebuggerController.cs`, `HoverbikeDebuggerController.cs`). Behaviour is
  preserved; this is a pure refactor, not a feature removal.
- **`CyclopsDebuggerController`** was refactored to inherit from `SubRootDebuggerController`
  instead of `AbstractDebuggerController<SubRoot>` directly, dropping its own duplicate
  `Capacity`/`Charge` (now inherited). `SubRootDebuggerController.ShowLights` is overridden to
  `false` (`SubRootDebuggerController.cs:10`, correct for a plain `SubRoot` — it has no lights
  of its own); `CyclopsDebuggerController` computes real `LightsType`/`LightsActive` values and
  re-overrides `ShowLights` to `true` (`CyclopsDebuggerController.cs:11`), keeping its
  lights-status overlay line active (verified live with two Cyclops:
  `docs/mods/bettersubnautica.md`).
- `SeatruckDebuggerController` lost its `Awake()` override that used to `Destroy(this)` on
  non-main segments; this is not a regression because
  `Patches/Debug/SeatruckPatches.cs:13-19` already only attaches the controller to the segment
  where `SeaTruckSegment.GetHead(instance) == instance` (the main segment).

Added:

- **`Helpers/Timerwatch.cs`** — new seconds-based stopwatch helper (§1.1), not yet wired to any
  patch.
- `AbstractAwakeSingleton.OnDestroy()` — clears `Instance` on destroy, closing a stale-reference
  leak.
- `ComponentExtensions.GetLightsInChildren<T>()` — new helper, and `GetLightsParent`/
  `GetToggleLights` were made more general (children lookup, `SeaTruckSegment` branch) instead of
  hard-coding a `SeaMoth`-only special case.
- Minor C# modernization across the diff (target-typed `new()`, pattern-matching `is { }`,
  expression bodies, `// Ignored` on previously silent empty `catch` blocks) — no behavior change.

---

## 2. BetterNitrox — multiplayer bridge

### 2.1 Purpose and architecture

`BetterNitrox` is the plugin that lets the BepInEx/Nautilus mod stack coexist with **botbenson**
(the Below Zero multiplayer mod, internal namespace `Subnautica.*`, distributed as its own
launcher/loader that normally does *not* go through BepInEx). It neutralizes botbenson's
anti-tamper checks, redirects its logging into the BepInEx log, and — on Below Zero — pre-runs
botbenson's own bootstrap sequence from inside BepInEx's plugin load, before the game's
`StartScreen` gets a chance to trigger the (crashing) path itself.

- `Plugin.cs:17-29` — `Plugin : SubnauticaPlugin`, `[BepInDependency(BetterSubnautica...)]`. Adds
  `NitroxLoaded` (event) and `IsNitroxLoaded` (bool) so other plugins can defer patches until the
  bridge is ready.
- `Plugin.cs:31-40` (`ApplyPatches` override) — subscribes `NitroxLoaded` to run
  `HarmonyUtility.PatchAll` + `PostPatchAll` for `BetterNitrox`'s own assembly (i.e. everything
  **not** marked `[PrePatch]`), then calls `LoadNitrox()`. The subscription comes first because
  `LoadNitrox()` raises the event synchronously on success, so the deferred patches (including
  `ZeroGameSetLightsActivePatch`) run as soon as the bridge is ready.
- `Patches/StartScreenPatches.cs`, `Patches/ToolsPatches.cs`, `Patches/LoggerPatches.cs` are all
  `[PrePatch]` — applied by `LoadNitrox()` via `HarmonyUtility.PrePatchAll` **before** botbenson's
  own `Loader.Run()` is invoked (see §2.2), which is what lets them intercept botbenson's startup
  behaviour instead of racing it.
- `Patches/ZeroGamePatches.cs` has no `[PrePatch]`/`[PostPatch]` attribute, so it is applied later,
  during the normal `PatchAll` pass triggered by `NitroxLoaded`.

### 2.2 `LoadNitrox()` flow (`Plugin.cs:42-116`)

Below Zero multiplayer build (`#if BELOWZERO`, `Plugin.cs:44-115`):

1. `Plugin.cs:47` — if `SubnauticaBootstrap.IsLoaded` is already `true`, skip (another loader
   already bootstrapped botbenson).
2. `Plugin.cs:51-52` — resolve `SubnauticaBootstrap.GetLoaderPath()` /
   `GetAPIFilePath()` (both point under
   `%AppData%\.botbenson\Subnautica Below Zero\Game\...`). `SubnauticaBootstrap` is **not** a
   class defined in this repo — it is a class botbenson itself injects into the game's
   `Assembly-CSharp.dll` (verified at
   `D:\Projects\Subnautica\BelowZero\Assembly-CSharp\SubnauticaBootstrap.cs:5-46`). Its members
   (`IsLoaded`, `GetLoaderPath`, `GetAPIFilePath`, `Load`) are declared `private`, but
   `Directory.Build.targets:59-60` publicizes `Assembly-CSharp`/`Assembly-CSharp-firstpass`
   (`BepInEx.AssemblyPublicizer`), which is what lets `BetterNitrox` call them directly instead of
   through reflection.
3. `Plugin.cs:58-59` — if both files exist, load `Subnautica.Loader.dll` and
   `Subnautica.API.dll` via `Assembly.Load(File.ReadAllBytes(...))` (byte-load, not
   `LoadFrom`, to avoid file locks).
4. `Plugin.cs:61-91` — resolve `Subnautica.Loader.Loader` by name and, if found:
   - `Plugin.cs:65` — invoke the **private static** `Loader.LoadDependencies()` via reflection
     (`BindingFlags.NonPublic | BindingFlags.Static`) — confirmed to exist at
     `Subnautica.Loader/Loader.cs:26-51`, it loads every DLL under `Game\Dependencies` except
     `DiscordRPCNativeNamedPipe`/`Subnautica.API` (already loaded).
   - `Plugin.cs:67-80` — a manual loop that resolves
     `Subnautica.API.Features.Paths.GetGamePluginsPath()` (confirmed public static,
     `Subnautica.API/Subnautica.API.Features/Paths.cs:145-152`) and `Assembly.Load`s every `.dll`
     in `Game\Plugins`, discarding the result without calling `Initialize()`/`OnEnabled()` on
     anything. This duplicates work `Loader.Run()` already does a few lines later (see next
     point) — `Loader.LoadPlugins()` (`Subnautica.Loader/Loader.cs:53-103`) is the real activation
     path (checksum de-dup, priority ordering, `Initialize`/`OnEnabled`), so this loop currently
     only pre-warms the assembly-load cache and is functionally redundant.
   - `Plugin.cs:82` — `HarmonyUtility.PrePatchAll(Harmony, GetType().Assembly, Logger)` — applies
     `StartScreenPatches`/`ToolsPatches`/`LoggerPatches` now, before botbenson's own code runs.
   - `Plugin.cs:84` — invoke `Loader.Run()` (public static, confirmed at
     `Subnautica.Loader/Loader.cs:20-24`): calls `LoadDependencies()` **again** (redundant with
     step 4a) then `LoadPlugins()`, which actually instantiates and activates every
     `SubnauticaPlugin`-derived class found in `Game\Plugins` (e.g. `Subnautica.Client`,
     `Subnautica.Events`).
   - `Plugin.cs:88` — sets `SubnauticaBootstrap.IsLoaded = true`, so that when the game's own
     `StartScreen.Start()` later calls `SubnauticaBootstrap.Load()`
     (`Assembly-CSharp/SubnauticaBootstrap.cs:24-45`), it becomes a no-op (`IsLoaded` guard at
     `SubnauticaBootstrap.cs:26-29`) — botbenson does not attempt to bootstrap itself a second
     time.
   - `Plugin.cs:89-90` — sets `IsNitroxLoaded = true` and raises `NitroxLoaded`.
5. If either file is missing, or `Loader`/type resolution fails, or any exception is thrown,
   `LoadNitrox()` logs a warning/error and returns without crashing the whole plugin load
   (`Plugin.cs:92-110`).

Non-multiplayer Subnautica/Below Zero build (`#else`, `Plugin.cs:111-115`): no botbenson
interaction at all — just `HarmonyUtility.PrePatchAll` followed by `IsNitroxLoaded = true` and
`OnNitroxLoaded()`, so plugins depending on `NitroxLoaded` still fire in single-player.

### 2.3 Root cause this bridge fixes, and how each patch addresses it

Verified against the decompiled, botbenson-patched `Assembly-CSharp.dll`
(`D:\Projects\Subnautica\BelowZero\Assembly-CSharp\StartScreen.cs`): botbenson overwrites
`SubnauticaZero_Data\Managed\Assembly-CSharp.dll` with its own multiplayer build (it does not
install through BepInEx). That build's `StartScreen.Awake()` calls `TryToShowDisclaimer()`
(`StartScreen.cs:97`), and `Start()` calls `SubnauticaBootstrap.Load()` (`StartScreen.cs:102`).
`TryToShowDisclaimer()` (`StartScreen.cs:307-330`) is botbenson's anti-tamper check: it walks the
process's loaded modules and, if it finds a file containing `"winhttp"` next to a module whose
extension starts with `.ex*` (`StartScreen.cs:323`), it **calls itself recursively**
(`StartScreen.cs:325`). Because BepInEx's proxy is `winhttp.dll` (doorstop), this recursion never
terminates under BepInEx — main-thread `StackOverflowException` inside `Awake()`, i.e. a black
screen before the main menu ever renders.

Patches, all `[PrePatch]` so they land before `StartScreen` runs:

- **`StartScreenPatches.cs:8-17`** — `[HarmonyPatch(typeof(StartScreen))]` /
  `[HarmonyPatch(nameof(StartScreen.TryToShowDisclaimer))]`, Prefix returns `false`: skips the
  method body entirely, so the anti-tamper recursion never starts. **This is the fix for the black
  screen.**
- **`ToolsPatches.cs:9-18`** (file renamed from `SubnauticaApiPatches.cs` since `06a354e`, same
  content) — `[HarmonyPatch(typeof(Subnautica.API.Features.Tools))]` /
  `[HarmonyPatch(nameof(Tools.IsBepinexInstalled))]`, Prefix forces `__result = false`. Confirmed
  at `Subnautica.API/Subnautica.API.Features/Tools.cs:473-503`: `IsBepinexInstalled()` checks for
  `doorstop_config.ini`, `winhttp.dll`, or a `BepInEx` folder next to `Assembly-CSharp.dll` and
  would otherwise make botbenson refuse to run under BepInEx.
- **`LoggerPatches.cs`** (new file since `06a354e`) — Prefix on `Subnautica.API.Features.Log.Send`
  and `Log.SendRaw`, both returning `false`, redirecting every botbenson log
  line into `Core.Logger.LogInfo/LogWarning/LogError` (BepInEx log) instead of botbenson's own
  file logger. Signatures confirmed against
  `Subnautica.API/Subnautica.API.Features/Log.cs:20-53` (`Send(string, LogLevel)`,
  `SendRaw(string)`) and `Subnautica.API.Enums.LogLevel` (`Info`/`Warn`/`Error`).
- **`ZeroGamePatches.cs:7-27`** — `[HarmonyPatch(typeof(ZeroGame))]` /
  `[HarmonyPatch(nameof(ZeroGame.SetLightsActive), typeof(ToggleLights), typeof(bool),
  typeof(bool))]` (the 3-argument overload, confirmed present at
  `Subnautica.API/Subnautica.API.Features/ZeroGame.cs:233-245`, disambiguated from the
  2-argument `SeaTruckLights` overload at `ZeroGame.cs:247-250`). This is botbenson's own
  server-authoritative "set vehicle lights" call (`LightProcessor.OnProcessCompleted` uses this
  overload for the Hoverbike-class `ToggleLights` path). The Prefix forwards the call to
  `IToggleLightsController` (now in `BetterSubnautica/Components/IToggleLightsController.cs` —
  moved out of BetterLights so BetterNitrox can reference it without a circular project
  dependency) and returns `false` when a controller is present, so multiplayer light-state
  changes drive `BetterLights`' controllers instead of the vanilla toggle; with no controller it
  logs a warning and falls through to the original. The patch is applied at runtime (BepInEx log:
  `Patched DMD<Subnautica.API.Features.ZeroGame::SetLightsActive>`).

### 2.4 TODOs found

- `Plugin.cs:11` — `// TODO: Gestire Subnautica Nitrox` ("handle Subnautica Nitrox"), a top-of-file
  comment noting that the (original, Subnautica-side) Nitrox mod integration is not implemented —
  `LoadNitrox()` only has a real implementation under `#if BELOWZERO` (botbenson); the `#else`
  branch (`Plugin.cs:111-115`) is a no-op passthrough for plain Subnautica/Subnautica-Nitrox.
- No other `// TODO` comments exist in `BetterNitrox`.

### 2.5 Features and behaviors offered

- Neutralizes botbenson's BepInEx anti-tamper check (`StartScreen.TryToShowDisclaimer`),
  preventing the stack-overflow/black-screen on startup.
- Forces botbenson's `Tools.IsBepinexInstalled` to `false` so it does not refuse to run alongside
  BepInEx.
- Pre-loads and runs botbenson's own bootstrap (`Subnautica.Loader.Loader.Run()`) from inside
  BepInEx's plugin `Awake`, before botbenson's own `StartScreen`-driven bootstrap would (avoiding
  a race and letting the pre-patches above land first).
- Also loads any extra `.dll` found in botbenson's `Game\Plugins` folder ahead of time (currently
  redundant with `Loader.Run()`, see §2.2).
- Redirects all of botbenson's internal logging into the BepInEx log window/file.
- Exposes a `NitroxLoaded` event / `IsNitroxLoaded` flag so other plugins (or future patches) can
  wait for the bridge before applying their own multiplayer-dependent patches.
- Routes multiplayer vehicle-light updates (`ZeroGame.SetLightsActive`) into `BetterLights`'
  controllers (`ZeroGamePatches`, applied at runtime — see §2.3).

### 2.6 Changes since `06a354e`

Added:

- **`LoggerPatches.cs`** — new file (§2.3).
- `Plugin.cs:65-80` — `LoadDependencies` reflection call + the manual `Game\Plugins` assembly-load
  loop (redundant with `Loader.Run()`, see §2.2).

Renamed (no content change):

- `SubnauticaApiPatches.cs` → `ToolsPatches.cs` (100% identical content, git-detected rename).

No feature was removed from `BetterNitrox` since `06a354e`; all changes are additive.

---

## 3. Compatibility status against the current decompiled game/botbenson build

All `[HarmonyPatch]` targets referenced by `BetterNitrox` were checked against the decompiled,
botbenson-patched sources in `D:\Projects\Subnautica\BelowZero\` (Below Zero 1.22.53872). None show
a discrepancy at this game/botbenson version:

| Patch | Target | Verified against | Result |
|---|---|---|---|
| `StartScreenPatches.cs:8-9` | `StartScreen.TryToShowDisclaimer` (private instance method) | `Assembly-CSharp/StartScreen.cs:307` | Match — method exists, no parameters. |
| `ToolsPatches.cs:9-10` | `Subnautica.API.Features.Tools.IsBepinexInstalled` (public static, no params) | `Subnautica.API/Subnautica.API.Features/Tools.cs:473` | Match. |
| `LoggerPatches.cs` (`LogSendPatch`) | `Subnautica.API.Features.Log.Send(string, LogLevel)` | `Subnautica.API/Subnautica.API.Features/Log.cs:50` | Match, exact signature. |
| `LoggerPatches.cs` (`LogSendRawPatch`) | `Subnautica.API.Features.Log.SendRaw(string)` | `Log.cs:55` | Match. |
| `ZeroGamePatches.cs:7-8` | `ZeroGame.SetLightsActive(ToggleLights, bool, bool)` | `Subnautica.API/Subnautica.API.Features/ZeroGame.cs:233` | Match — 3-arg overload correctly disambiguated from the 2-arg `SeaTruckLights` overload at `ZeroGame.cs:247`. |
| `Plugin.cs:61,65,84` | `Subnautica.Loader.Loader` (`LoadDependencies` private static, `Run` public static) | `Subnautica.Loader/Subnautica.Loader/Loader.cs:12-24,26` | Match. |
| `Plugin.cs:47,51,52,88` | `SubnauticaBootstrap.IsLoaded`/`GetLoaderPath`/`GetAPIFilePath` (game-injected, publicized) | `Assembly-CSharp/SubnauticaBootstrap.cs:5-46` | Match — members are `private` in source but accessible via `Assembly-CSharp`/`-firstpass` publicization (`Directory.Build.targets:59-60`). |
| `Plugin.cs:70` (`Game\Plugins` load loop) | `Subnautica.API.Features.Paths.GetGamePluginsPath()` | `Subnautica.API/Subnautica.API.Features/Paths.cs:145` | Match. |

No `[HarmonyPatch(typeof(X))]` target in `BetterNitrox` currently points at a missing type or
member — every patch is expected to apply cleanly against this game/botbenson build. The risk
surface for future breakage is these exact members changing shape on the *next* botbenson or game
update (they are all external, versioned independently of this repo): `StartScreen`,
`SubnauticaBootstrap`, and every `Subnautica.API`/`Subnautica.Loader` type are owned by botbenson,
not by Unknown Worlds, so a botbenson update is the most likely source of drift.

`BetterSubnautica`'s own `[HarmonyPatch]` targets (`SubRoot`, `Exosuit`, `PlayerTool`/`FlashLight`/
`FlashlightHelmet`/`Seaglide`, `MapRoomCamera`, `SeaMoth`, `SeaTruckSegment`, `Hoverbike`,
`WeatherManager`, `uGUI_TabbedControlsPanel`) all target vanilla Unknown Worlds game types (not
botbenson-owned), so they only break on a genuine game update, not on a botbenson update; these
were not individually re-verified line-by-line as part of this pass since none of them changed
target type/method name since `06a354e` (only the property-body refactors described in §1.3).

### SUBNAUTICA vs BELOWZERO differences

- `BetterSubnautica`: `CyclopsDebuggerController` and `SeamothDebuggerController` /
  `Patches/Debug/SeamothPatches.cs` are SUBNAUTICA-only; `HoverbikeDebuggerController`,
  `SeatruckDebuggerController`, `FlashlightHelmetDebuggerController` and their patches are
  BELOWZERO-only; `WeatherManagerPatches.cs` is BELOWZERO-only;
  `CyclopsExternalCamsExtensions.cs` is SUBNAUTICA-only; `DockableExtensions.cs`/
  `SeaTruckSegmentExtensions.cs` are BELOWZERO-only. `Settings.cs` mirrors the same split for the
  corresponding debug toggles.
- `BetterNitrox`: every patch file (`StartScreenPatches.cs`, `ToolsPatches.cs`,
  `LoggerPatches.cs`, `ZeroGamePatches.cs`) is guarded by `#if BELOWZERO_MULTI` — none of them
  compile, let alone apply, outside the Below Zero multiplayer configuration. `Plugin.cs`'s
  `LoadNitrox()` has a real implementation only under `#if BELOWZERO`; the Subnautica
  (non-Below-Zero) path is the documented TODO from §2.4.
