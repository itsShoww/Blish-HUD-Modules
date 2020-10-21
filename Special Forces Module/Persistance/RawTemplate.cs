using Blish_HUD;
using Gw2Sharp.ChatLinks;
using Gw2Sharp.WebApi.V2.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

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
        [JsonIgnore]
        public Specialization Specialization
        {
            get
            {
                if (_specialization == null)
                {
                    var task = GetEliteSpecialization();
                    if (task != null)
                        _specialization = task.Result;
                    return _specialization;
                };
                return _specialization;
            }
            set {
                if (_specialization != null) return;
                _specialization = value;
            }
        }

        private BuildChatLink _buildChatLink;
        [JsonIgnore]
        public BuildChatLink BuildChatLink
        {
            get
            {
                if (_buildChatLink == null) _buildChatLink = Build();
                return _buildChatLink;
            }
            set {
                if (_buildChatLink != null) return;
                _buildChatLink = value;
            }
        }
        private BuildChatLink Build()
        {
            if (Template != null) {
                try {
                    return (BuildChatLink) Gw2ChatLink.Parse(Template);
                } catch (FormatException e) {
                    Logger.Error(e.Message + e.StackTrace);
                }
            }
            return (BuildChatLink) Gw2ChatLink.Parse("[&DQYAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=]");
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
        private Task<Specialization> GetEliteSpecialization()
        {
            if (BuildChatLink.Specialization3Id > 0)
                return GameService.Gw2WebApi.AnonymousConnection.Client.V2.Specializations.GetAsync(BuildChatLink.Specialization3Id);
            return default;
        }
        internal string GetClassFriendlyName()
        {
            return Specialization != null && Specialization.Elite
                ? Specialization.Name
                : BuildChatLink.Profession.ToString();
        }
    }

    internal class Rotation
    {
        [JsonProperty("opener")] public string Opener { get; set; }

        [JsonProperty("loop")] public string Loop { get; set; }
    }
}