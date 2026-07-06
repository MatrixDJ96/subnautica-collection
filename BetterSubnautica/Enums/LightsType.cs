using System;

namespace BetterSubnautica.Enums
{
    [Flags]
    public enum LightsType
    {
        None = 0,
        Internal = 1,
        External = 2,
        Camera = 4
    }
}
