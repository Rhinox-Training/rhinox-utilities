using System.Collections.Generic;

namespace Rhinox.Utilities.Parsers
{
    public abstract class FieldParser
    {
        public abstract bool CanParse(ConfigField configField);

        public abstract bool ParseValue(ConfigField field, string fieldValue, out object value);
    }

    public static class FieldParserHelper
    {
        public static ICollection<FieldParser> GetParsers()
        {
            return new FieldParser[]
            {
                new FloatFieldParser(),
                new IntFieldParser(),
                new BoolFieldParser(),
                new StringFieldParser(),
                new EnumFieldParser()
            };
        }
    }
}