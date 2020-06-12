using Blish_HUD;
using Gw2Sharp.ChatLinks;
using Gw2Sharp.Models;
using Gw2Sharp.WebApi.V2.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace Special_Forces_Module.Persistance
{
    internal class RawTemplate
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(RawTemplate));

        [JsonProperty("title")] public string Title { get; set; }

        [JsonProperty("patch")] public DateTime Patch { get; set; }

        [JsonProperty("template")] public string Template { get; set; }

        [JsonProperty("rotation")] public Rotation Rotation { get; set; }

        [JsonProperty("utilitykeys")] public int[] Utilitykeys { get; set; }

        private Specialization _specialization;
        public Specialization Specialization
        {
            get
            {
                if (_specialization == null) GetEliteSpecialization();
                return _specialization;
            }
            set {
                if (_specialization != null) return;
                _specialization = value;
            }
        }

        public int Profession => (int)Enum.GetValues(typeof(ProfessionType)).Cast<ProfessionType>().ToList()
                                          .Find(x => x.ToString().Equals(GetProfession(), StringComparison.InvariantCultureIgnoreCase));
        private BuildChatLink Build(string template = null)
        {
            BuildChatLink result;
            try
            {
                result = (BuildChatLink) Gw2ChatLink.Parse(template ?? Template);
            }
            catch (FormatException e)
            {
                result = null;
                Logger.Warn(e.Message);
            }

            return result;
        }

        internal void Save()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            var title = Title;
            foreach (var c in Path.GetInvalidFileNameChars()) title = title.Replace(c, '-');
            var path = Path.Combine(
                SpecialForcesModule.ModuleInstance.DirectoriesManager.GetFullDirectoryPath("specialforces"), title);
            System.IO.File.WriteAllText(path + ".json", json);
        }

        internal bool IsValid(string template = null)
        {
            return Build(template) != null;
        }
        internal string GetProfession()
        {
            return IsValid() ? Build().Profession.ToString() : "";
        }
        internal int GetFirstSpecialization()
        {
            return IsValid() ? Build().Specialization1Id : -1;
        }

        internal int GetSecondSpecialization()
        {
            return IsValid() ? Build().Specialization2Id : -1;
        }

        internal int GetThirdSpecialization()
        {
            return IsValid() ? Build().Specialization3Id : -1;
        }
        private async void GetEliteSpecialization()
        {
            if (GetThirdSpecialization() > 0)
                _specialization = await GameService.Gw2WebApi.AnonymousConnection.Client.V2.Specializations.GetAsync(GetThirdSpecialization());
        }
        internal string GetClassFriendlyName()
        {
            return Specialization.Elite
                ? Specialization.Name
                : GetProfession();
        }
    }

    internal class Rotation
    {
        [JsonProperty("opener")] public string Opener { get; set; }

        [JsonProperty("loop")] public string Loop { get; set; }
    }
}