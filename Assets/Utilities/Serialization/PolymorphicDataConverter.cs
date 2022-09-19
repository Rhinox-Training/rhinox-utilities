using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rhinox.Lightspeed;
using RMDY.Networking.Messages;
using RMDY.RoomConfig;
using Rhinox.Lightspeed.Reflection;

namespace RMDY.Networking
{
    [LoadCustomJsonConverter]
    public class PolymorphicDataConverter : JsonConverter
    {
        private const string TYPE_KEYWORD = "type";

        public override bool CanConvert(Type objectType)
        {
            if (objectType != null &&
                typeof(IPolymorphicJsonType).IsAssignableFrom(objectType))
                return true;
            return false;
        }
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            var attr = value.GetType().GetCustomAttribute<JsonPolymorphicTypeAttribute>();
            if (attr != null)
            {
                writer.WritePropertyName("type");
                writer.WriteValue(attr.TypeString);
            }
            
            var valueType = value.GetType();
            var members = SerializeHelper.GetPublicAndSerializedMembers(valueType);
            foreach (var member in members)
            {
                if (member is FieldInfo fieldInfo)
                {
                    var memberValue = fieldInfo.GetValue(value);
                    writer.WritePropertyName(fieldInfo.Name);
                    serializer.Serialize(writer, memberValue, fieldInfo.FieldType);
                }
                else if (member is PropertyInfo propertyInfo)
                {
                    var memberValue = propertyInfo.GetValue(value);
                    writer.WritePropertyName(propertyInfo.Name);
                    serializer.Serialize(writer, memberValue, propertyInfo.PropertyType);
                }
            }
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            var definedTypes = AppDomain.CurrentDomain.GetDefinedTypesOfType(objectType);
            Type resultType = null;
            foreach (var type in definedTypes)
            {
                var attr = type.GetCustomAttribute<JsonPolymorphicTypeAttribute>();
                if (attr == null)
                    continue;

                string typeStr = (string)obj[TYPE_KEYWORD];
                if (attr.TypeString.Equals(typeStr, StringComparison.InvariantCulture))
                {
                    resultType = type;
                    break;
                }
            }

            if (resultType == null)
                return null;

            var data = Activator.CreateInstance(resultType);
            // Populate the object properties
            using (JsonReader jObjectReader = CopyReaderForObject(reader, obj))
            {
                serializer.Populate(jObjectReader, data);
            }
            return data;
        }
        
        /// <summary>Creates a new reader for the specified jObject by copying the settings
        /// from an existing reader.</summary>
        /// <param name="reader">The reader whose settings should be copied.</param>
        /// <param name="jToken">The jToken to create a new reader for.</param>
        /// <returns>The new disposable reader.</returns>
        public static JsonReader CopyReaderForObject(JsonReader reader, JToken jToken)
        {
            JsonReader jTokenReader = jToken.CreateReader();
            jTokenReader.Culture = reader.Culture;
            jTokenReader.DateFormatString = reader.DateFormatString;
            jTokenReader.DateParseHandling = reader.DateParseHandling;
            jTokenReader.DateTimeZoneHandling = reader.DateTimeZoneHandling;
            jTokenReader.FloatParseHandling = reader.FloatParseHandling;
            jTokenReader.MaxDepth = reader.MaxDepth;
            jTokenReader.SupportMultipleContent = reader.SupportMultipleContent;
            return jTokenReader;
        }
    }
}