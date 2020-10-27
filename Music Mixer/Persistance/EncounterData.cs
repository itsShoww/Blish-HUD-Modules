using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Music_Mixer.Persistance
{
    public partial class EncounterData 
    { 
        /// <summary>
        /// Name of the encounter.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Relevant agent ids.
        /// </summary>
        [JsonProperty("ids"), JsonConverter(typeof(HexStringJsonConverter))]
        public IReadOnlyList<uint> Ids { get; set; }

        /// <summary>
        /// The initial health.
        /// </summary>
        [JsonProperty("health")]
        public long Health { get; set; }

        /// <summary>
        /// Time until encounter goes into rage mode.
        /// </summary>
        [JsonProperty("enrageTimer")]
        public long EnrageTimer { get; set; }
        /// <summary>
        /// Phases in percentage of remaining health.
        /// </summary>
        [JsonProperty("phases")]
        public IReadOnlyList<int> Phases { get; set; }

        /// <summary>
        /// Phases in milliseconds since encounter start.
        /// </summary>
        [JsonProperty("times")]
        public IReadOnlyList<long> Times { get; set; }
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
            var result = new List<uint>();

            JToken t = JToken.ReadFrom(reader);
            foreach (var val in t.Values<string>()) {
                var str = val;
                if (str == null) continue;
                if (str.Length < 2) throw new JsonSerializationException($"Unable to convert hex value. Hex string length too small, was \"{str}\".");

                if (str.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase) ||
                    str.StartsWith("&H", StringComparison.CurrentCultureIgnoreCase)) 
                {
                    str = str.Substring(2);
                }

                bool parsedSuccessfully = uint.TryParse(str, 
                        NumberStyles.HexNumber, 
                        CultureInfo.CurrentCulture, 
                        out var parsedUint);
            
                result.Add(parsedSuccessfully ? parsedUint : throw new JsonSerializationException($"Unknown error when converting value \"{str}\"."));
            }

            return result;
        }
    }
}
