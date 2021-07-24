using Blish_HUD;
using Blish_HUD.ArcDps.Common;
using Gw2Sharp.WebApi.V2.Models;
using KillProofModule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static Blish_HUD.GameService;
using static KillProofModule.KillProofModule;

namespace KillProofModule.Manager
{
    public class PartyManager
    {
        public List<PlayerProfile> Players;

        public PlayerProfile Self;

        public event EventHandler<ValueEventArgs<PlayerProfile>> PlayerAdded;
        public event EventHandler<ValueEventArgs<PlayerProfile>> PlayerLeft;

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
                    if (!result.IsCompleted || result.IsFaulted)
                    {
                        Self.Identifier = result.Result.Name;
                        Self.KillProof = await ProfileManager.GetKillProofContent(result.Result.Name);
                    }
                });
            }
        }

        private void PlayerAddedEvent(CommonFields.Player player)
        {
            if (player.Self)
            {
                Self.Player = player;
                return;
            }
            var profile = Players.FirstOrDefault(p => p.Player.AccountName.Equals(player.AccountName)) ?? new PlayerProfile();
            profile.Player = player;
            PlayerAdded?.Invoke(this, new ValueEventArgs<PlayerProfile>(profile));
        }

        private void PlayerLeavesEvent(CommonFields.Player player)
        {
            if (!ModuleInstance.AutomaticClearEnabled.Value) return;
            var profile = Players.FirstOrDefault(p => p.Player.AccountName.Equals(player.AccountName));
            Players.Remove(profile);
            PlayerLeft?.Invoke(this, new ValueEventArgs<PlayerProfile>(profile));
        }
    }
}
