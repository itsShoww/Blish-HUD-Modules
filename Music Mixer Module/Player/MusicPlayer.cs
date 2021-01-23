using System;
using System.IO;
using Blish_HUD;
using Blish_HUD.Controls;

namespace Nekres.Music_Mixer.Player
{
    public class MusicPlayer : IDisposable
    {
        public enum SoundLayer
        {
            Main,
            Intermediate
        }

        public event EventHandler<ValueEventArgs<SoundLayer>> AudioEnded;

        private string _cachePath;

        private Soundtrack _currentMainTrack;
        private Soundtrack _currentIntermediateTrack; // sound layer for intermediate states like mounted/submerged/combat

        private bool _submergedFxEnabled;

        private SoundLayer _activeSoundLayer;

        public MusicPlayer(string cachePath) {
            _cachePath = Directory.CreateDirectory(cachePath).FullName;
        }

        public void Dispose()
        {
            if (_currentMainTrack != null)
            {
                _currentMainTrack.Disposed -= OnMainAudioEnded;
                _currentMainTrack.Dispose();
            }

            if (_currentIntermediateTrack != null)
            {
                _currentIntermediateTrack.Disposed -= OnIntermediateAudioEnded;
                _currentIntermediateTrack.Dispose();
            }
        }

        public void PlayNext(string uri)
        {
            switch (_activeSoundLayer)
            {
                case SoundLayer.Main:
                    break;
                case SoundLayer.Intermediate:
                    break;
                default: break;
            }
        }

        public void StopMainTrack()
        {
            if (_currentMainTrack == null) return;
            _currentMainTrack.Disposed -= OnMainAudioEnded;
            _currentMainTrack?.Stop();
        }

        public void StopIntermediateTrack()
        {
            if (_currentIntermediateTrack == null) return;
            _currentIntermediateTrack.Disposed -= OnIntermediateAudioEnded;
            _currentIntermediateTrack.Stop();
        }

        /// <summary>
        /// Enables submerged fx for the main and the intermediate track.
        /// </summary>s
        public void ToggleSubmergedFx(bool enable) {
            _submergedFxEnabled = enable;
            _currentMainTrack?.ToggleSubmergedFx(enable);
            _currentIntermediateTrack?.ToggleSubmergedFx(enable);
        }

        /// <summary>
        /// Fades the main and the intermediate track.
        /// </summary>
        public void Fade(float? from, float to, TimeSpan duration)
        {
            _currentMainTrack?.Fade(from, to, duration);
            _currentIntermediateTrack?.Fade(from, to, duration);
        }

        /// <summary>
        /// Sets the volume of the main and the intermediate track.
        /// </summary>
        public void SetVolume(float volume)
        {
            _currentMainTrack?.SetVolume(volume);
            _currentIntermediateTrack?.SetVolume(volume);
        }

        private void PlayMainTrack(string uri) {
            StopMainTrack();
            _currentMainTrack = new Soundtrack(uri, _cachePath, _submergedFxEnabled);
            _currentMainTrack.Disposed += OnMainAudioEnded;
            _activeSoundLayer = SoundLayer.Main;
        }

        private void PlayIntermediateTrack(string uri) {
            StopIntermediateTrack();

            _currentMainTrack?.Pause();

            _currentIntermediateTrack = new Soundtrack(uri, _cachePath, _submergedFxEnabled);
            _currentIntermediateTrack.Disposed += OnIntermediateAudioEnded;
            _activeSoundLayer = SoundLayer.Intermediate;
        }

        public void PlayTrack(SoundLayer soundLayer, string uri)
        {
            if (soundLayer.Equals(SoundLayer.Main))
                PlayMainTrack(uri);
            else
                PlayIntermediateTrack(uri);
            
        }
        /// <summary>
        /// Resumes the main track and stops the intermediate track.
        /// </summary>
        public void Resume() {
            StopIntermediateTrack();

            _currentMainTrack?.Resume();
            _activeSoundLayer = SoundLayer.Main;
        }

        /// <summary>
        /// Pauses the main track.
        /// </summary>
        public void Pause()
        {
            _currentMainTrack?.Pause();
        }

        private void OnMainAudioEnded(object o, EventArgs e)
        {
            if (_activeSoundLayer.Equals(SoundLayer.Intermediate)) return;
            AudioEnded?.Invoke(this, new ValueEventArgs<SoundLayer>(SoundLayer.Main));
        }

        private void OnIntermediateAudioEnded(object o, EventArgs e)
        {
            if (_activeSoundLayer.Equals(SoundLayer.Main)) return;
            AudioEnded?.Invoke(this, new ValueEventArgs<SoundLayer>(SoundLayer.Intermediate));
        }
    }
}
