using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using KillProofModule.Manager;
using KillProofModule.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Blish_HUD.GameService;
using static KillProofModule.KillProofModule;

namespace KillProofModule.Controls.Views
{
    public class MainView : IView
    {
        #region Constants

        private const int TOP_MARGIN = 0;
        private const int RIGHT_MARGIN = 5;
        private const int BOTTOM_MARGIN = 10;
        private const int LEFT_MARGIN = 8;

        #endregion

        private readonly Point LABEL_SMALL = new Point(400, 30);

        private readonly Regex Gw2AccountName = new Regex(@".{3,32}", RegexOptions.Singleline | RegexOptions.Compiled);

        private Texture2D _killProofMeLogoTexture;

        private List<PlayerButton> _displayedPlayers;
        private Panel _squadPanel;

        public MainView()
        {
            _displayedPlayers = new List<PlayerButton>();
            ModuleInstance.PartyManager.PlayerAdded += PlayerAddedEvent;
            ModuleInstance.PartyManager.PlayerLeft += PlayerLeavesEvent;
        }

        public async Task<bool> DoLoad(IProgress<string> progress)
        {
            _killProofMeLogoTexture = ModuleInstance.ContentsManager.GetTexture("killproof_logo.png");
            return true;
        }

        public void DoBuild(Panel buildPanel)
        {
            buildPanel.CanScroll = false;

            _squadPanel = BuildBody(BuildHeader(buildPanel));
            BuildFooter(_squadPanel);
        }

        public void DoUnload()
        {
            _displayedPlayers.Clear();
        }

        public event EventHandler<EventArgs> Loaded;
        public event EventHandler<EventArgs> Built;
        public event EventHandler<EventArgs> Unloaded;

        private Panel BuildHeader(Panel buildPanel)
        {
            var header = new Panel
            {
                Parent = buildPanel,
                Size = new Point(buildPanel.Width, 200),
                Location = new Point(0, 0),
                CanScroll = false
            };

            var imgKillproof = new Image(_killProofMeLogoTexture)
            {
                Parent = header,
                Size = new Point(128, 128),
                Location = new Point(LEFT_MARGIN + 10, TOP_MARGIN + 5)
            };
            var labAccountName = new Label
            {
                Parent = header,
                Size = new Point(300, 30),
                Location = new Point(header.Width / 2 - 100, header.Height / 2 + 30 + TOP_MARGIN),
                StrokeText = true,
                ShowShadow = true,
                Text = Properties.Resources.Account_Name_or_KillProof_me_ID_
            };
            var tbAccountName = new TextBox
            {
                Parent = header,
                Size = new Point(200, 30),
                Location = new Point(header.Width / 2 - 100, labAccountName.Bottom + TOP_MARGIN),
                PlaceholderText = "Player.0000"
            };
            tbAccountName.EnterPressed += delegate
            {
                if (!string.IsNullOrEmpty(tbAccountName.Text) && Gw2AccountName.IsMatch(tbAccountName.Text))
                {
                    Overlay.BlishHudWindow.Navigate(new LoadingView());
                    ProfileManager.GetKillProofContent(tbAccountName.Text).ContinueWith(kpResult =>
                    {
                        if (!kpResult.IsCompleted || kpResult.IsFaulted) return;
                        var killproof = kpResult.Result;
                        if (string.IsNullOrEmpty(killproof.Error))
                        {
                            Overlay.BlishHudWindow.Navigate(new ProfileView(killproof));
                        }
                        else
                        {
                            Overlay.BlishHudWindow.Navigate(new NotFoundView(tbAccountName.Text));
                        }
                    });
                }
                tbAccountName.Focused = false;
            };
            var labSquadPanel = new Label
            {
                Parent = header,
                Size = new Point(300, 40),
                Font = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size24,
                    ContentService.FontStyle.Regular),
                StrokeText = true,
                Location = new Point(LEFT_MARGIN, header.Bottom - 40),
                Text = Properties.Resources.Recent_profiles_
            };

            if (!string.IsNullOrEmpty(ModuleInstance.PartyManager.Self.Identifier)) {
                var selfButtonPanel = new Panel
                {
                    Parent = header,
                    Size = new Point(335, 114),
                    ShowBorder = true,
                    ShowTint = true,
                    Location =
                        new Point(header.Right - 335 - RIGHT_MARGIN, TOP_MARGIN + 15)
                };

                var smartPingCheckBox = new Checkbox
                {
                    Parent = header,
                    Location = new Point(selfButtonPanel.Location.X + LEFT_MARGIN, selfButtonPanel.Bottom),
                    Size = new Point(selfButtonPanel.Width, 30),
                    Text = Properties.Resources.Show_Smart_Ping_Menu,
                    BasicTooltipText = Properties.Resources.Shows_a_menu_on_the_top_left_corner_of_your_screen_which_allows_you_to_quickly_access_and_ping_your_killproofs_,
                    Checked = ModuleInstance.SmartPingMenuEnabled.Value
                };
                smartPingCheckBox.CheckedChanged += (_,e) => ModuleInstance.SmartPingMenuEnabled.Value = e.Checked;

                var localPlayerButton = new DetailsButton
                {
                    Parent = selfButtonPanel,
                    Icon = Content.GetTexture("common/733268"),
                    Text = ModuleInstance.PartyManager.Self.Identifier,
                    Title = ModuleInstance.PartyManager.Self.Identifier,
                    Location = new Point(0, 0)
                };
            }
            return header;
        }

