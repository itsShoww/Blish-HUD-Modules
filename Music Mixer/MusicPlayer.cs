using Blish_HUD;
using CSCore.Codecs;
using CSCore.SoundOut;
using CSCore.Streams.Effects;
using Gw2Sharp.Models;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using static Blish_HUD.GameService;

namespace Nekres.Music_Mixer
{
    public class MusicPlayer : IDisposable
    {
        private float _masterVolume => MusicMixerModule.ModuleInstance.MasterVolume.Value / 100;
        private bool _toggleFourDayCycle => MusicMixerModule.ModuleInstance.ToggleFourDayCycle.Value;

        private Regex _youtubeVideoID = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)", RegexOptions.Compiled);

        private WasapiOut _outputDevice;
        private bool _initialized;

        private YoutubeDL _youtubeDL;
        private OptionSet _youtubeDLOptions;
        
        private Stopwatch _stopwatch;

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

        public MusicPlayer(string _playlistDirectory, string _FFmpegPath, string _youtubeDLPath) {
            _outputDevice = new WasapiOut();

            _stopwatch = new Stopwatch();

            _youtubeDL = new YoutubeDL();
            _youtubeDLOptions = new OptionSet()
            {
                NoContinue = true,
                Format = "best",
                NoPart = true
            };
            _youtubeDL.FFmpegPath = Path.Combine(_playlistDirectory, _FFmpegPath);
            _youtubeDL.YoutubeDLPath = Path.Combine(_playlistDirectory, _youtubeDLPath);
            _youtubeDL.OutputFolder = Directory.CreateDirectory(Path.Combine(_playlistDirectory, "cache")).FullName;

            _combatPlaylist = LoadPlaylist<List<Track>>(Path.Combine(_playlistDirectory, "combat.json"));
            _encounterPlaylist = LoadPlaylist<List<EncounterTrack>>(Path.Combine(_playlistDirectory, "encounter.json"));
            _instancePlaylist = LoadPlaylist<List<Track>>(Path.Combine(_playlistDirectory, "instance.json"));
            _mountedPlaylist = LoadPlaylist<MountPlaylists>(Path.Combine(_playlistDirectory, "mounted.json"));
            _pvpPlaylist = LoadPlaylist<List<Track>>(Path.Combine(_playlistDirectory, "pvp.json"));
            _openWorldPlaylist = LoadPlaylist<List<Track>>(Path.Combine(_playlistDirectory, "world.json"));
            _wvwPlaylist = LoadPlaylist<List<Track>>(Path.Combine(_playlistDirectory, "wvw.json"));
            _submergedPlaylist = LoadPlaylist<List<Track>>(Path.Combine(_playlistDirectory, "submerged.json"));

            Gw2Mumble.UI.IsMapOpenChanged += OnIsMapOpenChanged;
        }

        private void OnIsMapOpenChanged(object o, ValueEventArgs<bool> e) {
            if (e.Value)
                Fade(0.4f * _masterVolume, 450);
            else
                Fade(_masterVolume, 450);
        }

        public void FadeOut() => Fade(0, 2000);
        public void Fade(float target, int durationMs) {
            if (!_initialized) return;
            float start = _outputDevice.Volume;
            if (target == start) return;
            float value;
            bool reached = false;
            _stopwatch.Restart();
            while (!reached && _stopwatch.ElapsedMilliseconds < durationMs) {
                if (target < start) {
                    value = Math.Abs(start * (_stopwatch.ElapsedMilliseconds / (float)durationMs) - start);
                    reached = value < target;
                } else {
                    value = start * (_stopwatch.ElapsedMilliseconds / (float)durationMs) + start;
                    reached = value > target;
                }
                value = reached ? target : value;
                SetVolume(value);
            }
            _stopwatch.Stop();
            if (target == 0)
                Stop();
        }

        public void Stop() {
            if (!_initialized) return; 
            _outputDevice?.Stop();
        }

