# BetterHUD and BetterQuickSlots — technical analysis

*Snapshot: 2026-07-03 — `develop`, game build BZ 1.22.53872, botbenson build of the same date.
Line references are valid for this snapshot.*

Analysis of `BetterHUD/` and `BetterQuickSlots/`. Cross-checked against the
decompiled Below Zero sources in `D:\Projects\Subnautica\BelowZero\` (reference game: Below Zero
1.22.53872, see `README.md`). The verification covers the Below Zero (BZ) variant; since
2026-07-05 the decompiled Subnautica 1 tree exists too (`D:\Projects\Subnautica\Subnautica\`)
and BetterHUD is verified live on SN1 as well (Phase 2 of the porting campaign).

---

## BetterHUD

> **Status: builds in all four configurations; verified in-game on BZ.MULTI (2026-07-03).** The
> wait loop in `TimeDisplayController.AsyncStart()` gates on `WaitScreen.IsWaiting` (the game's
> "loading in progress" signal) plus `LightmappedPrefabs`/`PAXTerrainController`/`uGUI`/
> `HandReticle` availability — the legacy `PAXTerrainController.isWorking` and `uGUI.isLoading`
> members are absent from the decompiled BZ build. Confirmed in a hosted MP survival world: the
> controller attaches, the loop exits, and the clock renders (`font=Aller_Rg, clock=03:06`) with
> no exceptions.

### 1. Purpose and architecture

- `BetterHUD/Plugin.cs:8` — `Plugin : SubnauticaPlugin`, `[BepInPlugin]`. Registers `Settings`
  via `Nautilus.Handlers.OptionsPanelHandler.RegisterModOptions<Settings>()`
  (`BetterHUD/Plugin.cs:10`).
- `BetterHUD/Settings.cs:7` — `Settings : ConfigFile`, Nautilus options: `ShowHUDClock`
  (toggle, line 10), `TimeFontSize` (slider 0-100, line 13), `TimeFontStyle` (4-value choice,
  line 16), `TimePositionX` (slider 0-512, line 19), `TimePositionY` (slider 0-256, line 22).
- `BetterHUD/MonoBehaviours/TimeDisplayController.cs:7` — `MonoBehaviour` attached to the
  player.
  - `Style` (lines 10-25) and `Position` (lines 28-43): properties that read live from
    `Core.Settings`.
  - `Text` (lines 45-55): computes the clock from `DayNightCycle.main.GetDayScalar()`.
  - `Start()`/`AsyncStart()`: coroutine that waits on
    `LightmappedPrefabs.main`/`IsWaitingOnLoads()`, `PAXTerrainController.main`, `uGUI.main`,
    `WaitScreen.IsWaiting` and `HandReticle.main` before initializing
    `Style`/`Position` from `HandReticle.main.compTextHand.font.sourceFontFile` (line 77) and
    setting `started = true` (line 87).
  - `OnGUI()` (lines 90-96): draws the clock label when
    `Core.Settings.ShowHUDClock && started`.
- `BetterHUD/Patches/PlayerPatches.cs:6-8` — `[HarmonyPatch(typeof(Player))]`
  `[HarmonyPatch(nameof(Player.Awake))]`, postfix `PlayerAwakePatch` (line 8) that adds
  `TimeDisplayController` to the player gameObject if absent (lines 10-16).
- `BetterHUD/Using.cs:1` — `global using static BetterHUD.Plugin` (alias `Core` for accessing
  `Settings`).

No dedicated `Utility` class in this project.

### 2. Features/behavior

- Shows a digital clock in `HH:mm` format overlaid on the HUD, computed from the game's
  day/night cycle (it is not the real system clock).
- Font size, style (Normal/Bold/Italic/Bold & Italic) and X/Y screen position are configurable
  from the Nautilus options menu ("Better HUD").
- On/off toggle for the entire clock HUD.

### 3. What changed since commit 06a354e

Commit `06a354e` **is** the QModManager→BepInEx migration commit for this project (in its diff
against the parent `d51c6e9`: removed legacy `Core.cs`, `mod.json`, `Properties/AssemblyInfo.cs`;
added a BepInEx-style `Plugin.cs`; removed the conditional branch
`#if SUBNAUTICA_STABLE ... HandReticle.main.interactPrimaryText.font ... #else
HandReticle.main.compTextHand.font.sourceFontFile ... #endif` in `TimeDisplayController.cs`,
keeping only the `compTextHand` branch). On top of the migration, the diff on `develop` touches
only the wait-loop condition in `TimeDisplayController.AsyncStart()` (the
`WaitScreen.IsWaiting`-based check described in §1 and the status note above).