        private Panel BuildBody(Panel header)
        {
            var body = new Panel
            {
                Parent = header.Parent,
                Size = new Point(header.Size.X, header.Parent.Height - header.Height),
                Location = new Point(0, header.Bottom),
                ShowBorder = true,
                CanScroll = true,
                ShowTint = true
            };

            // Features only available when ArcDps is installed.
            if (ArcDps.Loaded)
            {
                var clearButton = new StandardButton()
                {
                    Parent = header.Parent,
                    Size = new Point(100, 30),
                    Location = new Point(body.Location.X + body.Width - 100 - RIGHT_MARGIN, body.Location.Y + body.Height + BOTTOM_MARGIN),
                    Text = Properties.Resources.Clear,
                    BasicTooltipText = Properties.Resources.Removes_profiles_of_players_which_are_not_in_squad_
                };

                var clearCheckbox = new Checkbox()
                {
                    Parent = header.Parent,
                    Size = new Point(20, 30),
                    Location = new Point(clearButton.Location.X - 20 - RIGHT_MARGIN, clearButton.Location.Y),
                    Text = "",
                    BasicTooltipText = Properties.Resources.Remove_leavers_automatically_,
                    Checked = ModuleInstance.AutomaticClearEnabled.Value
                };

                clearCheckbox.CheckedChanged += (_,e) => ModuleInstance.AutomaticClearEnabled.Value = e.Checked;

                clearButton.Click += delegate
                {
                    foreach (var c in _displayedPlayers.ToArray())
                    {
                        if (c == null)
                            _displayedPlayers.Remove(null);
                        else if (!ArcDps.Common.PlayersInSquad.Any(p => p.Value.AccountName.Equals(c.PlayerProfile.Player.AccountName)))
                        {
                            _displayedPlayers.Remove(c);
                            c.Dispose();
                        }
                    }
                };
            }
            return body;
        }

        private void BuildFooter(Panel body)
        {
            var footer = new Panel
            {
                Parent = body.Parent,
                Size = new Point(body.Parent.Width, 50),
                Location = new Point(0, body.Height + 10),
                CanScroll = false
            };
            var creditLabel = new Label
            {
                Parent = footer,
                Size = LABEL_SMALL,
                HorizontalAlignment = HorizontalAlignment.Center,
                Location = new Point(footer.Width / 2 - LABEL_SMALL.X / 2, footer.Height / 2 - LABEL_SMALL.Y / 2),
                StrokeText = true,
                ShowShadow = true,
                Text = Properties.Resources.Powered_by_www_killproof_me
            };
        }

        private async void PlayerAddedEvent(object o, ValueEventArgs<PlayerProfile> profile)
        {
            var playerBtn = _displayedPlayers.FirstOrDefault(x => x.PlayerProfile.Player.AccountName.Equals(profile.Value.Player.AccountName, StringComparison.InvariantCultureIgnoreCase));
            if (playerBtn == null && await ProfileManager.ProfileAvailable(profile.Value.Player.AccountName))
            {
                var playerButton = new PlayerButton(profile.Value)
                {
                    Parent = _squadPanel,
                    Icon = ModuleInstance.GetProfessionRender(profile.Value.Player),
                    Font = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size16, ContentService.FontStyle.Regular)
                };
                playerButton.Click += delegate
                {
                    playerButton.IsNew = false;
                    Overlay.BlishHudWindow.Navigate(new LoadingView());
                    ProfileManager.GetKillProofContent(playerButton.PlayerProfile.Identifier).ContinueWith(kpResult =>
                    {
                        if (!kpResult.IsCompleted || kpResult.IsFaulted) return;
                        var killproof = kpResult.Result;
                        if (string.IsNullOrEmpty(killproof.Error))
                        {
                            Overlay.BlishHudWindow.Navigate(new ProfileView(killproof));
                        }
                        else
                        {
                            Overlay.BlishHudWindow.Navigate(new NotFoundView(playerButton.PlayerProfile.Player.AccountName));
                        }
                    });
                };
                _displayedPlayers.Add(playerButton);
            }

            PlayerNotification.ShowNotification(profile.Value.Player.AccountName, ModuleInstance.GetProfessionRender(profile.Value.Player), Properties.Resources.profile_available, 10);

            RepositionPlayers();
        }

        private void PlayerLeavesEvent(object o, ValueEventArgs<PlayerProfile> profile)
        {
            if (!ModuleInstance.AutomaticClearEnabled.Value) return;
            var profileBtn = _displayedPlayers.FirstOrDefault(x => x.PlayerProfile.Player.AccountName.Equals(profile.Value.Player.AccountName));
            _displayedPlayers.Remove(profileBtn);
            profileBtn?.Dispose();
        }

        private void RepositionPlayers()
        {
            var sorted = from player in _displayedPlayers
                orderby player.IsNew descending
                select player;

            var pos = 0;
            foreach (var e in sorted)
            {
                var x = pos % 3;
                var y = pos / 3;
                e.Location = new Point(x * (e.Width + 8), y * (e.Height + 8));

                ((Panel)e.Parent).VerticalScrollOffset = 0;
                e.Parent.Invalidate();
                if (e.Visible) pos++;
            }
        }
    }
}
