using Blish_HUD;
using CSCore;
using CSCore.Codecs;
using CSCore.DSP;
using CSCore.SoundOut;
using CSCore.Streams;
using CSCore.Streams.Effects;
using Microsoft.Xna.Framework;
using Nekres.Music_Mixer.Controls;
using Nekres.Music_Mixer.Player.API;
using Nekres.Music_Mixer.Player.Source;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nekres.Music_Mixer.Player
{
    public class Soundtrack : IDisposable
    {
        private bool _toggleKeepAudioFiles => MusicMixerModule.ModuleInstance.ToggleKeepAudioFiles;

        private static readonly Logger Logger = Logger.GetLogger(typeof(Soundtrack));

        private float _masterVolume => MusicMixerModule.ModuleInstance.MasterVolume;

        public event EventHandler Disposed;

        private WasapiOut _outputDevice;
        private bool _initialized;
        private bool _paused;

        private FadeInOut _fadeInOut;
        private const double _fadeSeconds = 3;
        private Equalizer _equalizer;
        private BiQuadFilterSource _biQuadFilter;

        private NTimer _endTimer;

        private string _cachePath;

        private Thread _playbackThread;
        private bool _isDisposing;
        public Soundtrack(string uri, string cachePath, bool submergedFxEnabled = false)
        {
            _cachePath = cachePath;

            _endTimer = new NTimer(){ AutoReset = false };
            _endTimer.Elapsed += (o, e) => Dispose();


            _playbackThread = new Thread(async () => { 
                await GetSoundSource(uri).ContinueWith(response => {
                    if (response.Exception != null && response.Result == null) return;
                    Play(response.Result);
                    ToggleSubmergedFx(submergedFxEnabled);
                });
            });
            _playbackThread.Start();
        }

        private void DisposeOnFadeFinished(object o, EventArgs e)
        {
            if ((o as LinearFadeStrategy)?.CurrentVolume > 0.01f)
                return;
            _fadeInOut.FadeStrategy.FadingFinished -= DisposeOnFadeFinished;

            Dispose();
        }

        public void Pause()
        {
            _paused = true;
            if (!_initialized) return;
            Fade(1, 0, TimeSpan.FromSeconds(_fadeSeconds));
        }

        public void Resume()
        {
            _paused = false;
            if (!_initialized) return;
            Fade(0, 1, TimeSpan.FromSeconds(_fadeSeconds));
            _outputDevice.Resume();
        }

        public void Dispose()
        {
            _initialized = false;
            _isDisposing = true;
            _outputDevice?.Dispose();
            _outputDevice = null;
            Disposed?.Invoke(this, null);
        }

        public void Stop()
        {
            if (!_initialized)
            {
                Dispose(); 
                return;
            }
            _fadeInOut.FadeStrategy.FadingFinished += DisposeOnFadeFinished;
            Fade(1, 0, TimeSpan.FromSeconds(_fadeSeconds));
        }

        public void Fade(float? from, float to, TimeSpan duration)
        {
            _fadeInOut?.FadeStrategy.StopFading();
            _fadeInOut?.FadeStrategy.StartFading(from, MathHelper.Clamp(to, 0, 1), duration);
        }

        public void SetVolume(float volume) {
            if (!_initialized) return;
            _outputDevice.Volume = MathHelper.Clamp(volume, 0, _masterVolume);
        }

        public void ToggleSubmergedFx(bool enable) {
            if (!_initialized || _equalizer == null) return;
            _equalizer.SampleFilters[1].AverageGainDB = enable ? 19.5 : 0; // Bass
            _equalizer.SampleFilters[9].AverageGainDB = enable ? 13.4 : 0; // Treble
            _biQuadFilter.Enabled = enable;
        }

        private void SetNextTimer(TimeSpan duration) {
            _endTimer.Stop();
            if (duration <= TimeSpan.Zero) return;
            _endTimer.Interval = duration.TotalMilliseconds;
            _endTimer.Start();
        }

        private async Task<IWaveSource> GetSoundSource(string uri) {
            if (uri == null || uri.Equals("")) return null;

            IWaveSource source;

            if (!FileUtil.IsLocalPath(uri)) {

                var id = youtube_dl.Instance.GetYouTubeIdFromLink(uri);
                if (id.Equals(string.Empty)) return null;
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

                if (!File.Exists(uri)) return null;

                source = CodecFactory.Instance.GetCodec(uri);
                SetNextTimer(source.GetLength());
            }
            return _isDisposing ? null : source;
        }

        private void Play(IWaveSource source) {
            if (source == null) return;
            var outputDevice = new WasapiOut(){ Latency = 100 };

            #if DEBUG
            outputDevice.Stopped += (o, e) => {
                if (!e.HasError) return;
                Logger.Warn(e.Exception.Message);
            };
            #endif

            var fadeStrat = new LinearFadeStrategy(){ SampleRate = source.WaveFormat.SampleRate, Channels = source.WaveFormat.Channels };
            _fadeInOut = new FadeInOut(source.ToSampleSource()){FadeStrategy = fadeStrat};
            _biQuadFilter = new BiQuadFilterSource(_fadeInOut) { Filter = new LowpassFilter(source.WaveFormat.SampleRate, 400) };
            _equalizer = Equalizer.Create10BandEqualizer(_biQuadFilter);

            var finalSource = _equalizer
                    .ToStereo()
                    .ChangeSampleRate(source.WaveFormat.SampleRate)
                    .ToWaveSource(source.WaveFormat.BitsPerSample);

            outputDevice.Initialize(finalSource);
            _initialized = true;

            _outputDevice = outputDevice;

            // Restore previous sound effects.
            SetVolume(_masterVolume);

            Fade(0, 1, TimeSpan.FromSeconds(_fadeSeconds));
            _outputDevice.Play();

        }
    }
}
