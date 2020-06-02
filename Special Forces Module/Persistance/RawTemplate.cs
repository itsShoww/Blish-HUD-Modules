using System;
using System.IO;
using System.Net;
using Blish_HUD;
using Gw2Sharp.ChatLinks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Special_Forces_Module.Persistance
{
    public class RawTemplate
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(RawTemplate));

        [JsonProperty("title")] public string Title { get; set; }

        [JsonProperty("patch")] public DateTime Patch { get; set; }

        [JsonProperty("template")] public string Template { get; set; }

        [JsonProperty("rotation")] public Rotation Rotation { get; set; }

        [JsonProperty("utilitykeys")] public int[] Utilitykeys { get; set; }

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

        public void Save()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            var title = Title;
            foreach (var c in Path.GetInvalidFileNameChars()) title = title.Replace(c, '-');
            var path = Path.Combine(
                SpecialForcesModule.ModuleInstance.DirectoriesManager.GetFullDirectoryPath("specialforces"), title);
            File.WriteAllText(path + ".json", json);
        }

        public bool IsValid(string template = null)
        {
            return Build(template) != null;
        }
        public string GetProfession()
        {
            return Build() != null ? Build().Profession.ToString() : "";
        }
        public int GetFirstSpecialization()
        {
            return Build().Specialization1Id;
        }

        public int GetSecondSpecialization()
        {
            return Build().Specialization2Id;
        }

        public int GetThirdSpecialization()
        {
            return Build().Specialization3Id;
        }

        public string GetEliteSpecialization()
        {
            var result = "";
            if (Build() == null) return result;

            StreamReader reader;
            string objText;
            var request = (HttpWebRequest) WebRequest.Create(@"https://api.guildwars2.com/v2/specializations/" +
                                                             Build().Specialization3Id);
            using (var response = (HttpWebResponse) request.GetResponse())
            {
                using (reader = new StreamReader(response.GetResponseStream()))
                {
                    objText = reader.ReadToEnd();
                    var jsonObj = JsonConvert.DeserializeObject<JObject>(objText);
                    if ((bool) jsonObj["elite"])
                        result = (string) jsonObj["name"];
                    return result;
                }
            }
        }

        public string GetClassFriendlyName()
        {
            return !GetEliteSpecialization().Equals("") ? GetEliteSpecialization() : GetProfession();
        }
    }

    public class Rotation
    {
        [JsonProperty("opener")] public string Opener { get; set; }

        [JsonProperty("loop")] public string Loop { get; set; }
    }
}