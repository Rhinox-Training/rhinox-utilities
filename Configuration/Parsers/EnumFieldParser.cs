using System;

namespace Rhinox.Utilities.Parsers
{
    public class EnumFieldParser : FieldParser
    {
        public override bool CanParse(ConfigField configField)
        {
            if (configField == null || configField.Field == null)
                return false;
            return configField.Field.FieldType.IsEnum;
        }

        public override bool ParseValue(ConfigField field, string fieldValue, out object value)
        {
            try
            {
                object enumVal = Enum.Parse(field.Field.FieldType, fieldValue);
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