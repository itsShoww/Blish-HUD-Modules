using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Special_Forces_Module.Persistance
{
    public class TemplateReader
    {
        private readonly List<RawTemplate> cached = new List<RawTemplate>();
        private readonly IsoDateTimeConverter dateFormat = new IsoDateTimeConverter {DateTimeFormat = "dd/MM/yyyy"};
        private string[] loaded;

        private bool IsLocalPath(string p)
        {
            return new Uri(p).IsFile;
        }

        public List<RawTemplate> LoadMultiple(string uri)
        {
            StreamReader reader;
            string objText;

            if (IsLocalPath(uri))
                using (reader = new StreamReader(uri))
                {
                    objText = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<List<RawTemplate>>(objText, dateFormat);
                }

            var request = (HttpWebRequest) WebRequest.Create(uri);
            using (var response = (HttpWebResponse) request.GetResponse())
            {
                using (reader = new StreamReader(response.GetResponseStream()))
                {
                    objText = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<List<RawTemplate>>(objText, dateFormat);
                }
            }
        }

        public RawTemplate LoadSingle(string uri)
        {
            StreamReader reader;
            string objText;

            if (IsLocalPath(uri))
                using (reader = new StreamReader(uri))
                {
                    objText = reader.ReadToEnd();

                    return JsonConvert.DeserializeObject<RawTemplate>(objText, dateFormat);
                }

            var request = (HttpWebRequest) WebRequest.Create(uri);
            using (var response = (HttpWebResponse) request.GetResponse())
            {
                using (reader = new StreamReader(response.GetResponseStream()))
                {
                    objText = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<RawTemplate>(objText, dateFormat);
                }
            }
        }

        public List<RawTemplate> LoadDirectory(string path)
        {
            cached.Clear();
            loaded = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var file in loaded) cached.Add(LoadSingle(file));
            return cached;
        }
    }
}