### 4. Game-API notes (verified against the decompiled BZ)

- `Player.Awake()`, `DayNightCycle.main`/`GetDayScalar()`,
  `LightmappedPrefabs.main`/`IsWaitingOnLoads()`, `PAXTerrainController.main`, `uGUI.main` and
  `HandReticle.main`/`compTextHand` (a `TextMeshProUGUI`) all exist in the decompiled game.
- `PAXTerrainController` has no `isWorking` member (only debug bools), and `uGUI` has no
  `isLoading` bool (it exposes a `public uGUI_SceneLoading loading;` field instead).
  `WaitScreen.IsWaiting` (`WaitScreen.cs:80`) is the "loading in progress" signal the wait loop
  uses.

---

## BetterQuickSlots

> **Status: builds in all four configurations; verified in-game on BZ.MULTI (2026-07-03) and
> on SN.MULTI (2026-07-06, InputSystem reimplementation — see
> `docs/mods/betterquickslots.md`).** The solution publicizes `Assembly-CSharp`
> (`Directory.Build.targets:59`), so `nameof()` on private game members, direct field access,
> and private-method calls all compile as-is, and Mono skips IL visibility checks at runtime.
> The BZ wait condition in the `HandleInput` postfix uses `WaitScreen.IsWaiting`, and
> `KeyCodeUtility` (core project, split `#if SUBNAUTICA`/`#elif BELOWZERO`) provides the
> binding helpers — the BZ branch rides the string overload of `SetBindingInternal`
> (`GameInput.cs:375` in the BZ tree), the SN branch converts `KeyCode`s to InputSystem
> control paths for `GameInput.SetBinding`/`GetDisplayText`. `uGUIUtility` is a mod class
> (`BetterSubnautica/Utility/uGUIUtility.cs`). Verified in a hosted BZ MP survival world:
> controller attaches, `target` casts to `QuickSlots`, binding resizes 5→10, `Init` rebuilds 10
> icons, labels instantiate, bindings + tooltips update — no mod exceptions.

### 1. Purpose and architecture

- `BetterQuickSlots/Plugin.cs:8` — `Plugin : SubnauticaPlugin`, registers `Settings`.
- `BetterQuickSlots/Settings.cs:9` — `SlotCount` (slider 5-10, line 12), keybinds
  `Slot1`..`Slot10` (lines 15-42, each with `[OnChange(nameof(ForceUpdateQuickSlots))]`),
  `TextFontSize` (line 45), `TextOffsetY` (line 48). `ForceUpdateQuickSlots()` (lines 50-56)
  sets `QuickSlotsController.Instance.ForceUpdate = true`.
- `BetterQuickSlots/MonoBehaviours/QuickSlotsController.cs:13` —
  `AbstractAwakeSingleton<QuickSlotsController>`. `Component` (lines 16-26) obtains
  `uGUI_QuickSlots` via `GetComponent`. `Icons` (line 28) reads `component.icons`.
  `Target` (line 26) reads `component.target as QuickSlots`. `Update()` (lines 30-100): if
  `ForceUpdate`, resizes `Target.binding`, updates `Target.slotCount`, calls
  `component.Init(Target)`, then for each slot instantiates a text label from
  `HandReticle.main.compTextHand` (line 58 — a `TextMeshProUGUI` in both games; the current
  SN1 has no `interactPrimaryText`, so the old `SUBNAUTICA_STABLE` branch is gone) with the
  assigned key, finally calls `SlotsUtility.UpdateSlotBindings()` and
  `TooltipFactory.RefreshActionStrings()` (lines 89-90).
- `BetterQuickSlots/Patches/GameInputPatches.cs` — binding-write postfix that sets
  `ForceUpdate = true` when a binding changes: on BZ `[HarmonyPatch(typeof(GameInput))]` on
  the int overload of `SetBindingInternal`; on SN `[HarmonyPatch(typeof(GameInputSystem))]`
  on `SetBinding(Device, Button, BindingSet, string)` (the `IGameInput` implementation).
