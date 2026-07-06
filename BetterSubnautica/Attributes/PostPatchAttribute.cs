using System;

namespace BetterSubnautica.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class PostPatchAttribute : Attribute
    {
        public PostPatchAttribute() { }
    }
}
