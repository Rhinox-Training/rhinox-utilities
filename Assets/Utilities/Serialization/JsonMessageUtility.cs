using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rhinox.Lightspeed.Reflection;
using RMDY.RoomConfig;
using UnityEngine;

namespace RMDY.Networking
{
    public static class JsonMessageUtility
    {
        private static JsonConverter[] _converterCache;

        private static JsonConverter[] GetOrFindConverters()
        {
            if (_converterCache == null)
            {
                var list = new List<JsonConverter>();

                var types = AppDomain.CurrentDomain.GetDefinedTypesWithAttribute<LoadCustomJsonConverterAttribute>();
                foreach (var type in types)
                {
                    if (type == null)
                        continue;

                    var converter = Activator.CreateInstance(type) as JsonConverter;
                    if (converter == null)
                        continue;
                    
                    list.Add(converter);
                }

                _converterCache = list.ToArray();

            }
            return _converterCache;
        }

        public static ISocketMessage FromJsonString(string jsonStr, Type type)
        {
            // Newtonsoft
            var payload = JsonConvert.DeserializeObject(jsonStr.ToString(), type, GetOrFindConverters()) as ISocketMessage;
            // Unity
            //var payload = JsonUtility.FromJson(data.ToString(), type) as ISocketMessage;

            if (payload is IMessageTransformer messageTransformer)
            {
                var data = JContainer.Parse(jsonStr) as JContainer;
                messageTransformer.Transform(data);
            }

            return payload;
        }
        
        public static ISocketMessage FromJson(JContainer data, Type type)
        {
            // Newtonsoft
            var payload = JsonConvert.DeserializeObject(data.ToString(), type, GetOrFindConverters()) as ISocketMessage;
            // Unity
            //var payload = JsonUtility.FromJson(data.ToString(), type) as ISocketMessage;

            if (payload is IMessageTransformer messageTransformer)
                messageTransformer.Transform(data);
            return payload;
        }

        public static JObject ToJson(ISocketMessage message)
        {
            var payload = JsonConvert.SerializeObject(message, GetOrFindConverters());
            return JObject.Parse(payload);
        }
        
        public static string ToJsonString(ISocketMessage message)
        {
            var payload = JsonConvert.SerializeObject(message, GetOrFindConverters());
            return payload;
        }
        
        public static JObject ToJson(object message)
        {
            var payload = JsonConvert.SerializeObject(message, GetOrFindConverters());
            return JObject.Parse(payload);
        }
    }
}