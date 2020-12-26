using Blish_HUD;
using CSCore;
using CSCore.Codecs;
using CSCore.DSP;
using CSCore.SoundOut;
using CSCore.Streams;
using CSCore.Streams.Effects;
using Microsoft.Xna.Framework;
using Nekres.Music_Mixer.Player.API;
using Nekres.Music_Mixer.Player.Source;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Nekres.Music_Mixer.Player
{
    public class MusicPlayer : IDisposable
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(MusicPlayer));

        private float _masterVolume => MusicMixerModule.ModuleInstance.MasterVolume;
        private bool _toggleKeepAudioFiles => MusicMixerModule.ModuleInstance.ToggleKeepAudioFiles.Value;

        public event EventHandler<ValueEventArgs<string>> AudioEnded;

        //private Regex _vimeoVideoID = new Regex(@"(http|https)?:\/\/(www\.|player\.)?vimeo\.com\/(?:channels\/(?:\w+\/)?|groups\/([^\/]*)\/videos\/|video\/|)(\d+)(?:|\/\?)", RegexOptions.Compiled);

        private WasapiOut _outputDevice;
        private bool _initialized;

        private FadeInOut _fadeInOut;
        private const double _fadeSeconds = 3;
        private Equalizer _equalizer;
        private BiQuadFilterSource _biQuadFilter;
        private LowpassFilter _lowPassFilter;

        private string _currentTitle;

        private Timer _endTimer;

        private bool _submergedFxEnabled;

        private string _cachePath;

        public MusicPlayer(string cachePath) {
            _cachePath = Directory.CreateDirectory(cachePath).FullName;

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
            _endTimer?.Stop();
            _endTimer?.Dispose();
        }

        public void Stop() {
            if (!_initialized) return;
            _initialized = false;
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
            _equalizer.SampleFilters[1].AverageGainDB = enable ? 19.5 : 0; // Bass
            _equalizer.SampleFilters[9].AverageGainDB = enable ? 13.4 : 0; // Treble
            _biQuadFilter.Filter = enable ? _lowPassFilter : null;
        }

        private void SetNextTimer(TimeSpan duration) {
            _endTimer.Stop();
            if (duration <= TimeSpan.Zero) return;
            _endTimer.Interval = duration.TotalMilliseconds;
            _endTimer.Start();
        }

        public async void PlayTrack(string uri, float volume = 0) {
            if (uri == null || uri.Equals("")) return;

            IWaveSource source;

            if (!FileUtil.IsLocalPath(uri)) {

                var id = youtube_dl.Instance.GetYouTubeIdFromLink(uri);
                if (id.Equals(string.Empty)) return;
                var dir = Path.Combine(_cachePath, id);

                FileInfo file = null; 

                if (Directory.Exists(dir))
                    file = new DirectoryInfo(dir).GetFiles().FirstOrDefault();
                
                if (file != null && !FileUtil.IsFileLocked(file.FullName)) {

                    source = CodecFactory.Instance.GetCodec(file.FullName);
                    SetNextTimer(source.GetLength());

                } else {

                    if (file == null && _toggleKeepAudioFiles)
                        await youtube_dl.Instance.Download(uri, dir, AudioFormat.FLAC, null);

                    source = await youtube_dl.Instance.GetSoundSource(uri);
                    SetNextTimer(source.GetLength());
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

            _lowPassFilter = new LowpassFilter(source.WaveFormat.SampleRate, 400);
            var fadeStrat = new LinearFadeStrategy(){ SampleRate = source.WaveFormat.SampleRate, Channels = source.WaveFormat.Channels };
            _fadeInOut = new FadeInOut(source.ToSampleSource()){FadeStrategy = fadeStrat};
            _biQuadFilter = new BiQuadFilterSource(_fadeInOut);
            _equalizer = Equalizer.Create10BandEqualizer(_biQuadFilter);

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
                if ((s as LinearFadeStrategy)?.CurrentVolume > 0.01f)
                    return;
                finalSource?.Dispose();
                outputDevice?.Dispose();
            };
        }
    }
}
