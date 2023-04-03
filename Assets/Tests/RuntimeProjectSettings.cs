using Rhinox.Utilities;
using Rhinox.Utilities.Attributes;

namespace Tests
{
    [CustomProjectSettings(RuntimeSupported = true)]
    public class RuntimeProjectSettings : CustomProjectSettings<RuntimeProjectSettings>
    {
        public bool Enabled = false;
    }
}