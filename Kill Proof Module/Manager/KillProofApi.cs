using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nekres.Kill_Proof_Module.Models;
using Nekres.Kill_Proof_Module.Utils;
using Newtonsoft.Json;
using static Blish_HUD.GameService;
using static Nekres.Kill_Proof_Module.KillProofModule;

namespace Nekres.Kill_Proof_Module.Manager
{
    public class KillProofApi
    {
        private const string KILLPROOF_API_URL = "https://killproof.me/api/";

        private static List<KillProof> _cachedKillProofs;

        static KillProofApi()
        {
            _cachedKillProofs = new List<KillProof>();
        }

        public static async Task<Resources> LoadResources()
        {
            return await TaskUtil.GetJsonResponse<Resources>(KILLPROOF_API_URL + "resources?lang=" + Overlay.UserLocale.Value)
                .ContinueWith(result =>
                {
                    if (!result.IsCompleted || !result.Result.Item1)
                    {
                        using (var fs = ModuleInstance.ContentsManager.GetFileStream("resources.json"))
                        {
                            fs.Position = 0;
                            using (var jsonReader = new JsonTextReader(new StreamReader(fs)))
                            {
                                var serializer = new JsonSerializer();
                                return serializer.Deserialize<Resources>(jsonReader);
                            }
                        }
                    }
                    else
                    {
                        return result.Result.Item2;
                    }
                });
        }

        public static async Task<bool> ProfileAvailable(string account)
        {
            var (responseSuccess, optionalKillProof) =
                await TaskUtil.GetJsonResponse<KillProof>(KILLPROOF_API_URL + $"kp/{account}?lang=" + Overlay.UserLocale.Value);

            return responseSuccess && optionalKillProof?.Error == null;
        }

        public static async Task<KillProof> GetKillProofContent(string account)
        {
            if (_cachedKillProofs.Any(x => x.AccountName.Equals(account, StringComparison.InvariantCultureIgnoreCase)))
                return _cachedKillProofs.FirstOrDefault(x =>
                    x.AccountName.Equals(account, StringComparison.InvariantCultureIgnoreCase));

            var (responseSuccess, killProof) = await TaskUtil.GetJsonResponse<KillProof>(KILLPROOF_API_URL + $"kp/{account}?lang=" + Overlay.UserLocale.Value)
                .ConfigureAwait(false);

            if (responseSuccess && killProof?.Error == null)
            {
                _cachedKillProofs.Add(killProof);
                return killProof;
            }
            return null;
        }
    }
}