- `BetterQuickSlots/Patches/QuickSlotsPatch.cs:7-35` — three transpiler patches, all delegating
  to `SlotsUtility.Transpiler`:
  - `[HarmonyPatch(typeof(QuickSlots))] [HarmonyPatch(nameof(QuickSlots.Update))]`
    (lines 7-8)
  - `[HarmonyPatch(typeof(QuickSlots))] [HarmonyPatch(nameof(QuickSlots.SelectInternal))]`
    (lines 17-18)
  - `[HarmonyPatch(typeof(QuickSlots))] [HarmonyPatch(nameof(QuickSlots.DeselectInternal))]`
    (lines 27-28)
- `BetterQuickSlots/Patches/TooltipFactoryPatches.cs:10-11` —
  `[HarmonyPatch(typeof(TooltipFactory))]`
  `[HarmonyPatch(nameof(TooltipFactory.RefreshActionStrings))]`, postfix (lines 12-20) that
  starts `UpdateKeyRangeCoroutine` (lines 22-85), which rebuilds
  `TooltipFactory.stringKeyRange15` (line 84) with the configured key ranges.
- `BetterQuickSlots/Patches/uGUIPatches.cs:11-13` —
  `[HarmonyPatch(typeof(uGUI))] [HarmonyPatch(nameof(uGUI.Awake))]`, postfix `uGUIAwakePatch`
  (lines 14-25) that adds `QuickSlotsController` to the `uGUI.main.quickSlots` gameObject.
- `BetterQuickSlots/Patches/uGUIPatches.cs` — `[HarmonyPatch(typeof(uGUI_QuickSlots))]`
  `[HarmonyPatch(nameof(uGUI_QuickSlots.HandleInput))]`, postfix
  `uGUIQuickSlotsHandleInputPatch` that handles the slots beyond
  `SlotsUtility.VanillaSlotCount` and drives `__instance.target`: on SN it polls the custom
  `GameInput.Button`s (`GameInput.GetButtonDown/Held/Up` →
  `SlotKeyDown`/`SlotKeyHeld`/`SlotKeyUp`, gated like the vanilla loop on `uGUI.isIntro` and
  `IntroLifepodDirector.IsActive`), on BZ it reads via reflection the configured `KeyCode`s
  and polls legacy `Input.GetKeyDown/Up` → `SlotKeyDown`/`SlotKeyUp`.
- `BetterQuickSlots/Patches/uGUIPatches.cs:57-62` —
  `[HarmonyPatch(typeof(uGUI_TabbedControlsPanel))]` on the 5-parameter overload (with a 5th
  `out` param) of `AddBindingOption`, postfix `uGUITabbedControlsPanelAddBindingOptionPatch`
  (lines 63-83) that hides the redundant vanilla quick-slot entries from the options panel,
  using `uGUIUtility.KeyboardTabIndex` (line 67) to identify the keyboard tab.
- `BetterQuickSlots/Utility/SlotsUtility.cs` — exposes the 10 configurable `KeyCode`s and
  `VanillaSlotCount` (`uGUI_QuickSlots.quickSlotButtons.Length` on SN — the game has no
  `Player.quickSlotButtonsCount` — and `Player.quickSlotButtonsCount` on BZ), generates
  `SlotNames` dynamically by reading the `SliderAttribute` of `SlotCount`,
  `GetInputSlotName`, `UpdateSlotBindings` (writes the first `VanillaSlotCount` bindings into
  the game via `KeyCodeUtility.SetKeyCode`), `Transpiler` (remaps `QuickSlots.slotNames` to
  `SlotsUtility.SlotNames`).

### 2. Features/behavior

- Extends the number of quick slots from 6 (vanilla) up to 10, configurable via a slider.
- Lets you freely remap the key of each slot (1..10) from the options menu.
- Displays above each quick-slot icon a label with the assigned key (font size/offset
  configurable).
- Hides the redundant vanilla quick-slot entries in the key-binding panel and updates the
  action-tooltip string with the configured key ranges.

### 3. What changed since commit 06a354e

