using System;
using System.Reflection;

namespace Rhinox.Utilities
{
    public abstract class TypedFieldParser : FieldParser
    {
        public Type TargetType { get; }

        protected TypedFieldParser(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            TargetType = type;
        }

        public override bool CanParse(IConfigField configField)
        {
            if (configField == null || configField.Type == null)
                return false;
            return TargetType.IsAssignableFrom(configField.Type);
        }
    }

    public class FloatFieldParser : TypedFieldParser
    {
        public FloatFieldParser() : base(typeof(float)) { }

        public override bool ParseValue(IConfigField field, string fieldValue, out object value)
        {
            if (float.TryParse(fieldValue, out float result))
            {
                value = result;
                return true;
            }

            value = null;
            return false;
        }
    }
    
    public class DoubleFieldParser : TypedFieldParser
    {
        public DoubleFieldParser() : base(typeof(double)) { }

        public override bool ParseValue(IConfigField field, string fieldValue, out object value)
        {
            if (double.TryParse(fieldValue, out double result))
            {
                value = result;
                return true;
            }

            value = null;
            return false;
        }
    }
    
    public class IntFieldParser : TypedFieldParser
    {
        public IntFieldParser() : base(typeof(int)) { }

        public override bool ParseValue(IConfigField field, string fieldValue, out object value)
        {
            if (int.TryParse(fieldValue, out int result))
            {
                value = result;
                return true;
            }

            value = null;
            return false;
        }
    }
    
    public class BoolFieldParser : TypedFieldParser
    {
        public BoolFieldParser() : base(typeof(bool)) { }

        public override bool ParseValue(IConfigField field, string fieldValue, out object value)
        {
            if (bool.TryParse(fieldValue, out bool result))
            {
                value = result;
                return true;
            }

            // Handle 0 and 1 as possible settings (bool.TryParse does not handle these)
            if (int.TryParse(fieldValue, out int intResult))
            {
                value = intResult != 0;
                return true;
            }

            value = null;
            return false;
        }
    }
    
    public class StringFieldParser : TypedFieldParser
    {
        public StringFieldParser() : base(typeof(string)) { }

        public override bool ParseValue(IConfigField field, string fieldValue, out object value)
        {
            value = fieldValue;
            return true;
        }
    }
}