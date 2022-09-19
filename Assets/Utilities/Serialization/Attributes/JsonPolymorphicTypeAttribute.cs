using System;

namespace RMDY.RoomConfig
{
    public class JsonPolymorphicTypeAttribute : Attribute
    {
        public string TypeString { get; }

        public JsonPolymorphicTypeAttribute(string typeString)
        {
            TypeString = typeString;
        }
    }
}