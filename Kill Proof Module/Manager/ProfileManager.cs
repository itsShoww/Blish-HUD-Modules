using System;
using System.Collections.Generic;
using System.Linq;
using KillProofModule.Persistance;
using System.Threading.Tasks;
using static Blish_HUD.GameService;
using static KillProofModule.KillProofModule;

namespace KillProofModule.Manager
{
    public class ProfileManager
    {
        private const string KILLPROOF_API_URL = "https://killproof.me/api/";

        private static List<KillProof> _cachedKillProofs;

        static ProfileManager()
        {
            _cachedKillProofs = new List<KillProof>();
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
