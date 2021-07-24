using Blish_HUD.ArcDps.Common;

namespace Nekres.Kill_Proof_Module.Models
{
    public class PlayerProfile
    {
        public string Identifier;
        public CommonFields.Player Player { get; set; }
        public KillProof KillProof { get; set; }
    }
}
