using System;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework;
using static Blish_HUD.GameService;
using static Nekres.Kill_Proof_Module.KillProofModule;

namespace Nekres.Kill_Proof_Module.Controls.Views
{
    public class NotFoundView : IView
    {
        private readonly string _searchTerm;

        public NotFoundView(string searchTerm)
        {
            _searchTerm = searchTerm;
        }

        public async Task<bool> DoLoad(IProgress<string> progress)
        {
            return true;
        }

        public void DoBuild(Panel hPanel)
        {
            var tintPanel = new Panel
            {
                Parent = hPanel,
                Size = new Point(hPanel.Size.X - 150, hPanel.Size.Y - 150),
                Location = new Point(75, 75),
                ShowBorder = true,
                ShowTint = true
            };
            var labNothingHere = new Label
            {
                Parent = hPanel,
                Size = hPanel.Size,
                Location = new Point(0, -20),
                ShowShadow = true,
                StrokeText = true,
                Font = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size36, ContentService.FontStyle.Regular),
                Text = Properties.Resources.No_profile_for___0___found___.Replace("{0}", _searchTerm),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle
            };
            var labVisitUs = new Label
            {
                Parent = hPanel,
                Size = hPanel.Size,
                Location = new Point(0, -20),
                ShowShadow = true,
                StrokeText = true,
                Font = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size24, ContentService.FontStyle.Regular),
                Text = "\n\n" + Properties.Resources.Please__share_www_killproof_me_with_this_player_and_help_expand_our_database_,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle
            };
            if (ModuleInstance.PartyManager.Self.IsOwner(_searchTerm)) {
                labNothingHere.Text = Properties.Resources.Not_yet_registered___;
                labVisitUs.Text = "\n\n" + Properties.Resources.Visit_www_killproof_me_and_allow_us_to_record_your_KillProofs_for_you_;
            }
        }

        public void DoUnload()
        {
        }

        public event EventHandler<EventArgs> Loaded;
        public event EventHandler<EventArgs> Built;
        public event EventHandler<EventArgs> Unloaded;
    }
}
