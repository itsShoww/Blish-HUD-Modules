using System.Collections.Generic;
using Blish_HUD.Controls.Intern;
using Microsoft.Xna.Framework;

namespace Special_Forces_Module.Professions
{
    internal class Elementalist : IProfession
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
                {GuildWarsControls.ProfessionSkill1, (-316, 150, 48)},
                {GuildWarsControls.ProfessionSkill2, (-267, 150, 48)},
                {GuildWarsControls.ProfessionSkill3, (-218, 150, 48)},
                {GuildWarsControls.ProfessionSkill4, (-168, 150, 48)},
                {GuildWarsControls.ProfessionSkill5, (-107, 150, 45)},
                {GuildWarsControls.SpecialAction, (-85, 157, 54)}
            };

        private readonly Dictionary<GuildWarsControls, (int, int, int)> layout;

        private readonly Dictionary<GuildWarsControls, (int, int, int)> Tempest =
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
                {GuildWarsControls.ProfessionSkill1, (-316, 150, 48)},
                {GuildWarsControls.ProfessionSkill2, (-267, 150, 48)},
                {GuildWarsControls.ProfessionSkill3, (-218, 150, 48)},
                {GuildWarsControls.ProfessionSkill4, (-168, 150, 48)},
                {GuildWarsControls.ProfessionSkill5, (-107, 150, 45)},
                {GuildWarsControls.SpecialAction, (-85, 157, 54)}
            };

        private readonly Dictionary<GuildWarsControls, (int, int, int)> Weaver =
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
                {GuildWarsControls.ProfessionSkill1, (-316, 150, 48)},
                {GuildWarsControls.ProfessionSkill2, (-267, 150, 48)},
                {GuildWarsControls.ProfessionSkill3, (-218, 150, 48)},
                {GuildWarsControls.ProfessionSkill4, (-168, 150, 48)},
                {GuildWarsControls.ProfessionSkill5, (-107, 150, 45)},
                {GuildWarsControls.SpecialAction, (-85, 157, 54)}
            };

        internal string DisplayName;

        internal Elementalist(string specialization = "")
        {
            switch (specialization)
            {
                case "tempest":
                    layout = Tempest;
                    break;
                case "weaver":
                    layout = Weaver;
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
            switch (skill)
            {
                case GuildWarsControls.ProfessionSkill1:
                    return true;
                case GuildWarsControls.ProfessionSkill2:
                    return true;
                case GuildWarsControls.ProfessionSkill3:
                    return true;
                case GuildWarsControls.ProfessionSkill4:
                    return true;
                default:
                    return false;
            }
        }

        public string GetDisplayText(GuildWarsControls skill)
        {
            switch (skill)
            {
                case GuildWarsControls.ProfessionSkill1:
                    return "Attune Fire!";
                case GuildWarsControls.ProfessionSkill2:
                    return "Attune Water!";
                case GuildWarsControls.ProfessionSkill3:
                    return "Attune Air!";
                case GuildWarsControls.ProfessionSkill4:
                    return "Attune Earth!";
                default:
                    return "";
            }
        }
        public Color GetDisplayTextColor(GuildWarsControls skill)
        {
            switch (skill)
            {
                case GuildWarsControls.ProfessionSkill1:
                    return Color.Firebrick;
                case GuildWarsControls.ProfessionSkill2:
                    return Color.Aqua;
                case GuildWarsControls.ProfessionSkill3:
                    return Color.Violet;
                case GuildWarsControls.ProfessionSkill4:
                    return Color.SandyBrown;
                default:
                    return Color.White;
            }
        }
    }
}