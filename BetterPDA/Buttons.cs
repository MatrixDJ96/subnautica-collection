#if SUBNAUTICA
using BetterSubnautica.Utility;

namespace BetterPDA
{
    public static class Buttons
    {
        public const string Category = "Better PDA";

        public static GameInput.Button EatUse { get; private set; }

        internal static void Register()
        {
            EatUse = ModInputUtility.RegisterButton("BetterPDAEatUse", "Eat/Use Button", "Uses or eats the hovered item.", Category, Core.Settings.EatUse);
        }
    }
}
#elif BELOWZERO
using BetterSubnautica.Utility;

namespace BetterPDA
{
    public static class Buttons
    {
        public const string Category = "Better PDA";

        internal static void Register()
        {
            var settings = Plugin.Core.Settings;

            ModInputUtility.RegisterKeybind(Category, "Eat/Use Button", "Uses or eats the hovered item.", settings, () => settings.EatUse, value => settings.EatUse = value);
        }
    }
}
#endif
