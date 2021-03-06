using System;
using Blish_HUD.Controls.Intern;
using static Nekres.Musician_Module.MusicianModule;
namespace Nekres.Musician_Module.Controls.Instrument
{
    public class HornPreview : IInstrumentPreview
    {
        private HornNote.Octaves _octave = HornNote.Octaves.Middle;

        private readonly HornSoundRepository _soundRepository = new HornSoundRepository();

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
                case HornNote.Octaves.None:
                    break;
                case HornNote.Octaves.Low:
                    _octave = HornNote.Octaves.Middle;
                    break;
                case HornNote.Octaves.Middle:
                    _octave = HornNote.Octaves.High;
                    break;
                case HornNote.Octaves.High:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DecreaseOctave()
        {
            switch (_octave)
            {
                case HornNote.Octaves.None:
                    break;
                case HornNote.Octaves.Low:
                    break;
                case HornNote.Octaves.Middle:
                    _octave = HornNote.Octaves.Low;
                    break;
                case HornNote.Octaves.High:
                    _octave = HornNote.Octaves.Middle;
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