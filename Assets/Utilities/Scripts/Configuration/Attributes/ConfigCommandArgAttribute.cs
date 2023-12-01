using System;

namespace Rhinox.Utilities
{
    public class ConfigCommandArgAttribute : Attribute
    {
        public string ArgumentKey { get; }
        
        public ConfigCommandArgAttribute(string argKey)
        {
            ArgumentKey = argKey;
        }
    }
}