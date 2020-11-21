using CSCore.Codecs.MP3;
using CSCore.SoundOut;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
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
        private Regex _youtubeVideoID = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)", RegexOptions.Compiled);

        private WasapiOut _outputDevice;
        private YoutubeDL _youtubeDL;
        private OptionSet _youtubeDLOptions;

        #region Playlists
        
        private Combat _combatPlaylist;
        private StoryInstance _instancePlaylist;
        private Mounted _mountedPlaylist;
        private CompetitiveMode _pvpPlaylist;
        private OpenWorld _openWorldPlaylist;
        private WorldVsWorld _wvwPlaylist;

        #endregion

        public MusicPlayer(string _playlistDirectory, string _FFmpegPath, string _youtubeDLPath) {
           _outputDevice = new WasapiOut();

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

            _combatPlaylist = LoadPlaylist<Combat>(Path.Combine(_playlistDirectory, "combat.json"));
            _instancePlaylist = LoadPlaylist<StoryInstance>(Path.Combine(_playlistDirectory, "instance.json"));
            _mountedPlaylist = LoadPlaylist<Mounted>(Path.Combine(_playlistDirectory, "mounted.json"));
            _pvpPlaylist = LoadPlaylist<CompetitiveMode>(Path.Combine(_playlistDirectory, "pvp.json"));
            _openWorldPlaylist = LoadPlaylist<OpenWorld>(Path.Combine(_playlistDirectory, "world.json"));
            _wvwPlaylist = LoadPlaylist<WorldVsWorld>(Path.Combine(_playlistDirectory, "wvw.json"));
        }


        public void SetVolume(float volume) {
            if (_outputDevice != null && _outputDevice.PlaybackState != PlaybackState.Stopped)
                _outputDevice.Volume = MathHelper.Clamp(volume, 0, 1);
        }


        public void PlayTrack(string uri) {
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
            _outputDevice.Initialize(new Mp3MediafoundationDecoder(uri));
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
            if (!File.Exists(url)) return (T)new object();
            using (var fs = File.OpenRead(url)) {
                fs.Position = 0;
                using (var jsonReader = new JsonTextReader(new StreamReader(fs)))
                {
                    var serializer = new JsonSerializer();
                    return serializer.Deserialize<T>(jsonReader);
                }
            }
        }


        public void Dispose() {
            _outputDevice.Stop();
            _outputDevice.Dispose();
            _outputDevice = null;
        }
    }
}
