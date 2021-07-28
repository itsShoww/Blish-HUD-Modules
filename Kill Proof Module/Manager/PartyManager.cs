using Blish_HUD;
using Blish_HUD.ArcDps.Common;
using Gw2Sharp.WebApi.V2.Models;
using Nekres.Kill_Proof_Module.Controls;
using Nekres.Kill_Proof_Module.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static Blish_HUD.GameService;
using static Nekres.Kill_Proof_Module.KillProofModule;

namespace Nekres.Kill_Proof_Module.Manager
{
    public class PartyManager
    {
        private Logger Logger = Logger.GetLogger<PartyManager>();

        public List<PlayerProfile> Players;

        public PlayerProfile Self;

        public event EventHandler<ValueEventArgs<PlayerProfile>> PlayerAdded;
        public event EventHandler<ValueEventArgs<PlayerProfile>> PlayerLeft;

        public PartyManager()
        {
            Self = new PlayerProfile(true);
            Players = new List<PlayerProfile>();

            RequestSelf();

            ArcDps.Common.Activate();
            ArcDps.Common.PlayerAdded += PlayerAddedEvent;
            ArcDps.Common.PlayerRemoved += PlayerLeavesEvent;
        }

        public async void RequestSelf()
        {
            if (!string.IsNullOrEmpty(Self.AccountName))
            {
                Self.KillProof = await KillProofApi.GetKillProofContent(Self.AccountName);
            } 
            else if (ModuleInstance.Gw2ApiManager.HavePermission(TokenPermission.Account))
            {
                await ModuleInstance.Gw2ApiManager.Gw2ApiClient.V2.Account.GetAsync().ContinueWith(async result =>
                {
                    if (!result.IsCompleted || result.IsFaulted) return;
                    Self.KillProof = await KillProofApi.GetKillProofContent(result.Result.Name);
                });
            }
        }

        private async void PlayerAddedEvent(CommonFields.Player player)
        {
            if (player.Self)
            {
                Self.Player = player;
                RequestSelf();
                return;
            }

            var profile = Players.FirstOrDefault(p => p.IsOwner(player.AccountName));
            if (profile == null) 
            {
                profile = new PlayerProfile { Player = player };
                Players.Add(profile);
                PlayerAdded?.Invoke(this, new ValueEventArgs<PlayerProfile>(profile));
                await KillProofApi.ProfileAvailable(player.AccountName).ContinueWith(response =>
                {
                    if (!response.IsCompleted || response.IsFaulted) return;
                    if (response.Result)
                        PlayerNotification.ShowNotification(profile, Properties.Resources.profile_available, 10);
                });
            } 
            else
            {
                profile.Player = player;
            }
        }

        private void PlayerLeavesEvent(CommonFields.Player player)
        {
            if (player.Self || !ModuleInstance.AutomaticClearEnabled.Value) return;
            var profile = Players.FirstOrDefault(p => p.IsOwner(player.AccountName));
            Players.Remove(profile);
            PlayerLeft?.Invoke(this, new ValueEventArgs<PlayerProfile>(profile));
        }
    }
}
