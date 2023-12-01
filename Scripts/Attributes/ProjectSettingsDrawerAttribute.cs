using System;
using Rhinox.Lightspeed.Reflection;

namespace Rhinox.Utilities.Attributes
{
    public class ProjectSettingsDrawerAttribute : Attribute
    {
        public Type SettingsType { get; }
        
        public ProjectSettingsDrawerAttribute(Type settingsType)
        {
            if (!settingsType.InheritsFrom(typeof(CustomProjectSettings)))
                throw new ArgumentException(nameof(settingsType));
            SettingsType = settingsType;
        }
    }
}