using Blish_HUD;
using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;
using CSCore.Streams;
using CSCore.Streams.Effects;
using Gw2Sharp.Models;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using static Blish_HUD.GameService;
using Timer = System.Timers.Timer;

namespace Nekres.Music_Mixer
{
    public class MusicPlayer : IDisposable
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(MusicPlayer));

        private float _masterVolume => MusicMixerModule.ModuleInstance.MasterVolume.Value / 1000;
        private bool _toggleFourDayCycle => MusicMixerModule.ModuleInstance.ToggleFourDayCycle.Value;
        private bool _toggleKeepAudioFiles => MusicMixerModule.ModuleInstance.ToggleKeepAudioFiles.Value;

        private Regex _youtubeVideoID = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)", RegexOptions.Compiled);
        //private Regex _vimeoVideoID = new Regex(@"(http|https)?:\/\/(www\.|player\.)?vimeo\.com\/(?:channels\/(?:\w+\/)?|groups\/([^\/]*)\/videos\/|video\/|)(\d+)(?:|\/\?)", RegexOptions.Compiled);
        
        private const int _sampleRate = 44100;
        private const int _bits = 16;
        private const int _channels = 1;
        private const int _bitRate = 196; // kbps
        private const string _rawAudioFormat = "s16le";
        private const string _rawAudioCodec = "pcm_s16le";

        private const string _audioCodec = "mp3";
        private const int _bufferSize = 65536; // 64KB chunks

        private WasapiOut _outputDevice;
        private bool _initialized;
        private Equalizer _equalizer;

        private Process _streamingProcess;
        private Thread _bufferedPlayback;

        private Process _downloadProcess;
        private Queue<string> _downloadQueue;
        private string _currentDownload;

        private Process _metaProcess;

        private Stopwatch _stopwatch;
        public bool IsFading => _stopwatch?.IsRunning ?? false;

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
        private Timer _nextTimer;

        private bool _submergedFxEnabled;
        private bool _isEncounter;

        private string _FFmpegPath;
        private string _youtubeDLPath;
        private string _cachePath;

        public MusicPlayer(string playlistDirectory, string FFmpegPath, string youtubeDLPath) {
            _cachePath = Directory.CreateDirectory(Path.Combine(playlistDirectory, "cache")).FullName;

            _FFmpegPath = FFmpegPath;
            _youtubeDLPath = youtubeDLPath;
            
            _outputDevice = new WasapiOut();
            _stopwatch = new Stopwatch();
            _downloadQueue = new Queue<string>();
            _nextTimer = new Timer(){ AutoReset = false };
            _nextTimer.Elapsed += (o, e) => { if (!_isEncounter) PlayNext(); };

            _combatPlaylist = LoadPlaylist<List<Track>>(Path.Combine(playlistDirectory, "combat.json"));
            _encounterPlaylist = LoadPlaylist<List<EncounterTrack>>(Path.Combine(playlistDirectory, "encounter.json"));
            _instancePlaylist = LoadPlaylist<List<Track>>(Path.Combine(playlistDirectory, "instance.json"));
            _mountedPlaylist = LoadPlaylist<MountPlaylists>(Path.Combine(playlistDirectory, "mounted.json"));
            _pvpPlaylist = LoadPlaylist<List<Track>>(Path.Combine(playlistDirectory, "pvp.json"));
            _openWorldPlaylist = LoadPlaylist<List<Track>>(Path.Combine(playlistDirectory, "world.json"));
            _wvwPlaylist = LoadPlaylist<List<Track>>(Path.Combine(playlistDirectory, "wvw.json"));
            _submergedPlaylist = LoadPlaylist<List<Track>>(Path.Combine(playlistDirectory, "submerged.json"));

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
                if (!_stopwatch.IsRunning)
                    return;
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
            _initialized = false;
            _bufferedPlayback?.Abort();
            _stopwatch.Stop();
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


        public void ToggleSubmergedFx(bool enable) {
            _submergedFxEnabled = enable;
            if (!_initialized) return;
            _equalizer.SampleFilters[1].AverageGainDB = enable ? +19.5 : 0; // Bass
            _equalizer.SampleFilters[9].AverageGainDB = enable ? -13.4 : 0; // Treble
            SetVolume(enable ? 0.4f * _masterVolume : _masterVolume);
        }


        private WriteableBufferingSource StreamTrack(string youtubeId) {
            var url = "https://youtu.be/" + youtubeId;

            var fileName = @"C:\Windows\System32\cmd.exe";
            var args = $"/C youtube-dl \"{url}\" -o - -f \"(mp4/webm)[asr={_sampleRate}][abr<={_bitRate}]\" | ffmpeg -i pipe:0 -f {_rawAudioFormat} -c:a {_rawAudioCodec} -ar {_sampleRate} -ac {_channels} pipe:1";
            var wd = Path.GetDirectoryName(_youtubeDLPath);

            _streamingProcess = ProcessUtil.CreateProcess(fileName, wd, args, true);
            _streamingProcess.Exited += (o, e) => _streamingProcess?.Dispose();
            _streamingProcess.Start();

            return new WriteableBufferingSource(new WaveFormat(_sampleRate, _bits, _channels)){ FillWithZeros = true };
        }


        private void SetNextTimer(double ms) {
            _nextTimer.Stop();
            if (ms <= 0) return;
            _nextTimer.Interval = ms;
            _nextTimer.Start();
        }


        private void OnMetaProcessDurationReceived(object o, DataReceivedEventArgs e) {
            if (e.Data == null) return;
            Process p = (Process)o;
            try {
                var timeComponents = e.Data.Split(':').Reverse().ToArray();
		        var seconds = timeComponents.Count() > 0 ? int.Parse(timeComponents[0]) : 0;
		        var minutes = timeComponents.Count() > 1 ? int.Parse(timeComponents[1]) : 0;
		        var hours = timeComponents.Count() > 2 ? int.Parse(timeComponents[2]) : 0;
		        var days = timeComponents.Count() > 3 ? int.Parse(timeComponents[3]) : 0;
                var duration = new TimeSpan(days, hours, minutes, seconds);
                SetNextTimer(duration.TotalMilliseconds);
            } catch (FormatException ex) {
                Logger.Warn(ex.Message);
                _nextTimer.Stop();
            }
            p?.Dispose();
        }

        private void GetDuration(string youtubeId) {
            var url = "https://youtu.be/" + youtubeId;

            var fileName = @"C:\Windows\System32\cmd.exe";
            var args = $"/C youtube-dl \"{url}\" --get-duration";
            var wd = Path.GetDirectoryName(_youtubeDLPath);

            if (_metaProcess != null) {
                _metaProcess.OutputDataReceived -= OnMetaProcessDurationReceived;
                _metaProcess.SafeClose();
                _metaProcess.Dispose();
            }
            _metaProcess = ProcessUtil.CreateProcess(fileName, wd, args, true);
            _metaProcess.OutputDataReceived += OnMetaProcessDurationReceived;
            _metaProcess.Start();
            _metaProcess.BeginOutputReadLine();
        }


        private void DownloadTrack(string youtubeId, string outputFolder) {
            var url = "https://youtu.be/" + youtubeId;

            if (string.Equals(_currentDownload, youtubeId) || _downloadQueue.Contains(youtubeId)) return;
            _downloadQueue.Enqueue(youtubeId);

            if (_downloadProcess != null) return;

            _currentDownload = youtubeId;

            var dir = Directory.CreateDirectory(Path.Combine(outputFolder, youtubeId)).FullName;

            var fileName = @"C:\Windows\System32\cmd.exe";
            var wd = Path.GetDirectoryName(_youtubeDLPath);
            var args = $"/C youtube-dl -f \"bestaudio/best[ext=mp4]/best\" \"{url}\" -o \"{dir}/%(title)s.%(ext)s\" --restrict-filenames --extract-audio --audio-format {_audioCodec} --ffmpeg-location \"{Path.GetDirectoryName(_FFmpegPath)}\"";
            _downloadProcess = ProcessUtil.CreateProcess(fileName, wd, args, false);
            _downloadProcess.Exited += (o, e) => {
                _currentDownload = null;
                if (_downloadQueue.Count == 0) return;
                DownloadTrack(_downloadQueue.Dequeue(), outputFolder);
                _downloadProcess?.Dispose();
                _downloadProcess = null;
            };
            _downloadProcess.Start();
        }


        public void StartBufferedPlayback(WriteableBufferingSource bufferingSource) {
            _bufferedPlayback = new Thread(o => {
                byte[] buffer = new byte[_bufferSize];
                int read = 0;
                while (_initialized && _streamingProcess != null) {
                    if(bufferingSource.MaxBufferSize - bufferingSource.Length > buffer.Length)
                    {
                        try {
                            read = _streamingProcess.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length);
                        } catch (InvalidOperationException ex) {
                            Logger.Warn(ex.Message);
                            break;
                        }
                        if (read <= 0)
                            break;
                        bufferingSource?.Write(buffer, 0, read);
                    }
                    Thread.Sleep(50);
                }
            });
            _bufferedPlayback.Start();
        }


        public void PlayTrack(string uri, bool isEncounter = false, float volume = 0) {
            if (uri == null || uri.Equals("")) return;

            Stop();

            WriteableBufferingSource bufferingSource = null;
            IWaveSource source;

            if (!FileUtil.IsLocalPath(uri)) {

                var youtubeMatch = _youtubeVideoID.Match(uri);
                if (!youtubeMatch.Success) return;

                var id = youtubeMatch.Groups[1].Value;
                var dir = Path.Combine(_cachePath, id);

                FileInfo file = null; 

                if (Directory.Exists(dir))
                    file = new DirectoryInfo(dir).GetFiles().FirstOrDefault(x => x.Extension.Equals($".{_audioCodec}"));
                
                if (file != null && !FileUtil.IsFileLocked(file.FullName)) {

                    source = CodecFactory.Instance.GetCodec(file.FullName);
                    SetNextTimer(source.GetLength().TotalMilliseconds);

                } else {

                    if (file == null && _toggleKeepAudioFiles)
                        DownloadTrack(id, _cachePath);

                    bufferingSource = StreamTrack(id);
                    source = bufferingSource;
                    GetDuration(id);
                }

            } else {

                if (!File.Exists(uri)) return;

                source = CodecFactory.Instance.GetCodec(uri);
                SetNextTimer(source.GetLength().TotalMilliseconds);
            }

            _isEncounter = isEncounter;

            var equalizer = Equalizer.Create10BandEqualizer(source.ToSampleSource());
            var finalSource = equalizer
                    .ToStereo()
                    .ChangeSampleRate(source.WaveFormat.SampleRate)
                    .AppendSource(Equalizer.Create10BandEqualizer, out equalizer)
                    .ToWaveSource(source.WaveFormat.BitsPerSample);

            _equalizer = equalizer;

            _outputDevice.Initialize(finalSource);
            _initialized = true;

            // Restore previous sound effects.
            ToggleSubmergedFx(_submergedFxEnabled); 

            _outputDevice.Play();

            if (bufferingSource != null)
                StartBufferedPlayback(bufferingSource);

            SetVolume(_masterVolume);
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


        private string SelectTrack(IList<Track> playlist) {
            if (playlist == null) return "";

            _currentPlaylist = playlist;

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

        private string SelectEncounterTrack(Encounter encounter) {
            var track = _encounterPlaylist.FirstOrDefault(x => x.EncounterIds.Except(encounter.Ids).Count() == 0);
            if (track == null) return SelectTrack(_combatPlaylist);
            var count = track.Uris.Count;
            if (count == 0) return SelectTrack(_combatPlaylist);
            var trackNr = encounter.CurrentPhase;
            if (trackNr > count - 1) return "";
            return track.Uris[trackNr];
        }

        internal void PlayNext() => PlayTrack(SelectTrack(_currentPlaylist));
        internal void PlayMountTrack(MountType mount) => PlayTrack(SelectTrack(_mountedPlaylist.GetPlaylist(mount)));
        internal void PlayOpenWorldTrack() => PlayTrack(SelectTrack(_openWorldPlaylist));
        internal void PlayCombatTrack() => PlayTrack(SelectTrack(_combatPlaylist));
        internal void PlayCompetitiveTrack() => PlayTrack(SelectTrack(_pvpPlaylist));
        internal void PlayWorldVsWorldTrack() => PlayTrack(SelectTrack(_wvwPlaylist));
        internal void PlayInstanceTrack() => PlayTrack(SelectTrack(_instancePlaylist));
        internal void PlaySubmergedTrack() => PlayTrack(SelectTrack(_submergedPlaylist));
        internal void PlayEncounterTrack(Encounter encounter) => PlayTrack(SelectEncounterTrack(encounter), true);

        public void Dispose() {
            Gw2Mumble.UI.IsMapOpenChanged -= OnIsMapOpenChanged;
            _outputDevice.Stop();
            _outputDevice.Dispose();
            _outputDevice = null;
            _bufferedPlayback?.Abort();
            _streamingProcess?.SafeClose();
            _streamingProcess?.Dispose();
            _downloadProcess?.SafeClose();
            _downloadProcess?.Dispose();
            _metaProcess?.SafeClose();
            _metaProcess?.Dispose();
            _nextTimer?.Stop();
            _nextTimer?.Dispose();
        }
    }
}
