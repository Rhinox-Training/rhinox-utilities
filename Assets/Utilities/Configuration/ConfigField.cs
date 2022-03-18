using System;
using System.Reflection;

namespace Rhinox.Utilities
{
    public class ConfigField
    {
        public FieldInfo Field;
        public string Section;

        public ConfigField(FieldInfo field, string section)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            
            Field = field;
            Section = section;
        }
    }
}