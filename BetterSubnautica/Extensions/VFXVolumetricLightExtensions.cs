using UnityEngine;

namespace BetterSubnautica.Extensions
{
    public static class VFXVolumetricLightExtensions
    {
        public static bool IsInitialized(this VFXVolumetricLight __instance)
        {
            return __instance.volumRenderer != null && __instance.lightSource != null && __instance.coneMat != null && __instance.sphereMat != null && __instance.block != null;
        }

        public static void UpdateMaterial(this VFXVolumetricLight __instance, float offset, bool forceUpdate = false)
        {
            if (__instance.IsInitialized())
            {
                if (__instance.color != __instance.lightSource.color || !Mathf.Approximately(__instance.lightIntensity, __instance.lightSource.intensity + offset) || forceUpdate)
                {
                    __instance.color = __instance.lightSource.color;
                    __instance.lightIntensity = __instance.lightSource.intensity + offset;
                    Color value = __instance.color;
                    value.a *= __instance.lightIntensity / 8f;
                    __instance.block.SetColor(ShaderPropertyID._Color, value);
                    __instance.volumRenderer.SetPropertyBlock(__instance.block);
                }
            }
        }
    }
}
