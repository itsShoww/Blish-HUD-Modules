using System.Collections.Generic;
using Blish_HUD.Controls.Intern;

namespace Special_Forces_Module.Professions
{
    public class Mesmer : IProfession
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

        private readonly Dictionary<GuildWarsControls, (int, int, int)> Chronomancer =
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

        private readonly Dictionary<GuildWarsControls, (int, int, int)> Mirage =
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

        public Mesmer(string specialization = "")
        {
            switch (specialization)
            {
                case "chronomancer":
                    layout = Chronomancer;
                    break;
                case "mirage":
                    layout = Mirage;
                    break;
                default:
                    layout = Base;
                    break;
            }
        }

        public (int, int, int) GetTransformation(GuildWarsControls skill)
        {
            var transform = (0, 0, 0);


            return transform;
        }
    }
}