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

        public IReadOnlyList<string> GetTracks(MountType mount, TyrianTime time = TyrianTime.None, int mapId = -1) {
            switch (mount) {
                case MountType.Jackal:
                    return Jackal.Where(x => x.MapId == mapId).Where(x => x.DayTime == time).Select(x => x.Uri).ToList();
                case MountType.Griffon:
                    return Griffon.Where(x => x.MapId == mapId).Where(x => x.DayTime == time).Select(x => x.Uri).ToList();
                case MountType.Springer:
                    return Springer.Where(x => x.MapId == mapId).Where(x => x.DayTime == time).Select(x => x.Uri).ToList();
                case MountType.Skimmer:
                    return Skimmer.Where(x => x.MapId == mapId).Where(x => x.DayTime == time).Select(x => x.Uri).ToList();
                case MountType.Raptor:
                    return Raptor.Where(x => x.MapId == mapId).Where(x => x.DayTime == time).Select(x => x.Uri).ToList();
                case MountType.RollerBeetle:
                    return Rollerbeetle.Where(x => x.MapId == mapId).Where(x => x.DayTime == time).Select(x => x.Uri).ToList();
                case MountType.Warclaw:
                    return Warclaw.Where(x => x.MapId == mapId).Where(x => x.DayTime == time).Select(x => x.Uri).ToList();
                case MountType.Skyscale:
                    return Skyscale.Where(x => x.MapId == mapId).Where(x => x.DayTime == time).Select(x => x.Uri).ToList();
                default: return null;
            }
        }
    }
}
