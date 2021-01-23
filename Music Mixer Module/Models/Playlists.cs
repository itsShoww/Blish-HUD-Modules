using Gw2Sharp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Nekres.Music_Mixer
{
    public class Context
    {
        [JsonProperty("mapId")]
        public int MapId { get; set; }
        [JsonConverter(typeof(StringEnumConverter)), JsonProperty("dayTime")] 
        public TyrianTime DayTime { get; set; }
        [JsonProperty("volume")]
        public float Volume { get; set; }
        [JsonProperty("uri")]
        public string Uri { get; set; }
        [JsonIgnore]
        public bool Active { get; set; }
    }

    public class EncounterContext
    {
        [JsonProperty("encounterIds"), JsonConverter(typeof(HexStringJsonConverter))]
        public IReadOnlyList<uint> EncounterIds { get; set; }
        [JsonProperty("uris")]
        public IReadOnlyList<string> Uris { get; set; }
    }

    public class MountPlaylists
    {
        [JsonProperty("raptor")]
        public IList<Context> Raptor { get; set; }
        [JsonProperty("springer")]
        public IList<Context> Springer { get; set; }
        [JsonProperty("skimmer")]
        public IList<Context> Skimmer { get; set; }
        [JsonProperty("jackal")]
        public IList<Context> Jackal { get; set; }
        [JsonProperty("griffon")]
        public IList<Context> Griffon { get; set; }
        [JsonProperty("rollerbeetle")]
        public IList<Context> Rollerbeetle { get; set; }
        [JsonProperty("warclaw")]
        public IList<Context> Warclaw { get; set; }
        [JsonProperty("skyscale")]
        public IList<Context> Skyscale { get; set; }

        public IList<Context> GetPlaylist(MountType mount) {
            switch (mount) {
                case MountType.Jackal:
                    return Jackal;
                case MountType.Griffon:
                    return Griffon;
                case MountType.Springer:
                    return Springer;
                case MountType.Skimmer:
                    return Skimmer;
                case MountType.Raptor:
                    return Raptor;
                case MountType.RollerBeetle:
                    return Rollerbeetle;
                case MountType.Warclaw:
                    return Warclaw;
                case MountType.Skyscale:
                    return Skyscale;
                default: return null;
            }
        }
    }
}
