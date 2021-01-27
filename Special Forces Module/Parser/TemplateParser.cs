using Nekres.Special_Forces_Module.Persistance;
using Nekres.Special_Forces_Module.Professions;
using System;
using Gw2Sharp.Models;

namespace Nekres.Special_Forces_Module.Parser
{
    internal static class TemplateParser
    {
        internal static IProfession Parse(RawTemplate template)
        {
            IProfession obj;

            var profession = template.BuildChatLink.Profession;
            var specialization = template.BuildChatLink.Specialization3Id;

            switch (profession)
            {
                case ProfessionType.Engineer:
                    obj = new Engineer(specialization);
                    break;
                case ProfessionType.Necromancer:
                    obj = new Necromancer(specialization);
                    break;
                case ProfessionType.Ranger:
                    obj = new Ranger(specialization);
                    break;
                case ProfessionType.Warrior:
                    obj = new Warrior(specialization);
                    break;
                case ProfessionType.Guardian:
                    obj = new Guardian(specialization);
                    break;
                case ProfessionType.Thief:
                    obj = new Thief(specialization);
                    break;
                case ProfessionType.Elementalist:
                    obj = new Elementalist(specialization);
                    break;
                case ProfessionType.Mesmer:
                    obj = new Mesmer(specialization);
                    break;
                case ProfessionType.Revenant:
                    obj = new Revenant(specialization);
                    break;
                default: throw new NotSupportedException($"Profession {profession} is not supported.");
            }
            return obj;
        }
    }
}