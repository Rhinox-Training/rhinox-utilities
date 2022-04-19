using System;

namespace Rhinox.Utilities
{
    [Serializable]
    public class DynamicConfigFieldEntry
    {
        public string Name;
        public string Value;
    }

    public static class DynamicEntryExtensions
    {
        public static bool ContainsKey(this DynamicConfigFieldEntry[] entries, string key, bool ignoreCase = false)
        {
            if (entries == null)
                return false;
            var stringComp =
                ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            foreach (var entry in entries)
            {
                if (entry.Name.Equals(key, stringComp))
                    return true;
            }

            return false;
        }
        
        public static bool TryGetValue(this DynamicConfigFieldEntry[] entries, string key, out string value, bool ignoreCase = false)
        {
            if (entries == null)
            {
                value = null;
                return false;
            }

            var stringComp =
                ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            foreach (var entry in entries)
            {
                if (entry.Name.Equals(key, stringComp))
                {
                    value = entry.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }
        
        public static string GetValue(this DynamicConfigFieldEntry[] entries, string key, bool ignoreCase = false)
        {
            if (entries == null)
                return null;
            var stringComp =
                ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            foreach (var entry in entries)
            {
                if (entry.Name.Equals(key, stringComp))
                    return entry.Value;
            }

            return null;
        }
    }
}