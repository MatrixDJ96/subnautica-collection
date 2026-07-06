#if BELOWZERO_MULTI
using System;
using System.Security.Cryptography;
using System.Text;
using BetterSubnautica.Attributes;
using HarmonyLib;
using Subnautica.API.Features;
using UnityEngine;

namespace BetterNitrox.Patches
{
    // Per-machine identity, derived when the platform reports no id (Steam offline on a joining
    // client): the server rejects null ids and duplicate names, so such a machine joins as
    // "<name>@<machine>" with a stable id seeded by machine + Windows user. With a platform id
    // available (the usual local/host case) the platform identity passes through untouched.
    static class MachineIdentity
    {
        private static bool logged;

        private static string id;

        public static bool Probing { get; private set; }

        public static bool Active
        {
            get
            {
                bool missing;

                Probing = true;

                try
                {
                    missing = string.IsNullOrEmpty(Tools.GetLoggedId());
                }
                finally
                {
                    Probing = false;
                }

                if (missing && !logged)
                {
                    logged = true;

                    Core.Logger.LogInfo($"Machine identity active: suffix='@{Environment.MachineName}', id='{Id}'");
                }

                return missing;
            }
        }

        public static string Id => id ??= DeriveId();

        public static string Name(string baseName)
        {
            return $"{(string.IsNullOrEmpty(baseName) ? Environment.UserName : baseName)}@{Environment.MachineName}";
        }

        private static string DeriveId()
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"{SystemInfo.deviceUniqueIdentifier}/{Environment.UserName}"));

                // 76561197960265728 = SteamID64 base for individual accounts.
                return (76561197960265728UL + BitConverter.ToUInt32(hash, 0)).ToString();
            }
        }
    }

    [PrePatch]
    [HarmonyPatch(typeof(Tools))]
    [HarmonyPatch(nameof(Tools.GetLoggedInName))]
    class ToolsGetLoggedInNamePatch
    {
        static void Postfix(ref string __result)
        {
            if (!MachineIdentity.Probing && MachineIdentity.Active)
            {
                __result = MachineIdentity.Name(__result);
            }
        }
    }

    [PrePatch]
    [HarmonyPatch(typeof(Tools))]
    [HarmonyPatch(nameof(Tools.GetLoggedId))]
    class ToolsGetLoggedIdPatch
    {
        static void Postfix(ref string __result)
        {
            if (!MachineIdentity.Probing && MachineIdentity.Active)
            {
                __result = MachineIdentity.Id;
            }
        }
    }
}
#endif
