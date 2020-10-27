using Nekres.Musician_Module.Controls.Instrument;
using Nekres.Musician_Module.Domain.Values;
using Nekres.Musician_Module.Controls;
namespace Nekres.Musician_Module.Player.Algorithms
{
    public interface IPlayAlgorithm
    {
        void Play(Instrument instrument, MetronomeMark metronomeMark, ChordOffset[] melody);
        void Dispose();
    }
}