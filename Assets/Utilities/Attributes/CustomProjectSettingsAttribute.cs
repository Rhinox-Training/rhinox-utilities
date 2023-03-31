using System;

namespace Rhinox.Utilities.Attributes
{
    public class CustomProjectSettingsAttribute : Attribute
    {
        public bool RuntimeSupported { get; set; }
        public string CustomCollection { get; set; }
    }
}