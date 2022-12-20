using System.Collections.Generic;

namespace Rhinox.Utilities
{
    public abstract class FieldParser
    {
        public abstract bool CanParse(IConfigField configField);

        public abstract bool ParseValue(IConfigField field, string fieldValue, out object value);
    }

    public static class FieldParserHelper
    {
        public static ICollection<FieldParser> GetParsers()
        {
            return new FieldParser[]
            {
                new FloatFieldParser(),
                new DoubleFieldParser(),
                new IntFieldParser(),
                new BoolFieldParser(),
                new StringFieldParser(),
                new EnumFieldParser()
            };
        }
    }
}