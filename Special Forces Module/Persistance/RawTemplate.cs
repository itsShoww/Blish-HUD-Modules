using System;
using System.IO;
using System.Linq;
using System.Net;
using Blish_HUD;
using Gw2Sharp.ChatLinks;
using Gw2Sharp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        public int Elite => IsEliteSpecialization();

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
            File.WriteAllText(path + ".json", json);
        }

        internal bool IsValid(string template = null)
        {
            return Build(template) != null;
        }
        internal string GetProfession()
        {
            return Build() != null ? Build().Profession.ToString() : "";
        }
        internal int GetFirstSpecialization()
        {
            return Build().Specialization1Id;
        }

        internal int GetSecondSpecialization()
        {
            return Build().Specialization2Id;
        }

        internal int GetThirdSpecialization()
        {
            return Build().Specialization3Id;
        }

        internal string GetDisplayName(int specializationId)
        {
            if (Build() == null) return "";

            var request = (HttpWebRequest) WebRequest.Create(@"https://api.guildwars2.com/v2/specializations/" + specializationId);
            using (var response = (HttpWebResponse) request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException()))
                {
                    var objText = reader.ReadToEnd();
                    var jsonObj = JsonConvert.DeserializeObject<JObject>(objText);
                    return (string) jsonObj["name"];
                }
            }
        }

        internal int IsEliteSpecialization()
        {
            if (Build() == null) return -1;

            var request = (HttpWebRequest) WebRequest.Create(@"https://api.guildwars2.com/v2/specializations/" + GetThirdSpecialization());
            using (var response = (HttpWebResponse) request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException()))
                {
                    var objText = reader.ReadToEnd();
                    var jsonObj = JsonConvert.DeserializeObject<JObject>(objText);
                    return (bool)jsonObj["elite"] ? GetThirdSpecialization() : -1;
                }
            }
        }
        internal string GetClassFriendlyName()
        {
            return IsEliteSpecialization() != -1
                ? GetDisplayName(GetThirdSpecialization())
                : GetProfession();
        }
    }

    internal class Rotation
    {
        [JsonProperty("opener")] public string Opener { get; set; }

        [JsonProperty("loop")] public string Loop { get; set; }
    }
}