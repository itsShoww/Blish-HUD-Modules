using Blish_HUD.ArcDps.Common;

namespace Nekres.Kill_Proof_Module.Models
{
    public class PlayerProfile
    {
        private string _identifier;
        public string Identifier
        {
            get => !string.IsNullOrEmpty(_identifier) ? _identifier : KillProof?.AccountName ?? Player.AccountName;
            set => _identifier = value;
        }
        public CommonFields.Player Player { get; set; }
        public KillProof KillProof { get; set; }
    }
}
