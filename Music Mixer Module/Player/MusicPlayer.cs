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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Nekres.Music_Mixer.Player
{
    public class MusicPlayer : IDisposable
    {
        public event EventHandler AudioEnded;

        private string _cachePath;

        private Soundtrack _currentTrack;

        private bool _submergedFxEnabled;

        public MusicPlayer(string cachePath) {
            _cachePath = Directory.CreateDirectory(cachePath).FullName;
        }

        public void Dispose() => _currentTrack?.Dispose();
        public void Stop() => _currentTrack?.Stop();
        public void ToggleSubmergedFx(bool enable) {
            _submergedFxEnabled = enable;
            _currentTrack?.ToggleSubmergedFx(enable);
        }
        public void Fade(float? from, float to, TimeSpan duration) => _currentTrack?.Fade(from, to, duration);
        public void SetVolume(float volume) => _currentTrack?.SetVolume(volume);

        public void PlayTrack(string uri) {
            if (_currentTrack != null) {
                _currentTrack.Disposed -= OnAudioEnded;
                _currentTrack.FadeOut();
            }
            _currentTrack = new Soundtrack(uri, _cachePath, _submergedFxEnabled);
            _currentTrack.Play();
            _currentTrack.Disposed += OnAudioEnded;
        }

        private void OnAudioEnded(object o, EventArgs e) {
            AudioEnded?.Invoke(this, null);
        }
    }
}
