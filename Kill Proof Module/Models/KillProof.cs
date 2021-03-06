using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nekres.Kill_Proof_Module.Models
{
    /// <summary>
    ///     JSON class for replies from https://killproof.me/api/kp/
    /// </summary>
    public enum Mode
    {
        [EnumMember(Value = "none")]
        None,
        [EnumMember(Value = "fractal")]
        Fractal,
        [EnumMember(Value = "raid")]
        Raid
    }
    public class Title
    {
        [JsonConverter(typeof(StringEnumConverter)), JsonProperty("mode")] public Mode Mode { get; set; }
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
    }
    public class KillProof
    {
        [JsonProperty("linked")] public IList<KillProof> Linked { get; set; }
        [JsonProperty("valid_api_key")] public bool ValidApiKey { get; set; }
        [JsonProperty("titles")] public IList<Title> Titles { get; set; }
        [JsonProperty("proof_url")] public string ProofUrl { get; set; }
        [JsonProperty("coffers")] public IList<Token> Coffers { get; set; }
        [JsonProperty("tokens")] public IList<Token> Tokens { get; set; }
        [JsonProperty("killproofs")] public IList<Token> Killproofs { get; set; }
        [JsonProperty("kpid")] public string KpId { get; set; }
        [JsonProperty("last_refresh")] public DateTime LastRefresh { get; set; }
        [JsonProperty("account_name")] public string AccountName { get; set; }
        [JsonProperty("error")] public string Error { get; set; }
        public Token GetToken(int id)
        {
            return GetAllTokens().FirstOrDefault(x => x.Id == id);
        }
        public Token GetToken(string name)
        {
            name = name.Split('|').Reverse().ToList()[0].Trim();
            return GetAllTokens().FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }
        public IEnumerable<Token> GetAllTokens()
        {
            var tokens = Tokens ?? Enumerable.Empty<Token>();
            var killproofs = Killproofs ?? Enumerable.Empty<Token>();
            return tokens.Concat(killproofs);
        }
    }
}