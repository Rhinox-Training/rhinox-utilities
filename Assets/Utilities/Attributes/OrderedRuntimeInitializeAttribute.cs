using System;

namespace Rhinox.Utilities.Attributes
{
    public class OrderedRuntimeInitializeAttribute : Attribute
    {
        public int Order { get; }
        
        public OrderedRuntimeInitializeAttribute(int order = 0)
        {
            Order = order;
        }
    }
}