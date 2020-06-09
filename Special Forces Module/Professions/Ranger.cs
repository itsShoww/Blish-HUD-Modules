using System.Collections.Generic;
using Blish_HUD.Controls.Intern;
using Microsoft.Xna.Framework;

namespace Special_Forces_Module.Professions
{
    internal class Ranger : IProfession
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

        private readonly Dictionary<GuildWarsControls, (int, int, int)> Druid =
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

        private readonly Dictionary<GuildWarsControls, (int, int, int)> layout;

        private readonly Dictionary<GuildWarsControls, (int, int, int)> Soulbeast =
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

        internal Ranger(string specialization = "")
        {
            switch (specialization)
            {
                case "druid":
                    layout = Druid;
                    break;
                case "soulbeast":
                    layout = Soulbeast;
                    break;
                default:
                    layout = Base;
                    break;
            }
        }

        public (int, int, int) GetTransformation(GuildWarsControls skill)
        {
            return layout?[skill] ?? (0,0,0);
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