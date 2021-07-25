using System;
using Blish_HUD;
using Blish_HUD.ArcDps.Common;

namespace Nekres.Kill_Proof_Module.Models
{
    public class PlayerProfile
    {
        public event EventHandler<ValueEventArgs<CommonFields.Player>> PlayerChanged;
        public event EventHandler<ValueEventArgs<KillProof>> KillProofChanged;

        public string AccountName => KillProof?.AccountName ?? Player.AccountName ?? "";
        public string CharacterName => Player.CharacterName ?? (IsSelf ? GameService.Gw2Mumble.PlayerCharacter.Name : Nickname());
        public string KpId => KillProof?.KpId ?? "";

        public readonly bool IsSelf;

        public PlayerProfile(bool isSelf = false)
        {
            IsSelf = isSelf;
        }

        private CommonFields.Player _player;
        public CommonFields.Player Player { 
            get => _player;
            set
            {
                if (_player.Equals(value)) return;
                _player = value; 
                PlayerChanged?.Invoke(this, new ValueEventArgs<CommonFields.Player>(value));
            }
        }

        private KillProof _killProof;
        public KillProof KillProof
        {
            get => _killProof;
            set
            {
                if (_killProof != null && _killProof.Equals(value)) return;
                _killProof = value;
                KillProofChanged?.Invoke(this, new ValueEventArgs<KillProof>(value));
            }
        }

        public bool IsOwner(string accountNameOrKpId)
        {
            if (string.IsNullOrEmpty(accountNameOrKpId)) return false;
            return !string.IsNullOrEmpty(AccountName) && AccountName.Equals(accountNameOrKpId, StringComparison.InvariantCultureIgnoreCase)
                    || !string.IsNullOrEmpty(KpId) && KpId.Equals(accountNameOrKpId, StringComparison.InvariantCulture);
        }

        public bool Equals(PlayerProfile other)
        {
            return IsOwner(other.AccountName) || IsOwner(other.KpId);
        }

        public bool HasKillProof()
        {
            return KillProof != null && string.IsNullOrEmpty(KillProof.Error);
        }

        public string Nickname()
        {
            var index = AccountName.IndexOf('.');
            return AccountName.Substring(0, index < 0 ? 0 : index);
        }
    }
}
