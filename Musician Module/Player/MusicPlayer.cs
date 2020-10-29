using System.Linq;
using System.Threading;
using Nekres.Musician_Module.Controls.Instrument;
using Nekres.Musician_Module.Domain;
using Nekres.Musician_Module.Player.Algorithms;
using Nekres.Musician_Module.Controls;
namespace Nekres.Musician_Module.Player
{
    public class MusicPlayer
    {
        public Thread Worker { get; private set; }
        public IPlayAlgorithm Algorithm { get; private set; }

        public void Dispose() => Algorithm.Dispose();
        
        public MusicPlayer(MusicSheet musicSheet, Instrument instrument, IPlayAlgorithm algorithm)
        {
            Algorithm = algorithm;
            Worker = new Thread(() => algorithm.Play(instrument, musicSheet.MetronomeMark, musicSheet.Melody.ToArray()));
        }
    }
}