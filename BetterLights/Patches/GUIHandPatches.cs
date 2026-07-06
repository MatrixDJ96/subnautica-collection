#if SUBNAUTICA
using BetterLights.MonoBehaviours.ToggleLights;
using HarmonyLib;

namespace BetterLights.Patches
{
    // The flashlight's vanilla toggle is the RightHand tool-use pipeline (use animation,
    // then OnToolUseAnim): route that pipeline to the mod button so the rebindable key
    // plays the full vanilla experience, and detach the hardwired RightHand read.
    [HarmonyPatch(typeof(GUIHand))]
    [HarmonyPatch(nameof(GUIHand.GetInput))]
    class GUIHandGetInputPatch
    {
        static bool Prefix(GUIHand __instance, GameInput.Button button, GUIHand.InputState flag, ref bool __result)
        {
            if (button == GameInput.Button.RightHand && __instance.GetTool() is FlashLight flashlight && flashlight.GetComponent<FlashlightToggleLightsController>() != null)
            {
                __result = flag switch
                {
                    GUIHand.InputState.Down => GameInput.GetButtonDown(Buttons.FlashlightLightsToggle),
                    GUIHand.InputState.Held => GameInput.GetButtonHeld(Buttons.FlashlightLightsToggle),
                    GUIHand.InputState.Up => GameInput.GetButtonUp(Buttons.FlashlightLightsToggle),
                    _ => false,
                };

                return false;
            }

            return true;
        }
    }
}
#endif
