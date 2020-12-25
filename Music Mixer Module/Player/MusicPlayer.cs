using Blish_HUD;
using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;
using CSCore.Streams;
using CSCore.Streams.Effects;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Timer = System.Timers.Timer;

namespace Nekres.Music_Mixer.Player
{
    public class MusicPlayer : IDisposable
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(MusicPlayer));

        private float _masterVolume => MusicMixerModule.ModuleInstance.MasterVolume;
        private bool _toggleKeepAudioFiles => MusicMixerModule.ModuleInstance.ToggleKeepAudioFiles.Value;

        public event EventHandler<ValueEventArgs<string>> AudioEnded;

        private Regex _youtubeVideoID = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)", RegexOptions.Compiled);
        //private Regex _vimeoVideoID = new Regex(@"(http|https)?:\/\/(www\.|player\.)?vimeo\.com\/(?:channels\/(?:\w+\/)?|groups\/([^\/]*)\/videos\/|video\/|)(\d+)(?:|\/\?)", RegexOptions.Compiled);
        


        private const int _sampleRate = 44100;
        private const int _bits = 16;
        private const int _channels = 2;
        private const string _audioCodec = "mp3";

        private const int _rawSampleRate = 44100;
        private const int _rawBits = 16; // streaming only.
        private const int _rawChannels = 1;
        private const string _rawAudioFormat = "s16le";
        private const string _rawAudioCodec = "pcm_s16le";

        private const int _bitRate = 196; // kbps

        private const int _bufferSize = 65536; // 64KB chunks

        private WasapiOut _outputDevice;
        private bool _initialized;

        private FadeInOut _fadeInOut;
        private const double _fadeSeconds = 1;
        private Equalizer _equalizer;

        private Process _streamingProcess;
        private Thread _bufferedPlayback;
        private WriteableBufferingSource _bufferingSource;

        private Process _downloadProcess;
        private Queue<string> _downloadQueue;

        private string _currentTitle;

        private Process _metaProcess;

        private Timer _endTimer;

        private bool _submergedFxEnabled;

        private string _FFmpegPath;
        private string _youtubeDLPath;
        private string _cachePath;

        public MusicPlayer(string cachePath, string FFmpegPath, string youtubeDLPath) {
            _cachePath = Directory.CreateDirectory(cachePath).FullName;

            _FFmpegPath = FFmpegPath;
            _youtubeDLPath = youtubeDLPath;

            _downloadQueue = new Queue<string>();
            _endTimer = new Timer(){ AutoReset = false };
            _endTimer.Elapsed += (o, e) => {
                _fadeInOut?.Dispose();
                AudioEnded?.Invoke(this, new ValueEventArgs<string>(_currentTitle));
            };
        }

        public void Dispose() {
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
            _endTimer?.Stop();
            _endTimer?.Dispose();
        }

        public void Stop() {
            if (!_initialized) return;
            _initialized = false;
            _bufferedPlayback?.Abort();
            _outputDevice?.Stop();
        }

        public void Fade(float? from, float to, TimeSpan duration) => _fadeInOut?.FadeStrategy.StartFading(from, MathHelper.Clamp(to, 0, 1), duration);

        public void SetVolume(float volume) {
            if (!_initialized) return;
            _outputDevice.Volume = MathHelper.Clamp(volume, 0, _masterVolume);
        }

        public void ToggleSubmergedFx(bool enable) {
            _submergedFxEnabled = enable;
            if (!_initialized || _equalizer == null) return;
            _equalizer.SampleFilters[1].AverageGainDB = enable ? +19.5 : 0; // Bass
            _equalizer.SampleFilters[9].AverageGainDB = enable ? -13.4 : 0; // Treble
            SetVolume(enable ? 0.4f * _masterVolume : _masterVolume);
        }

        private void SetNextTimer(TimeSpan duration) {
            _endTimer.Stop();
            if (duration <= TimeSpan.Zero) return;
            _endTimer.Interval = duration.TotalMilliseconds;
            _endTimer.Start();
        }


        public void PlayTrack(string uri, float volume = 0) {
            if (uri == null || uri.Equals("")) return;

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
                    SetNextTimer(source.GetLength());

                } else {

                    if (file == null && _toggleKeepAudioFiles)
                        DownloadTrack(id, _cachePath);

                    GetDuration(id);
                    StreamTrack(id);
                    _bufferingSource = new WriteableBufferingSource(new WaveFormat(_rawSampleRate, _rawBits, _rawChannels)){ FillWithZeros = true };
                    source = _bufferingSource;
                    StartBufferedPlayback();
                }

            } else {

                if (!File.Exists(uri)) return;

                source = CodecFactory.Instance.GetCodec(uri);
                SetNextTimer(source.GetLength());
            }

            _fadeInOut?.FadeStrategy.StartFading(null, 0, TimeSpan.FromSeconds(_fadeSeconds));

            var outputDevice = new WasapiOut(){ Latency = 100 };

            #if DEBUG
            outputDevice.Stopped += (o, e) => {
                if (!e.HasError) return;
                Logger.Warn(e.Exception.Message);
            };
            #endif

            var fadeStrat = new LinearFadeStrategy(){ SampleRate = source.WaveFormat.SampleRate, Channels = source.WaveFormat.Channels };
            _fadeInOut = new FadeInOut(source.ToSampleSource()){FadeStrategy = fadeStrat};
            _equalizer = Equalizer.Create10BandEqualizer(_fadeInOut);

            var finalSource = _equalizer
                    .ToStereo()
                    .ChangeSampleRate(source.WaveFormat.SampleRate)
                    .ToWaveSource(source.WaveFormat.BitsPerSample);

            outputDevice.Initialize(finalSource);
            _initialized = true;

            _outputDevice = outputDevice;
            _currentTitle = uri;

            // Restore previous sound effects.
            ToggleSubmergedFx(_submergedFxEnabled);
            SetVolume(_masterVolume);

            outputDevice.Play();

            // Setup dispose event when fade target volume has reached 0.
            fadeStrat.FadingFinished += (s, e) => {
                if ((s as LinearFadeStrategy)?.CurrentVolume > 0)
                    return;
                finalSource?.Dispose();
                outputDevice?.Dispose();
            };
        }


        /// <summary>
        /// Listens in to the <see cref="_streamingProcess">_streamingProcess</see> and
        /// writes data to a <paramref name="bufferingSource"/>.
        /// </summary>
        public void StartBufferedPlayback() {
            _bufferedPlayback = new Thread(o => {
                byte[] buffer = new byte[_bufferSize];
                int read = 0;
                var started = false;
                while (_initialized && _streamingProcess != null) {
                    if(_bufferingSource.MaxBufferSize - _bufferingSource.Length > buffer.Length)
                    {
                        try {
                            read = _streamingProcess.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length);
                        
                            if (read <= 0)
                                break;
                            if (!started) {
                                started = true;
                                _fadeInOut.FadeStrategy.StartFading(0, _masterVolume, _fadeSeconds);
                            }
                            _bufferingSource.Write(buffer, 0, read);
                        } catch (InvalidOperationException ex) {
                            #if DEBUG
                            Logger.Warn(ex.Message);
                            #endif
                            break;
                        }
                    }
                    Thread.Sleep(50);
                }
            });
            _bufferedPlayback.Start();
        }

        #region Processes

        /// <summary>
        /// Called when <see cref="_metaProcess">_metaProcess</see> has written a line to std_out.
        /// </summary>
        private void OnMetaProcessDurationReceived(object o, DataReceivedEventArgs e) {
            if (e.Data == null) return;
            Process p = (Process)o;
            try {
                var timeComponents = e.Data.Split(':').Reverse().ToArray();
		        var seconds = timeComponents.Count() > 0 ? int.Parse(timeComponents[0]) : 0;
		        var minutes = timeComponents.Count() > 1 ? int.Parse(timeComponents[1]) : 0;
		        var hours = timeComponents.Count() > 2 ? int.Parse(timeComponents[2]) : 0;
		        var days = timeComponents.Count() > 3 ? int.Parse(timeComponents[3]) : 0;
                SetNextTimer(new TimeSpan(days, hours, minutes, seconds));
            } catch (FormatException ex) {
                Logger.Warn(ex.Message);
                _endTimer.Stop();
            }
            p?.Dispose();
        }

        /// <summary>
        /// Starts a new <see cref="_metaProcess">_metaProcess</see> to get the duration of the mediafile.
        /// </summary>
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


        /// <summary>
        /// Starts a new <see cref="_streamingProcess">_streamingProcess</see> to stream the 
        /// raw audio track of the mediafile.
        /// </summary>
        private void StreamTrack(string youtubeId) {
            var url = "https://youtu.be/" + youtubeId;

            var fileName = @"C:\Windows\System32\cmd.exe";
            var args = $"/C youtube-dl \"{url}\" -o - -f \"(mp4/bestaudio)[asr={_rawSampleRate}][abr<={_bitRate}]\" | ffmpeg -i pipe:0 -f {_rawAudioFormat} -c:a {_rawAudioCodec} -ar {_rawSampleRate} -ac {_rawChannels} pipe:1";
            var wd = Path.GetDirectoryName(_youtubeDLPath);

            _streamingProcess = ProcessUtil.CreateProcess(fileName, wd, args, true);
            _streamingProcess.Exited += (o, e) => _streamingProcess?.Dispose();
            _streamingProcess.Start();
        }


        /// <summary>
        /// Starts a new <see cref="_downloadProcess">_downloadProcess</see> to download the mediafile and 
        /// convert it to <see cref="_audioCodec">_audioCodec</see>.
        /// </summary>
        private void DownloadTrack(string youtubeId, string outputFolder) {
            var url = "https://youtu.be/" + youtubeId;

            if (_downloadQueue.Contains(youtubeId)) return;
            _downloadQueue.Enqueue(youtubeId);

            if (_downloadProcess != null) return;

            var dir = Directory.CreateDirectory(Path.Combine(outputFolder, youtubeId)).FullName;

            var fileName = @"C:\Windows\System32\cmd.exe";
            var wd = Path.GetDirectoryName(_youtubeDLPath);
            var args = $"/C youtube-dl -f \"bestaudio/best[ext=mp4]/best\" \"{url}\" -o \"{dir}/%(title)s.%(ext)s\" --restrict-filenames --extract-audio --audio-format {_audioCodec} --ffmpeg-location \"{Path.GetDirectoryName(_FFmpegPath)}\"";
            _downloadProcess = ProcessUtil.CreateProcess(fileName, wd, args, false);
            _downloadProcess.Exited += (o, e) => {
                if (_downloadQueue.Count == 0) return;
                DownloadTrack(_downloadQueue.Dequeue(), outputFolder);
                _downloadProcess?.Dispose();
                _downloadProcess = null;
            };
            _downloadProcess.Start();
        }

        #endregion

    }
}
