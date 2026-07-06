#if BELOWZERO_MULTI
using BetterMap.MonoBehaviours;
using HarmonyLib;
using SubnauticaMap;

namespace BetterMap.Patches
{
    [HarmonyPatch(typeof(Storage))]
    [HarmonyPatch(nameof(Storage.slot), MethodType.Getter)]
    class StorageSlotPatch
    {
        static void Postfix(ref string __result)
        {
            if (SaveController.Instance != null)
            {
                if (SaveController.Instance.IsStarted)
                {
                    __result = SaveController.Instance.MapPath;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Storage))]
    [HarmonyPatch(nameof(Storage.userStorage), MethodType.Getter)]
    class StorageUserStoragePatch
    {
        static void Postfix(ref UserStorage __result)
        {
            if (SaveController.Instance != null)
            {
                if (SaveController.Instance.IsStarted)
                {
                    __result = SaveController.Instance.RealUserStorage;
                }
                else
                {
                    __result = SaveController.Instance.FakeUserStorage;
                }
            }
        }
    }
}
#endif
