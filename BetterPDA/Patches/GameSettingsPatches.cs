using HarmonyLib;

namespace BetterPDA.Patches
{
    [HarmonyPatch(typeof(GameSettings))]
    [HarmonyPatch(nameof(GameSettings.SerializeSettings))]
    class GameSettingsSerializeSettingsPatch
    {
        static void Prefix()
        {
            MiscSettings.pdaPause = Core.Settings.EnablePDAPause;
        }
    }
}
