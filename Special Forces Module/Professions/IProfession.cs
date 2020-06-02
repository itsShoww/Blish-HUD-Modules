using Blish_HUD.Controls.Intern;

namespace Special_Forces_Module.Professions
{
    public interface IProfession
    {
        (int, int, int) GetTransformation(GuildWarsControls skill);
    }
}