As with BetterHUD, `06a354e` is the migration commit itself: in
its diff against `d51c6e9` it only changes `using SMLHelper.V2.Options.Attributes` →
`using Nautilus.Options.Attributes` (`SlotsUtility.cs`), the pattern match
`is PropertyInfo property` → `is { } property` (`SlotsUtility.cs`, `uGUIPatches.cs`), and
`is uGUI_QuickSlots __instance/quickSlots` → `is { } __instance/quickSlots`
(`TooltipFactoryPatches.cs`, `uGUIPatches.cs`) — pure C# syntactic modernizations, no change to
the logic or to the referenced game types. On top of the migration, the diff on `develop`
touches only the wait condition in `uGUIPatches.cs:33` (`WaitScreen.IsWaiting`, see the status
note above).

### 4. Game-API notes (verified against the decompiled trees)

- No `GameInputSystem` class exists in the decompiled BZ: `GameInput` (with its nested
  `Device`/`Button`/`BindingSet` enums and the public `IsBindable`) is the input API. The
  decompiled SN1 tree has `GameInputSystem` as the runtime `IGameInput` implementation
  (Unity InputSystem, `PlatformUtils.cs:206`); the mod's SN binding-write postfix targets it.
- `uGUI_OptionsPanel` exists (`Assembly-CSharp/uGUI_OptionsPanel.cs:13`) and inherits
  `uGUI_TabbedControlsPanel`; no class in the decompiled tree defines a
  `PopulateBindingSettings` method.
- `uGUI_TabbedControlsPanel.AddBindingOption(int, string, Device, Button, out GameObject)` is
  public with the exact 5-parameter signature the mod patches
  (`Assembly-CSharp/uGUI_TabbedControlsPanel.cs:410`).
- Much of the game API the mod touches as direct C# is `private` in the decompiled game
  (`SetBindingInternal`, `SelectInternal`, `DeselectInternal`, `binding`, `slotCount`'s setter,
  `RefreshActionStrings`, `stringKeyRange15`, `uGUI.Awake`, `HandleInput`, `target`, `icons`,
  `Init`); the publicizer (see the status note above) makes all of it compile.

---

## Common section

### Build comparison in the solution

| Project | GUID | `.Build.0` present | Compiles today (verified) |
|---|---|---|---|
| BetterSubnautica | `BF74B9C6-ACE7-4494-82A0-22B9F78AC731` | 4/4 | Yes (out of scope for this analysis) |
| BetterNitrox | `60F92BD5-C740-4E3C-996F-61A248BB95A5` | 2/4 (only `BZ.MULTI`, `SN.MULTI`) | partial, out of scope |
| BetterLights | `E67CCE42-B46D-48EB-9731-9054038578CF` | 4/4 | Yes (out of scope) |
| BetterMap | `48AB7995-4793-4DD4-B296-BE4963724FA9` | 4/4 | Yes (out of scope) |
| BetterRemote | `8D2F5C43-9B1E-4A67-B2D0-3F4A5B6C7D8E` | 4/4 | Yes (out of scope) |
| **BetterHUD** | `C1494AF7-D308-4A38-B197-A0827AF618CE` | 4/4 | **Yes** — verified in-game on BZ.MULTI |
| **BetterQuickSlots** | `46BC23D6-EBB8-4270-A3BE-DAA115A97E69` | 4/4 | **Yes** — verified in-game on BZ.MULTI and SN.MULTI |

`BetterNitrox` shows that build exclusion is applied selectively per configuration
(`.ActiveCfg` lines without `.Build.0` for the excluded configs): it builds only the `MULTI`
variants.

### Diff since commit 06a354e

`git diff 06a354e -- BetterHUD BetterQuickSlots` touches exactly two files:
`BetterHUD/MonoBehaviours/TimeDisplayController.cs` and
`BetterQuickSlots/Patches/uGUIPatches.cs` — the `WaitScreen.IsWaiting` wait conditions described
in each project's status note. The rest of both projects is the code that came out of the
QModManager→BepInEx migration. Separate follow-up work exists on the unmerged `refactor` branch
(a single "WIP" commit above `06a354e`) — abandoned, non-compiling WIP whose
`GameInputSystemPatches` targets a type absent from the game (see §4 of BetterQuickSlots).
