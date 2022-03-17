using System;

namespace Rhinox.Utilities
{
    public class ConfigSectionAttribute : Attribute
    {
        public string Section { get; }
        
        public ConfigSectionAttribute(string section = null)
        {
            this.Section = section ?? "[ROOT]"; // TODO: empty section? fix in iniparser?
        }
    }
}