        public void SetVolume(float volume) {
            if (!_initialized) return;
            // Avoid clamped volumes.
            volume = volume < 0 ? _masterVolume : volume; 
            volume = volume > 1 ? _masterVolume : volume;
            // Keep _masterVolume in the safe zone as well. Clamped volumes are fine here.
            _outputDevice.Volume = MathHelper.Clamp(volume, 0f, 1f);
        }


        public void PlayTrack(string uri, float volume = 0) {
            if (uri == null || uri.Equals("")) return;
            if (!FileUtil.IsLocalPath(uri)) {
                var youtubeMatch = _youtubeVideoID.Match(uri);
                if (!youtubeMatch.Success) return;

                var id = youtubeMatch.Groups[1].Value;
                var dir = Directory.CreateDirectory(Path.Combine(_youtubeDL.OutputFolder, id));

                var file = dir.GetFiles().FirstOrDefault(x => x.Extension.Equals(".mp3"));
                if (file == null) {
                    DownloadTrack(id, _youtubeDL.OutputFolder);
                    return;
                } else
                    uri = file.FullName;
            }

            if (!File.Exists(uri)) return;
            _outputDevice.Initialize(CodecFactory.Instance.GetCodec(uri));
            _initialized = true;
            // Individual songs can hold different peaks. Allow custom reduction per track.
            SetVolume(_masterVolume - Math.Abs(1 - volume));
            _outputDevice.Play();
        }


        private async void DownloadTrack(string youtubeId, string outputFolder, IProgress<DownloadProgress> progress = null) {
            var url = "https://youtu.be/" + youtubeId;
            var dir = Directory.CreateDirectory(Path.Combine(outputFolder, youtubeId)).FullName;

            await _youtubeDL.RunAudioDownload(url, AudioConversionFormat.Mp3, default, progress, null, _youtubeDLOptions).ContinueWith(response => {
                if (response.IsFaulted || !response.Result.Success) return;

                var filePath = response.Result.Data;
                var newPath = Path.Combine(dir, FileUtil.Sanitize(Path.GetFileName(filePath)));

                if (File.Exists(newPath)) File.Delete(newPath);
                File.Move(filePath, Path.Combine(dir, Path.GetFileName(filePath)));
            });
        }


        private void SavePlaylist(object o, string fileName) {
            var json = JsonConvert.SerializeObject(o, Formatting.Indented);
            System.IO.File.WriteAllText(fileName + ".json", json);
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


        private string SelectTrack(IList<Track> playlist) {
            if (playlist == null) return "";

            var mapId = Gw2Mumble.CurrentMap.Id;
            var time = TyrianTimeUtil.GetCurrentDayCycle();

            var mapTracks = playlist.Where(x => x.MapId == mapId);
            if (mapTracks.Count() == 0)
                mapTracks = playlist.Where(x => x.MapId == -1);

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

        public void PlayMountTrack(MountType mount) => PlayTrack(SelectTrack(_mountedPlaylist.GetPlaylist(mount)));
        public void PlayOpenWorldTrack() => PlayTrack(SelectTrack(_openWorldPlaylist));
        public void PlayCombatTrack() => PlayTrack(SelectTrack(_combatPlaylist));
        public void PlayCompetitiveTrack() => PlayTrack(SelectTrack(_pvpPlaylist));
        public void PlayWorldVsWorldTrack() => PlayTrack(SelectTrack(_wvwPlaylist));
        public void PlayInstanceTrack() => PlayTrack(SelectTrack(_instancePlaylist));
        public void PlaySubmergedTrack() => PlayTrack(SelectTrack(_submergedPlaylist));

        public void Dispose() {
            Gw2Mumble.UI.IsMapOpenChanged -= OnIsMapOpenChanged;
            _outputDevice.Stop();
            _outputDevice.Dispose();
            _outputDevice = null;
        }
    }
}
