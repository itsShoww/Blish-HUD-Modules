using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekres.Music_Mixer
{
    public class MapTrack
    {
        [JsonProperty("mapId")]
        public int MapId { get; set; }
        [JsonProperty("uri")]
        public string Uri { get; set; }
        [JsonConverter(typeof(StringEnumConverter)), JsonProperty("dayTime")] 
        public TyrianTime DayTime { get; set; }
    }
    public class EncounterTrack
    {
        [JsonProperty("encounterIds"), JsonConverter(typeof(HexStringJsonConverter))]
        public IReadOnlyList<uint> EncounterIds { get; set; }
        [JsonProperty("uris")]
        public IReadOnlyList<string> Uris { get; set; }
    }
    public class Mounted
    {
        [JsonProperty("raptor")]
        public IList<MapTrack> Raptor { get; set; }
        [JsonProperty("springer")]
        public IList<MapTrack> Springer { get; set; }
        [JsonProperty("skimmer")]
        public IList<MapTrack> Skimmer { get; set; }
        [JsonProperty("jackal")]
        public IList<MapTrack> Jackal { get; set; }
        [JsonProperty("griffon")]
        public IList<MapTrack> Griffon { get; set; }
        [JsonProperty("rollerbeetle")]
        public IList<MapTrack> Rollerbeetle { get; set; }
        [JsonProperty("warclaw")]
        public IList<MapTrack> Warclaw { get; set; }
        [JsonProperty("skyscale")]
        public IList<MapTrack> Skyscale { get; set; }
    }
    public class CompetitiveMode
    {
        [JsonProperty("tracks")]
        public IList<MapTrack> Tracks { get; set; }
    }
    public class WorldVsWorld
    {
        [JsonProperty("tracks")]
        public IList<MapTrack> Tracks { get; set; }
    }
    public class StoryInstance
    {
        [JsonProperty("tracks")]
        public IList<MapTrack> Tracks { get; set; }
    }
    public class OpenWorld
    {
        [JsonProperty("tracks")]
        public IList<MapTrack> Tracks;
    }
    public class Combat
    {
        [JsonProperty("tracks")]
        public IList<MapTrack> Tracks { get; set; }
        [JsonProperty("encounterTracks")]
        public IList<EncounterTrack> EncounterTracks;
    }
}
