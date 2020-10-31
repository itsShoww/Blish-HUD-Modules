using System.Linq;
using System.Threading;
using Nekres.Musician_Module.Controls.Instrument;
using Nekres.Musician_Module.Domain;
using Nekres.Musician_Module.Player.Algorithms;
using CSCore;
using CSCore.SoundOut;
using System;

namespace Nekres.Musician_Module.Player
{
    public class MusicPlayer : IDisposable
    {
        public Thread Worker { get; private set; }
        public IPlayAlgorithm Algorithm { get; private set; }
        public WasapiOut OutputDevice { get; private set; }

        public void PlaySound(ISampleSource sampleSource) {
            StopSound();
            OutputDevice.Initialize(sampleSource.ToWaveSource().Loop());
            OutputDevice.Play();
        }


        public void StopSound() {
            OutputDevice.Stop();
        }



        public MusicPlayer(MusicSheet musicSheet, Instrument instrument, IPlayAlgorithm algorithm)
        {
            Algorithm = algorithm;
            Worker = new Thread(() => algorithm.Play(instrument, musicSheet.MetronomeMark, musicSheet.Melody.ToArray()));

            OutputDevice = new WasapiOut();
        }


        public void Dispose() {
            Algorithm.Dispose();
            OutputDevice.Stop();
            OutputDevice.Dispose();
        }
    }
}