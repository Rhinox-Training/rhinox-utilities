using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rhinox.Lightspeed.Reflection;

namespace Rhinox.Utilities
{
    public static class ProjectSettingsHelper
    {
        public const string BUILD_FOLDER = "__BUILD__";
        private static List<Type> _implementedProjectSettingsTypes;
        private class Finder : CustomProjectSettings<Finder>
        {
            
        }
        
        public static bool TryGetSettingsPath(Type t, out string settingsPath)
        {
            if (!t.InheritsFrom(typeof(CustomProjectSettings<>)))
            {
                settingsPath = null;
                return false;
            }

            var property = t.GetProperty(nameof(Finder.SettingsPath), BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (property == null)
            {
                settingsPath = null;
                return false;
            }

            settingsPath = property.GetValue(null) as string;
            return true;
        }
        
        public static ICollection<Type> FindImplementedTypes()
        {
            if (_implementedProjectSettingsTypes == null)
            {
                _implementedProjectSettingsTypes =
                    AppDomain.CurrentDomain.GetDefinedTypesOfType<CustomProjectSettings>().Where(x => x != typeof(Finder)).ToList();
            }

            return _implementedProjectSettingsTypes;
        }

        public static IEnumerable<CustomProjectSettings> EnumerateProjectSettings()
        {
            foreach (var type in FindImplementedTypes())
            {
                if (type == typeof(Finder))
                    continue;
                
                PropertyInfo propertyInfo = FindProperty(type);
                if (propertyInfo == null)
                    continue;
                var customProjectSettings = propertyInfo.GetValue(null) as CustomProjectSettings;
                if (customProjectSettings == null)
                    continue;
                yield return customProjectSettings;
            }
        }

        private static PropertyInfo FindProperty(Type type)
        {
            return typeof(CustomProjectSettings<>).MakeGenericType(type).GetProperty(nameof(Finder.Instance),
                BindingFlags.Static | BindingFlags.Public);
        }
    }
}