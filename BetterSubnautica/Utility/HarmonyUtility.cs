using System;
using System.Reflection;
using BepInEx.Logging;
using BetterSubnautica.Attributes;
using HarmonyLib;

namespace BetterSubnautica.Utility
{
    public static class HarmonyUtility
    {
        public static void PrePatchAll(Harmony harmony, Assembly assembly, ManualLogSource logger)
        {
            PatchWhere(harmony, assembly, logger, (prePatch, postPatch) => prePatch != null);
        }

        public static void PatchAll(Harmony harmony, Assembly assembly, ManualLogSource logger)
        {
            PatchWhere(harmony, assembly, logger, (prePatch, postPatch) => prePatch == null && postPatch == null);
        }

        public static void PostPatchAll(Harmony harmony, Assembly assembly, ManualLogSource logger)
        {
            PatchWhere(harmony, assembly, logger, (prePatch, postPatch) => postPatch != null && prePatch == null);
        }

        private static void PatchWhere(Harmony harmony, Assembly assembly, ManualLogSource logger, Func<PrePatchAttribute, PostPatchAttribute, bool> shouldPatch)
        {
            AccessTools.GetTypesFromAssembly(assembly).Do(type =>
            {
                var prePatchAttribute = type.GetCustomAttribute<PrePatchAttribute>();
                var postPatchAttribute = type.GetCustomAttribute<PostPatchAttribute>();

                if (shouldPatch(prePatchAttribute, postPatchAttribute))
                {
                    var methodInfos = harmony.CreateClassProcessor(type).Patch();

                    foreach (var methodInfo in methodInfos ?? [])
                    {
                        logger.LogInfo($" - Patched {methodInfo.Name} method");
                    }
                }
            });
        }
    }
}
