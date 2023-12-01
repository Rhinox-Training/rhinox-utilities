using System;

namespace Rhinox.Utilities
{
    public class EnumFieldParser : FieldParser
    {
        public override bool CanParse(IConfigField configField)
        {
            if (configField == null || configField.Type == null)
                return false;
            return configField.Type.IsEnum;
        }

        public override bool ParseValue(IConfigField field, string fieldValue, out object value)
        {
            try
            {
                object enumVal = Enum.Parse(field.Type, fieldValue);
                value = enumVal;
                return true;
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }
    }
}