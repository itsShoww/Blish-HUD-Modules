using Blish_HUD.Controls.Intern;
using Microsoft.Xna.Framework;

namespace Nekres.Special_Forces_Module.Professions
{
    internal interface IProfession
    { 
        (int, int, int) GetTransformation(GuildWarsControls skill);

        bool IsDynamic(GuildWarsControls skill);

        string GetDisplayText(GuildWarsControls skill);

        Color GetDisplayTextColor(GuildWarsControls skill);

    }
}