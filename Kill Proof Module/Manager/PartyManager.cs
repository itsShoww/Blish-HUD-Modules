using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.ArcDps.Common;
using Gw2Sharp.WebApi.V2.Models;
using Nekres.Kill_Proof_Module.Models;
using static Blish_HUD.GameService;
using static Nekres.Kill_Proof_Module.KillProofModule;

namespace Nekres.Kill_Proof_Module.Manager
{
    public class PartyManager
    {
        public List<PlayerProfile> Players;

        public PlayerProfile Self;

        public event EventHandler<ValueEventArgs<PlayerProfile>> PlayerAdded;
        public event EventHandler<ValueEventArgs<PlayerProfile>> PlayerLeft;
        public event EventHandler<ValueEventArgs<PlayerProfile>> SelfUpdated;

        public PartyManager()
        {
            Self = new PlayerProfile();
            Players = new List<PlayerProfile>();

            RequestSelf();

            ArcDps.Common.Activate();
            ArcDps.Common.PlayerAdded += PlayerAddedEvent;
            ArcDps.Common.PlayerRemoved += PlayerLeavesEvent;
        }

        public async void RequestSelf()
        {
            if (ModuleInstance.Gw2ApiManager.HavePermission(TokenPermission.Account))
            {
                await ModuleInstance.Gw2ApiManager.Gw2ApiClient.V2.Account.GetAsync().ContinueWith(async result =>
                {
                    if (!result.IsCompleted || result.IsFaulted) return;
                    Self.Identifier = result.Result.Name;
                    await KillProofApi.GetKillProofContent(result.Result.Name).ContinueWith(res =>
                    {
                        if (!res.IsCompleted || res.IsFaulted) return null;

                        Self.KillProof = res.Result;
                        SelfUpdated?.Invoke(this, new ValueEventArgs<PlayerProfile>(Self));
                        return res.Result;
                    });
                });
            }
        }

        private void PlayerAddedEvent(CommonFields.Player player)
        {
            if (player.Self)
            {
                Self.Player = player;
                SelfUpdated?.Invoke(this, new ValueEventArgs<PlayerProfile>(Self));
                return;
            }
            var profile = Players.FirstOrDefault(p => p.Identifier.Equals(player.AccountName)) ?? new PlayerProfile();
            profile.Player = player;
            PlayerAdded?.Invoke(this, new ValueEventArgs<PlayerProfile>(profile));
        }

        private void PlayerLeavesEvent(CommonFields.Player player)
        {
            if (player.Self) return;
            var profile = Players.FirstOrDefault(p => p.Identifier.Equals(player.AccountName));
            Players.Remove(profile);
            PlayerLeft?.Invoke(this, new ValueEventArgs<PlayerProfile>(profile));
        }
    }
}
