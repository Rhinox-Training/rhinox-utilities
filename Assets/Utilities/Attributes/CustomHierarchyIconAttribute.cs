using System;

namespace Rhinox.Utilities.Attributes
{
    public enum IconSet
    {
        Internal,
        Asset,
        Odin
    }
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class CustomHierarchyIconAttribute : Attribute
    {
        public string Name { get; }
        public IconSet Set { get; }

        public CustomHierarchyIconAttribute(string iconName, IconSet set = IconSet.Asset)
        {
            Name = iconName;
            Set = set;
        }
    }
}