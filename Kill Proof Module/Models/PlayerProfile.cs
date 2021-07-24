using Blish_HUD.ArcDps.Common;
using KillProofModule.Persistance;

namespace KillProofModule.Models
{
    public class PlayerProfile
    {
        public string Identifier;
        public CommonFields.Player Player { get; set; }
        public KillProof KillProof { get; set; }
    }
}
