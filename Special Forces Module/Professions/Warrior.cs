using System.Collections.Generic;
using Blish_HUD.Controls.Intern;
using Microsoft.Xna.Framework;

namespace Nekres.Special_Forces_Module.Professions
{
    internal class Warrior : IProfession
    {
        private readonly Dictionary<GuildWarsControls, (int, int, int)> Base =
            new Dictionary<GuildWarsControls, (int, int, int)>
            {
                {GuildWarsControls.SwapWeapons, (-383, 38, 43)},
                {GuildWarsControls.WeaponSkill1, (-328, 24, 0)},
                {GuildWarsControls.WeaponSkill2, (-267, 24, 0)},
                {GuildWarsControls.WeaponSkill3, (-206, 24, 0)},
                {GuildWarsControls.WeaponSkill4, (-145, 24, 0)},
                {GuildWarsControls.WeaponSkill5, (-84, 24, 0)},
                {GuildWarsControls.HealingSkill, (87, 24, 0)},
                {GuildWarsControls.UtilitySkill1, (148, 24, 0)},
                {GuildWarsControls.UtilitySkill2, (209, 24, 0)},
                {GuildWarsControls.UtilitySkill3, (270, 24, 0)},
                {GuildWarsControls.EliteSkill, (332, 24, 0)},
                {GuildWarsControls.ProfessionSkill1, (-316, 104, 48)},
                {GuildWarsControls.ProfessionSkill2, (-267, 104, 48)},
                {GuildWarsControls.ProfessionSkill3, (-218, 104, 48)},
                {GuildWarsControls.ProfessionSkill4, (-168, 104, 48)},
                {GuildWarsControls.ProfessionSkill5, (-107, 107, 45)},
                {GuildWarsControls.SpecialAction, (-85, 157, 54)}
            };
        private readonly Dictionary<GuildWarsControls, (int, int, int)> Berserker =
            new Dictionary<GuildWarsControls, (int, int, int)>
            {
                {GuildWarsControls.SwapWeapons, (-383, 38, 43)},
                {GuildWarsControls.WeaponSkill1, (-328, 24, 0)},
                {GuildWarsControls.WeaponSkill2, (-267, 24, 0)},
                {GuildWarsControls.WeaponSkill3, (-206, 24, 0)},
                {GuildWarsControls.WeaponSkill4, (-145, 24, 0)},
                {GuildWarsControls.WeaponSkill5, (-84, 24, 0)},
                {GuildWarsControls.HealingSkill, (87, 24, 0)},
                {GuildWarsControls.UtilitySkill1, (148, 24, 0)},
                {GuildWarsControls.UtilitySkill2, (209, 24, 0)},
                {GuildWarsControls.UtilitySkill3, (270, 24, 0)},
                {GuildWarsControls.EliteSkill, (332, 24, 0)},
                {GuildWarsControls.ProfessionSkill1, (-316, 104, 48)},
                {GuildWarsControls.ProfessionSkill2, (-267, 104, 48)},
                {GuildWarsControls.ProfessionSkill3, (-218, 104, 48)},
                {GuildWarsControls.ProfessionSkill4, (-168, 104, 48)},
                {GuildWarsControls.ProfessionSkill5, (-107, 107, 45)},
                {GuildWarsControls.SpecialAction, (-85, 157, 54)}
            };
        private readonly Dictionary<GuildWarsControls, (int, int, int)> Spellbreaker =
            new Dictionary<GuildWarsControls, (int, int, int)>
            {
                {GuildWarsControls.SwapWeapons, (-383, 38, 43)},
                {GuildWarsControls.WeaponSkill1, (-328, 24, 0)},
                {GuildWarsControls.WeaponSkill2, (-267, 24, 0)},
                {GuildWarsControls.WeaponSkill3, (-206, 24, 0)},
                {GuildWarsControls.WeaponSkill4, (-145, 24, 0)},
                {GuildWarsControls.WeaponSkill5, (-84, 24, 0)},
                {GuildWarsControls.HealingSkill, (87, 24, 0)},
                {GuildWarsControls.UtilitySkill1, (148, 24, 0)},
                {GuildWarsControls.UtilitySkill2, (209, 24, 0)},
                {GuildWarsControls.UtilitySkill3, (270, 24, 0)},
                {GuildWarsControls.EliteSkill, (332, 24, 0)},
                {GuildWarsControls.ProfessionSkill1, (-316, 104, 48)},
                {GuildWarsControls.ProfessionSkill2, (-267, 104, 48)},
                {GuildWarsControls.ProfessionSkill3, (-218, 104, 48)},
                {GuildWarsControls.ProfessionSkill4, (-168, 104, 48)},
                {GuildWarsControls.ProfessionSkill5, (-107, 107, 45)},
                {GuildWarsControls.SpecialAction, (-85, 157, 54)}
            };
        private readonly Dictionary<GuildWarsControls, (int, int, int)> _layout;
        internal Warrior(byte specialization)
        {
            switch (specialization)
            {
                case 18:
                    _layout = Berserker;
                    break;
                case 61:
                    _layout = Spellbreaker;
                    break;
                default:
                    _layout = Base;
                    break;
            }
        }

        public (int, int, int) GetTransformation(GuildWarsControls skill)
        {
            return _layout?[skill] ?? (0,0,0);
        }
        public bool IsDynamic(GuildWarsControls skill)
        {
            return false;
        }
        public string GetDisplayText(GuildWarsControls skill)
        {
            return "";
        }
        public Color GetDisplayTextColor(GuildWarsControls skill)
        {
            return Color.White;
        }
    }
}