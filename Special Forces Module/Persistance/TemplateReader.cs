using Blish_HUD;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Nekres.Special_Forces_Module.Persistance
{
    internal class TemplateReader
    {
        private readonly List<RawTemplate> _cached = new List<RawTemplate>();
        private readonly IsoDateTimeConverter _dateFormat = new IsoDateTimeConverter {DateTimeFormat = "dd/MM/yyyy"};
        private string[] _loaded;

        private bool IsLocalPath(string p)
        {
            return new Uri(p).IsFile;
        }

        internal List<RawTemplate> LoadMultiple(string uri)
        {
            StreamReader reader;
            string objText;

            if (IsLocalPath(uri))
                using (reader = new StreamReader(uri))
                {
                    objText = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<List<RawTemplate>>(objText, _dateFormat);
                }

            var request = (HttpWebRequest) WebRequest.Create(uri);
            using (var response = (HttpWebResponse) request.GetResponse())
            {
                using (reader = new StreamReader(response.GetResponseStream()))
                {
                    objText = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<List<RawTemplate>>(objText, _dateFormat);
                }
            }
        }

        internal RawTemplate LoadSingle(string uri)
        {
            StreamReader reader;
            string objText;

            if (IsLocalPath(uri))
                using (reader = new StreamReader(uri))
                {
                    objText = reader.ReadToEnd();

                    return JsonConvert.DeserializeObject<RawTemplate>(objText, _dateFormat);
                }

            var request = (HttpWebRequest) WebRequest.Create(uri);
            using (var response = (HttpWebResponse) request.GetResponse())
            {
                using (reader = new StreamReader(response.GetResponseStream()))
                {
                    objText = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<RawTemplate>(objText, _dateFormat);
                }
            }
        }

        internal async Task<List<RawTemplate>> LoadDirectory(string path)
        {
            _cached.Clear();
            _loaded = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var file in _loaded) _cached.Add(LoadSingle(file));

            var eliteIds = _cached.Where(template => template.BuildChatLink.Specialization3Id > 0)
                                          .Select(template => (int)template.BuildChatLink.Specialization3Id).ToList();

            var elites = await GameService.Gw2WebApi.AnonymousConnection.Client.V2.Specializations.ManyAsync(eliteIds);

            foreach (RawTemplate template in _cached)
            {
                if (template.BuildChatLink.Specialization3Id <= 0) continue;
                template.Specialization = elites.First(x => x.Id == template.BuildChatLink.Specialization3Id);
            }

            return _cached;
        }
    }
}