#if BELOWZERO_MULTI
using BetterSubnautica.Attributes;
using HarmonyLib;
using Subnautica.API.Enums;
using Subnautica.API.Features;

namespace BetterNitrox.Patches
{
    [PrePatch]
    [HarmonyPatch(typeof(Log))]
    [HarmonyPatch(nameof(Log.Send))]
    class LogSendPatch
    {
        static bool Prefix(string message, LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Info:
                    Core.Logger.LogInfo(message);
                    break;
                case LogLevel.Warn:
                    Core.Logger.LogWarning(message);
                    break;
                case LogLevel.Error:
                    Core.Logger.LogError(message);
                    break;
            }

            return false;
        }
    }

    [PrePatch]
    [HarmonyPatch(typeof(Log))]
    [HarmonyPatch(nameof(Log.SendRaw))]
    class LogSendRawPatch
    {
        static bool Prefix(string message)
        {
            Core.Logger.LogWarning($"[Log.SendRaw] {message}");

            return false;
        }
    }
}
#endif
