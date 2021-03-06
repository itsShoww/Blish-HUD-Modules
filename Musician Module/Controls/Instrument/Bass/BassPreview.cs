using System;
using Blish_HUD.Controls.Intern;
using static Nekres.Musician_Module.MusicianModule;
namespace Nekres.Musician_Module.Controls.Instrument
{
    public class BassPreview : IInstrumentPreview
    {
        private BassNote.Octaves _octave = BassNote.Octaves.Low;

        private BassSoundRepository _soundRepository = new BassSoundRepository();

        public void PlaySoundByKey(GuildWarsControls key)
        {
            switch (key)
            {
                case GuildWarsControls.WeaponSkill1:
                case GuildWarsControls.WeaponSkill2:
                case GuildWarsControls.WeaponSkill3:
                case GuildWarsControls.WeaponSkill4:
                case GuildWarsControls.WeaponSkill5:
                case GuildWarsControls.HealingSkill:
                case GuildWarsControls.UtilitySkill1:
                case GuildWarsControls.UtilitySkill2:
                    ModuleInstance.MusicPlayer.StopSound();
                    ModuleInstance.MusicPlayer.PlaySound(_soundRepository.Get(key, _octave));
                    break;
                case GuildWarsControls.UtilitySkill3:
                    DecreaseOctave();
                    break;
                case GuildWarsControls.EliteSkill:
                    IncreaseOctave();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void IncreaseOctave()
        {
            switch (_octave)
            {
                case BassNote.Octaves.None:
                    break;
                case BassNote.Octaves.Low:
                    _octave = BassNote.Octaves.High;
                    break;
                case BassNote.Octaves.High:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DecreaseOctave()
        {
            switch (_octave)
            {
                case BassNote.Octaves.None:
                    break;
                case BassNote.Octaves.Low:
                    break;
                case BassNote.Octaves.High:
                    _octave = BassNote.Octaves.Low;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public void Dispose() {
            _soundRepository?.Dispose();
        }
    }
}