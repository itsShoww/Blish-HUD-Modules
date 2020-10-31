using System;
using Blish_HUD.Controls.Intern;
using static Nekres.Musician_Module.MusicianModule;
namespace Nekres.Musician_Module.Controls.Instrument
{
    public class FlutePreview : IInstrumentPreview
    {
        private FluteNote.Octaves _octave = FluteNote.Octaves.Low;

        private readonly FluteSoundRepository _soundRepository = new FluteSoundRepository();

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
                    if (_octave == FluteNote.Octaves.Low)
                    {
                        IncreaseOctave();
                    }
                    else
                    {
                        DecreaseOctave();
                    }
                    break;
                case GuildWarsControls.EliteSkill:
                    ModuleInstance.MusicPlayer.StopSound();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void IncreaseOctave()
        {
            switch (_octave)
            {
                case FluteNote.Octaves.None:
                    break;
                case FluteNote.Octaves.Low:
                    _octave = FluteNote.Octaves.High;
                    break;
                case FluteNote.Octaves.High:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DecreaseOctave()
        {
            switch (_octave)
            {
                case FluteNote.Octaves.None:
                    break;
                case FluteNote.Octaves.Low:
                    break;
                case FluteNote.Octaves.High:
                    _octave = FluteNote.Octaves.Low;
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