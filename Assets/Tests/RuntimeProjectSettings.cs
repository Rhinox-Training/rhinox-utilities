using Rhinox.Utilities;
using Rhinox.Utilities.Attributes;

namespace Tests
{
    [RuntimeSupport]
    public class RuntimeProjectSettings : CustomProjectSettings<RuntimeProjectSettings>
    {
        public bool Enabled = false;
    }
}