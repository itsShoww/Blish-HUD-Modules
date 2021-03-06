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
        private bool _toggleFourDayCycle => MusicMixerModule.ModuleInstance.ToggleFourDayCycle;

        public enum Playlist
        {
            Battle,
            Encounter,
            Instance,
            Mounted,
            Pvp,
            OpenWorld,
            Wvw,
            Submerged,
            Victory,
            Defeated,
            MainMenu,
            BossBattle,
            Crafting
        }

        #region Playlists
        
        private IList<Context> _battlePlaylist;
        private IList<EncounterContext> _encounterPlaylist;
        private IList<Context> _instancePlaylist;
        private MountPlaylists _mountedPlaylist;
        private IList<Context> _pvpPlaylist;
        private IList<Context> _openWorldPlaylist;
        private IList<Context> _wvwPlaylist;
        private IList<Context> _submergedPlaylist;
        private IList<Context> _victoryPlaylist;
        private IList<Context> _defeatedPlaylist;
        private IList<Context> _mainMenuPlaylist;
        private IList<Context> _bossBattlePlaylist;
        private IList<Context> _craftingPlaylist;

        #endregion

        private IList<Context> _activeMainPlaylist;
        private IList<Context> _activeIntermediatePlaylist;

        public PlaylistManager(string playlistDirectory)
        {
            _activeMainPlaylist = new List<Context>();
            _activeIntermediatePlaylist = new List<Context>();

            _battlePlaylist = LoadPlaylist<List<Context>>(Path.Combine(playlistDirectory, "battle.json"));
            _encounterPlaylist = LoadPlaylist<List<EncounterContext>>(Path.Combine(playlistDirectory, "encounter.json"));
            _instancePlaylist = LoadPlaylist<List<Context>>(Path.Combine(playlistDirectory, "instance.json"));
            _mountedPlaylist = LoadPlaylist<MountPlaylists>(Path.Combine(playlistDirectory, "mounted.json"));
            _pvpPlaylist = LoadPlaylist<List<Context>>(Path.Combine(playlistDirectory, "pvp.json"));
            _openWorldPlaylist = LoadPlaylist<List<Context>>(Path.Combine(playlistDirectory, "world.json"));
            _wvwPlaylist = LoadPlaylist<List<Context>>(Path.Combine(playlistDirectory, "wvw.json"));
            _submergedPlaylist = LoadPlaylist<List<Context>>(Path.Combine(playlistDirectory, "submerged.json"));
            _victoryPlaylist = LoadPlaylist<List<Context>>(Path.Combine(playlistDirectory, "victory.json"));
            _defeatedPlaylist = LoadPlaylist<List<Context>>(Path.Combine(playlistDirectory, "defeated.json"));
            _mainMenuPlaylist = LoadPlaylist<List<Context>>(Path.Combine(playlistDirectory, "mainmenu.json"));
            _bossBattlePlaylist = LoadPlaylist<List<Context>>(Path.Combine(playlistDirectory, "crafting.json"));
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
                case Playlist.Battle:
                    _activeIntermediatePlaylist = _battlePlaylist;
                    break;
                case Playlist.Encounter:
                    break;
                case Playlist.Instance:
                    _activeMainPlaylist = _instancePlaylist;
                    break;
                case Playlist.Mounted:
                    _activeIntermediatePlaylist = _mountedPlaylist.GetPlaylist(Gw2Mumble.PlayerCharacter.CurrentMount);
                    break;
                case Playlist.Pvp:
                    _activeMainPlaylist = _pvpPlaylist;
                    break;
                case Playlist.OpenWorld:
                    _activeMainPlaylist = _openWorldPlaylist;
                    break;
                case Playlist.Wvw:
                    _activeMainPlaylist = _wvwPlaylist;
                    break;
                case Playlist.Submerged:
                    _activeIntermediatePlaylist = _submergedPlaylist;
                    break;
                case Playlist.Victory:
                    _activeIntermediatePlaylist = _victoryPlaylist;
                    break;
                case Playlist.Defeated:
                    _activeIntermediatePlaylist = _defeatedPlaylist;
                    break;
                case Playlist.MainMenu:
                    _activeMainPlaylist = _mainMenuPlaylist;
                    break;
                case Playlist.BossBattle:
                    _activeIntermediatePlaylist = _bossBattlePlaylist;
                    break;
                case Playlist.Crafting:
                    _activeIntermediatePlaylist = _craftingPlaylist;
                    break;
                default: break;
            }
        }

        private void CheckContexts(IList<Context> contexts)
        {
            var mapId = Gw2Mumble.CurrentMap.Id;
            var time = TyrianTimeUtil.GetCurrentDayCycle();
            foreach (var ctx in contexts)
            {
                var timeMatch = _toggleFourDayCycle ? ctx.DayTime.Equals(time) : ctx.DayTime.ContextEquals(time);
                var mapMatch = mapId == ctx.MapId;

                var noneTimeMatch = ctx.DayTime.Equals(TyrianTime.None);
                var noMapId = ctx.MapId == -1;

                ctx.Active = timeMatch && mapMatch ||
                             noneTimeMatch && mapMatch ||
                             timeMatch && noMapId ||
                             noneTimeMatch && noMapId;
            }
        }

        public void Refresh()
        {
            CheckContexts(_activeMainPlaylist);
            CheckContexts(_activeIntermediatePlaylist);
        }

        public string SelectTrack(MusicPlayer.SoundLayer soundLayer)
        {
            var playlist = soundLayer.Equals(MusicPlayer.SoundLayer.Main)
                ? _activeMainPlaylist
                : _activeIntermediatePlaylist;

            if (playlist == null) return "";
            var tracks = playlist.Where(x => x.Active).ToList();
            var count = tracks.Count;
            if (count == 0) return "";
            var track = tracks[RandomUtil.GetRandom(0, count - 1)];
            return track.Uri;
        }


        public string SelectEncounterTrack(Encounter encounter) {
            var track = _encounterPlaylist.FirstOrDefault(x => x.EncounterIds.Except(encounter.Ids).Count() == 0);
            if (track == null) {
                SetPlaylist(Playlist.Battle);
                return SelectTrack(MusicPlayer.SoundLayer.Intermediate);
            }
            var count = track.Uris.Count;
            if (count == 0) {
                SetPlaylist(Playlist.Battle);
                return SelectTrack(MusicPlayer.SoundLayer.Intermediate);
            }
            var trackNr = encounter.CurrentPhase;
            if (trackNr > count - 1) return "";
            return track.Uris[trackNr];
        }
    }
}
