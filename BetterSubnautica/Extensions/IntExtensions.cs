namespace BetterSubnautica.Extensions
{
    public static class IntExtensions
    {
        public static string ToHex(this int value)
        {
            return string.Format("{0:X2}", value);
        }
    }
}
