using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Music_Mixer.Persistance
{
    public class Boss { 
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("ids"), JsonConverter(typeof(HexStringJsonConverter))]
        public IReadOnlyList<uint> Ids;
        [JsonProperty("health")]
        public uint Health;
        [JsonProperty("enrageTimer")]
        public int EnrageTimer;
    }

    public sealed class HexStringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(uint).Equals(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue($"0x{value:x}");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var str = reader.ReadAsString();
            if (str == null || !str.StartsWith("0x"))
                throw new JsonSerializationException();
            return Convert.ToUInt32(str);
        }
    }
}
