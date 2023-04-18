using System;
using System.Reflection;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class ConfigField : IConfigField
    {
        public FieldInfo Field { get; private set; }
        public string Section { get; private set; }
        
        public string Name => Field?.Name;
        public Type Type => Field?.FieldType;

        public ConfigField(FieldInfo field, string section)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            
            Field = field;
            Section = section;
        }

        public object GetValue(object instance)
        {
            return Field.GetValue(instance);
        }
                
        public void SetValue(object instance, object value)
        {
            Field.SetValue(instance, value);
        }

        public T GetCustomAttribute<T>() where T : Attribute
        {
            if (Field == null)
                return null;
            return Field.GetCustomAttribute<T>();
        }
    }
}