using System;

namespace Rhinox.Utilities
{
    public interface IConfigField
    {
        string Section { get; }
        string Name { get; }
        
        Type Type { get; }

        object GetValue(object instance);

        void SetValue(object instance, object value);
    }
}