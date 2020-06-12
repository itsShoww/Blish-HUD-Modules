using Special_Forces_Module.Persistance;
using Special_Forces_Module.Professions;
using System;

namespace Special_Forces_Module.Parser
{
    internal static class TemplateParser
    {
        internal static IProfession Parse(RawTemplate template)
        {
            IProfession obj;

            var profession = template.GetProfession().ToLowerInvariant();
            var specialization = template.GetClassFriendlyName();

            switch (profession)
            {
                case "engineer":
                    obj = new Engineer(specialization);
                    break;
                case "necromancer":
                    obj = new Necromancer(specialization);
                    break;
                case "ranger":
                    obj = new Ranger(specialization);
                    break;
                case "warrior":
                    obj = new Warrior(specialization);
                    break;
                case "guardian":
                    obj = new Guardian(specialization);
                    break;
                case "thief":
                    obj = new Thief(specialization);
                    break;
                case "elementalist":
                    obj = new Elementalist(specialization);
                    break;
                case "mesmer":
                    obj = new Mesmer(specialization);
                    break;
                case "revenant":
                    obj = new Revenant(specialization);
                    break;

                default: throw new NotSupportedException(specialization);
            }

            return obj;
        }
    }
}