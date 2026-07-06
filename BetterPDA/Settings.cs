using Nautilus.Json;
using Nautilus.Options.Attributes;
using UnityEngine;

namespace BetterPDA
{
    [Menu("Better PDA")]
    public class Settings : ConfigFile
    {
        [Toggle("PDA Pause")]
        public bool EnablePDAPause { get; set; }

#if SUBNAUTICA
        [Keybind("Eat/Use Button")]
#endif
        public KeyCode EatUse { get; set; } = KeyCode.Mouse2;
    }
}
