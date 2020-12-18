using Blish_HUD;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Blish_HUD.GameService;
namespace Nekres.Music_Mixer.Player
{
    internal class PlaylistManager
    {
        private bool _toggleFourDayCycle => MusicMixerModule.ModuleInstance.ToggleFourDayCycle.Value;

        public enum Playlist
        {
            Combat,
            Encounter,
            Instance,
            Mounted,
            Pvp,
            OpenWorld,
            Wvw,
            Submerged
        }

        #region Playlists
        
        private IList<Track> _combatPlaylist;
        private IList<EncounterTrack> _encounterPlaylist;
        private IList<Track> _instancePlaylist;
        private MountPlaylists _mountedPlaylist;
        private IList<Track> _pvpPlaylist;
        private IList<Track> _openWorldPlaylist;
        private IList<Track> _wvwPlaylist;
        private IList<Track> _submergedPlaylist;

        #endregion

        private IList<Track> _currentPlaylist;

        public PlaylistManager(string playlistDirectory) {
            _combatPlaylist = LoadPlaylist<List<Track>>(Path.Combine(playlistDirectory, "combat.json"));
            _encounterPlaylist = LoadPlaylist<List<EncounterTrack>>(Path.Combine(playlistDirectory, "encounter.json"));
            _instancePlaylist = LoadPlaylist<List<Track>>(Path.Combine(playlistDirectory, "instance.json"));
            _mountedPlaylist = LoadPlaylist<MountPlaylists>(Path.Combine(playlistDirectory, "mounted.json"));
            _pvpPlaylist = LoadPlaylist<List<Track>>(Path.Combine(playlistDirectory, "pvp.json"));
            _openWorldPlaylist = LoadPlaylist<List<Track>>(Path.Combine(playlistDirectory, "world.json"));
            _wvwPlaylist = LoadPlaylist<List<Track>>(Path.Combine(playlistDirectory, "wvw.json"));
            _submergedPlaylist = LoadPlaylist<List<Track>>(Path.Combine(playlistDirectory, "submerged.json"));
        }


        private void SavePlaylist(object o, string fileName) {
            var json = JsonConvert.SerializeObject(o, Formatting.Indented);
            System.IO.File.WriteAllText(fileName, json);
        }


        private T LoadPlaylist<T>(string url) {
            if (!File.Exists(url)) return default;
            using (var fs = File.OpenRead(url)) {
                fs.Position = 0;
                using (var jsonReader = new JsonTextReader(new StreamReader(fs)))
                {
                    var serializer = new JsonSerializer();
                    return serializer.Deserialize<T>(jsonReader);
                }
            }
        }


        public void SetPlaylist(Playlist playlist) {
            switch (playlist) {
                case Playlist.Combat:
                    _currentPlaylist = _combatPlaylist;
                    break;
                case Playlist.Encounter:
                    break;
                case Playlist.Instance:
                    _currentPlaylist = _instancePlaylist;
                    break;
                case Playlist.Mounted:
                    _currentPlaylist = _mountedPlaylist.GetPlaylist(Gw2Mumble.PlayerCharacter.CurrentMount);
                    break;
                case Playlist.Pvp:
                    _currentPlaylist = _pvpPlaylist;
                    break;
                case Playlist.OpenWorld:
                    _currentPlaylist = _openWorldPlaylist;
                    break;
                case Playlist.Wvw:
                    _currentPlaylist = _wvwPlaylist;
                    break;
                case Playlist.Submerged:
                    _currentPlaylist = _submergedPlaylist;
                    break;
            }
        }

        public string SelectTrack() {
            if (_currentPlaylist == null) return "";

            var mapId = Gw2Mumble.CurrentMap.Id;
            var time = TyrianTimeUtil.GetCurrentDayCycle();

            var mapTracks = _currentPlaylist.Where(x => x.MapId == mapId);
            if (mapTracks.Count() == 0)
                mapTracks = _currentPlaylist.Where(x => x.MapId == -1);

            IEnumerable<Track> timeTracks;

            if (_toggleFourDayCycle)
                timeTracks = mapTracks.Where(x => x.DayTime == time);
            else
                timeTracks = mapTracks.Where(x => x.DayTime.ContextEquals(time));

            if (timeTracks.Count() == 0)
                timeTracks = mapTracks.Where(x => x.DayTime == TyrianTime.None);

            var filter = timeTracks.Select(x => x.Uri).ToList();
            var count = filter.Count;
            if (count == 0) return "";
            var track = filter[RandomUtil.GetRandom(0, count - 1)];
            return track;
        }


        private string SelectEncounterTrack(Encounter encounter) {
            var track = _encounterPlaylist.FirstOrDefault(x => x.EncounterIds.Except(encounter.Ids).Count() == 0);
            if (track == null) {
                SetPlaylist(Playlist.Combat);
                return SelectTrack();
            }
            var count = track.Uris.Count;
            if (count == 0) {
                SetPlaylist(Playlist.Combat);
                return SelectTrack();
            }
            var trackNr = encounter.CurrentPhase;
            if (trackNr > count - 1) return "";
            return track.Uris[trackNr];
        }
    }
}
