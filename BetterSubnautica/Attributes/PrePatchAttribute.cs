using System;

namespace BetterSubnautica.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class PrePatchAttribute : Attribute
    {
        public PrePatchAttribute() { }
    }
}
