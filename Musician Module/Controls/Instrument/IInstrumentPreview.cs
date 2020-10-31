using Blish_HUD.Controls.Intern;
using System;

namespace Nekres.Musician_Module.Controls.Instrument
{
    public interface IInstrumentPreview : IDisposable
    {
        void PlaySoundByKey(GuildWarsControls key);
    }
}