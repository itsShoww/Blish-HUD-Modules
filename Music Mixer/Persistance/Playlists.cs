using Gw2Sharp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Linq;

namespace Nekres.Music_Mixer
{
    public class Track
    {
        [JsonProperty("mapId")]
        public int MapId { get; set; }
        [JsonConverter(typeof(StringEnumConverter)), JsonProperty("dayTime")] 
        public TyrianTime DayTime { get; set; }
        [JsonProperty("volume")]
        public float Volume { get; set; }
        [JsonProperty("uri")]
        public string Uri { get; set; }
    }

    public class EncounterTrack
    {
        [JsonProperty("encounterIds"), JsonConverter(typeof(HexStringJsonConverter))]
        public IReadOnlyList<uint> EncounterIds { get; set; }
        [JsonProperty("uris")]
        public IReadOnlyList<string> Uris { get; set; }
    }

    public class MountPlaylists
    {
        [JsonProperty("raptor")]
        public IList<Track> Raptor { get; set; }
        [JsonProperty("springer")]
        public IList<Track> Springer { get; set; }
        [JsonProperty("skimmer")]
        public IList<Track> Skimmer { get; set; }
        [JsonProperty("jackal")]
        public IList<Track> Jackal { get; set; }
        [JsonProperty("griffon")]
        public IList<Track> Griffon { get; set; }
        [JsonProperty("rollerbeetle")]
        public IList<Track> Rollerbeetle { get; set; }
        [JsonProperty("warclaw")]
        public IList<Track> Warclaw { get; set; }
        [JsonProperty("skyscale")]
        public IList<Track> Skyscale { get; set; }

        public IList<Track> GetPlaylist(MountType mount) {
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
