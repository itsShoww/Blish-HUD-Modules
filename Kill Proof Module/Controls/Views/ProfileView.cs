using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nekres.Kill_Proof_Module.Models;
using static Blish_HUD.GameService;
using static Nekres.Kill_Proof_Module.KillProofModule;

namespace Nekres.Kill_Proof_Module.Controls.Views
{
    public class ProfileView : IView
    {
        #region Constants

        private const int TOP_MARGIN = 0;
        private const int RIGHT_MARGIN = 5;
        private const int BOTTOM_MARGIN = 10;
        private const int LEFT_MARGIN = 8;

        #endregion

        private Logger Logger = Logger.GetLogger<ProfileView>();

        private readonly Point LABEL_BIG = new Point(400, 40);
        private readonly Point LABEL_SMALL = new Point(400, 30);

        private string _currentSortMethod;

        private Texture2D _deletedItemTexture;
        private Texture2D _sortByWorldBossesTexture;
        private Texture2D _sortByTokenTexture;
        private Texture2D _sortByTitleTexture;
        private Texture2D _sortByRaidTexture;
        private Texture2D _sortByFractalTexture;

        private List<KillProofButton> _displayedKillProofs;
        private KillProof _profile;

        public ProfileView(KillProof profile)
        {
            _profile = profile;
            _displayedKillProofs = new List<KillProofButton>();
            _currentSortMethod = Properties.Resources.Everything;
        }

        public Task<bool> DoLoad(IProgress<string> progress)
        {
            return Task.Run(() =>
            {

                _deletedItemTexture = ModuleInstance.ContentsManager.GetTexture("deleted_item.png");
                _sortByWorldBossesTexture = ModuleInstance.ContentsManager.GetTexture("world-bosses.png");
                _sortByTokenTexture = ModuleInstance.ContentsManager.GetTexture("icon_token.png");
                _sortByTitleTexture = ModuleInstance.ContentsManager.GetTexture("icon_title.png");
                _sortByRaidTexture = ModuleInstance.ContentsManager.GetTexture("icon_raid.png");
                _sortByFractalTexture = ModuleInstance.ContentsManager.GetTexture("icon_fractal.png");
                return true;
            });
        }

        public void DoBuild(Panel buildPanel)
        {
            BuildFooter(BuildBody(BuildHeader(buildPanel)));
        }

        public void DoUnload()
        {
            foreach (var c in _displayedKillProofs) c?.Dispose();
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
            var currentAccountName = new Label
            {
                Parent = header,
                Size = LABEL_BIG,
                Location = new Point(LEFT_MARGIN, 100 - BOTTOM_MARGIN),
                ShowShadow = true,
                Font = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size36, ContentService.FontStyle.Regular),
                Text = _profile.AccountName
            };
            var currentAccountLastRefresh = new Label
            {
                Parent = header,
                Size = LABEL_SMALL,
                Location = new Point(LEFT_MARGIN, currentAccountName.Bottom + BOTTOM_MARGIN),
                Text = Properties.Resources.Last_Refresh_ + $" {_profile.LastRefresh:dddd, d. MMMM yyyy - HH:mm:ss}"
        };
            var sortingsMenu = new Panel
            {
                Parent = header,
                Size = new Point(260, 32),
                Location = new Point(header.Right - 310 - RIGHT_MARGIN, currentAccountLastRefresh.Location.Y),
                ShowTint = true
            };
            var bSortByAll = new Image
            {
                Parent = sortingsMenu,
                Size = new Point(32, 32),
                Location = new Point(RIGHT_MARGIN, 0),
                Texture = Content.GetTexture("255369"),
                BackgroundColor = Color.Transparent,
                BasicTooltipText = Properties.Resources.Everything
            };
            bSortByAll.LeftMouseButtonPressed += UpdateSort;
            bSortByAll.LeftMouseButtonPressed += MousePressedSortButton;
            bSortByAll.LeftMouseButtonReleased += MouseLeftSortButton;
            bSortByAll.MouseLeft += MouseLeftSortButton;
            var bSortByKillProof = new Image
            {
                Parent = sortingsMenu,
                Size = new Point(32, 32),
                Location = new Point(bSortByAll.Right + 20 + RIGHT_MARGIN, 0),
                Texture = _sortByWorldBossesTexture,
                BasicTooltipText = Properties.Resources.Progress_Proofs
            };
            bSortByKillProof.LeftMouseButtonPressed += UpdateSort;
            bSortByKillProof.LeftMouseButtonPressed += MousePressedSortButton;
            bSortByKillProof.LeftMouseButtonReleased += MouseLeftSortButton;
            bSortByKillProof.MouseLeft += MouseLeftSortButton;
            var bSortByToken = new Image
            {
                Parent = sortingsMenu,
                Size = new Point(32, 32),
                Location = new Point(bSortByKillProof.Right + RIGHT_MARGIN, 0),
                Texture = _sortByTokenTexture,
                BasicTooltipText = Properties.Resources.Tokens
            };
            bSortByToken.LeftMouseButtonPressed += UpdateSort;
            bSortByToken.LeftMouseButtonPressed += MousePressedSortButton;
            bSortByToken.LeftMouseButtonReleased += MouseLeftSortButton;
            bSortByToken.MouseLeft += MouseLeftSortButton;
            var bSortByTitle = new Image
            {
                Parent = sortingsMenu,
                Size = new Point(32, 32),
                Location = new Point(bSortByToken.Right + 20 + RIGHT_MARGIN, 0),
                Texture = _sortByTitleTexture,
                BasicTooltipText = Properties.Resources.Titles
            };
            bSortByTitle.LeftMouseButtonPressed += UpdateSort;
            bSortByTitle.LeftMouseButtonPressed += MousePressedSortButton;
            bSortByTitle.LeftMouseButtonReleased += MouseLeftSortButton;
            bSortByTitle.MouseLeft += MouseLeftSortButton;
            var bSortByRaid = new Image
            {
                Parent = sortingsMenu,
                Size = new Point(32, 32),
                Location = new Point(bSortByTitle.Right + RIGHT_MARGIN, 0),
                Texture = _sortByRaidTexture,
                BasicTooltipText = Properties.Resources.Raid_Titles
            };
            bSortByRaid.LeftMouseButtonPressed += UpdateSort;
            bSortByRaid.LeftMouseButtonPressed += MousePressedSortButton;
            bSortByRaid.LeftMouseButtonReleased += MouseLeftSortButton;
            bSortByRaid.MouseLeft += MouseLeftSortButton;
            var bSortByFractal = new Image
            {
                Parent = sortingsMenu,
                Size = new Point(32, 32),
                Location = new Point(bSortByRaid.Right + RIGHT_MARGIN, 0),
                Texture = _sortByFractalTexture,
                BasicTooltipText = Properties.Resources.Fractal_Titles
            };
            bSortByFractal.LeftMouseButtonPressed += UpdateSort;
            bSortByFractal.LeftMouseButtonPressed += MousePressedSortButton;
            bSortByFractal.LeftMouseButtonReleased += MouseLeftSortButton;
            bSortByFractal.MouseLeft += MouseLeftSortButton;

            return header;
        }

        private void MousePressedSortButton(object sender, MouseEventArgs e)
        {
            var bSortMethod = (Control)sender;
            bSortMethod.Size = new Point(bSortMethod.Size.X - 4, bSortMethod.Size.Y - 4);
        }

        private void MouseLeftSortButton(object sender, MouseEventArgs e)
        {
            var bSortMethod = (Control)sender;
            bSortMethod.Size = new Point(32, 32);
        }

        private void UpdateSort(object sender, EventArgs e)
        {
            if (sender != null) _currentSortMethod = ((Control)sender).BasicTooltipText;
            if (_currentSortMethod.Equals(Properties.Resources.Everything, StringComparison.InvariantCultureIgnoreCase))
            {
                _displayedKillProofs.Sort((e1, e2) =>
                {
                    var result = e1.IsTitleDisplay.CompareTo(e2.IsTitleDisplay);
                    if (result != 0) return result;
                    return string.Compare(e1.BottomText, e2.BottomText,
                        StringComparison.InvariantCultureIgnoreCase);
                });
                foreach (var e1 in _displayedKillProofs) e1.Visible = true;
            }
            else if (_currentSortMethod.Equals(Properties.Resources.Progress_Proofs, StringComparison.InvariantCultureIgnoreCase))
            {
                _displayedKillProofs.Sort((e1, e2) =>
                    string.Compare(e1.BottomText, e2.BottomText, StringComparison.InvariantCultureIgnoreCase));
                foreach (var e1 in _displayedKillProofs)
                    e1.Visible = _profile.Killproofs != null &&
                                 _profile.Killproofs.Any(x => x.Name.Equals(e1.Text, StringComparison.InvariantCultureIgnoreCase));
            }
            else if (_currentSortMethod.Equals(Properties.Resources.Tokens, StringComparison.InvariantCultureIgnoreCase))
            {
                _displayedKillProofs.Sort((e1, e2) =>
                    string.Compare(e1.BottomText, e2.BottomText, StringComparison.InvariantCultureIgnoreCase));
                foreach (var e1 in _displayedKillProofs)
                    e1.Visible = _profile.Tokens != null &&
                                 _profile.Tokens.Any(x => x.Name.Equals(e1.Text, StringComparison.InvariantCultureIgnoreCase));
            }
            else if (_currentSortMethod.Equals(Properties.Resources.Titles, StringComparison.InvariantCultureIgnoreCase))
            {
                _displayedKillProofs.Sort((e1, e2) =>
                    string.Compare(e1.BottomText, e2.BottomText, StringComparison.InvariantCultureIgnoreCase));
                foreach (var e1 in _displayedKillProofs) e1.Visible = e1.IsTitleDisplay;
            }
            else if (_currentSortMethod.Equals(Properties.Resources.Fractal_Titles, StringComparison.InvariantCultureIgnoreCase))
            {
                _displayedKillProofs.Sort((e1, e2) =>
                    string.Compare(e1.Text, e2.Text, StringComparison.InvariantCultureIgnoreCase));
                foreach (var e1 in _displayedKillProofs) e1.Visible = e1.BottomText.ToLower().Contains("fractal");
            }
            else if (_currentSortMethod.Equals(Properties.Resources.Raid_Titles, StringComparison.InvariantCultureIgnoreCase))
            {
                _displayedKillProofs.Sort((e1, e2) =>
                    string.Compare(e1.Text, e2.Text, StringComparison.InvariantCultureIgnoreCase));
                foreach (var e1 in _displayedKillProofs) e1.Visible = e1.BottomText.ToLower().Contains("raid");
            }
            RepositionKillProofs();
        }

        private Panel BuildBody(Panel header) 
        {
            var contentPanel = new Panel
            {
                Parent = header.Parent,
                Size = new Point(header.Size.X, header.Parent.Height - header.Height - 100),
                Location = new Point(0, header.Bottom),
                ShowBorder = true,
                CanScroll = true,
                ShowTint = true
            };

            if (_profile.Killproofs != null)
            {
                foreach (var killproof in _profile.Killproofs)
                {
                    var killProofButton = new KillProofButton
                    {
                        Parent = contentPanel,
                        Icon = ModuleInstance.GetTokenRender(killproof.Id),
                        Font = Content.GetFont(ContentService.FontFace.Menomonia,
                            ContentService.FontSize.Size16, ContentService.FontStyle.Regular),
                        Text = killproof.Name,
                        BottomText = killproof.Amount.ToString()
                    };
                    _displayedKillProofs.Add(killProofButton);
                }
            }
            else
            {
                // TODO: Show button indicating that killproofs were explicitly hidden
                Logger.Info($"PlayerProfile '{_profile.AccountName}' has LI details explicitly hidden.");
            }

            if (_profile.Tokens != null)
            {
                foreach (var token in _profile.Tokens)
                {
                    var killProofButton = new KillProofButton
                    {
                        Parent = contentPanel,
                        Icon = ModuleInstance.GetTokenRender(token.Id),
                        Font = Content.GetFont(ContentService.FontFace.Menomonia,
                            ContentService.FontSize.Size16, ContentService.FontStyle.Regular),
                        Text = token.Name,
                        BottomText = token.Amount.ToString()
                    };

                    _displayedKillProofs.Add(killProofButton);
                }
            }
            else
            {
                // TODO: Show button indicating that tokens were explicitly hidden
                Logger.Info($"PlayerProfile '{_profile.AccountName}' has tokens explicitly hidden.");
            }

            if (_profile.Titles != null) {
                foreach (var title in _profile.Titles)
                {
                    var titleButton = new KillProofButton
                    {
                        Parent = contentPanel,
                        Font = Content.DefaultFont16,
                        Text = title.Name,
                        BottomText = title.Mode.ToString(),
                        IsTitleDisplay = true
                    };

                    switch (title.Mode)
                    {
                        case Mode.Raid:
                            titleButton.Icon = _sortByRaidTexture;
                            break;
                        case Mode.Fractal:
                            titleButton.Icon = _sortByFractalTexture;
                            break;
                    }

                    _displayedKillProofs.Add(titleButton);
                }
            } 
            else
            {
                // TODO: Show text indicating that titles were explicitly hidden
                Logger.Info($"PlayerProfile '{_profile.AccountName}' has titles and achievements explicitly hidden.");
            }

            RepositionKillProofs();

            var backButton = new BackButton(Overlay.BlishHudWindow)
            {
                Text = ModuleInstance.KillProofTabName,
                NavTitle = Properties.Resources.Profile,
                Parent = contentPanel.Parent,
                Location = new Point(20, 20)
            };
            backButton.LeftMouseButtonReleased += delegate
            {
                Overlay.BlishHudWindow.NavigateBack();
            };
            return contentPanel;
        }

        private void BuildFooter(Panel body)
        {
            var currentAccountKpId = new Label
            {
                Parent = body.Parent,
                Size = LABEL_SMALL,
                HorizontalAlignment = HorizontalAlignment.Left,
                Location = new Point(LEFT_MARGIN, body.Bottom + BOTTOM_MARGIN),
                Font = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size11, ContentService.FontStyle.Regular),
                Text = Properties.Resources.ID_ + ' ' + _profile.KpId
            };
            var currentAccountProofUrl = new Label
            {
                Parent = body.Parent,
                Size = LABEL_SMALL,
                HorizontalAlignment = HorizontalAlignment.Left,
                Location = new Point(LEFT_MARGIN, currentAccountKpId.Location.Y + BOTTOM_MARGIN + 2),
                Font = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size11, ContentService.FontStyle.Regular),
                Text = _profile.ProofUrl
            };

            var creditLabel = new Label
            {
                Parent = body.Parent,
                Size = LABEL_SMALL,
                HorizontalAlignment = HorizontalAlignment.Center,
                Location = new Point(body.Width / 2 - LABEL_SMALL.X / 2, body.Bottom + BOTTOM_MARGIN),
                StrokeText = true,
                ShowShadow = true,
                Text = Properties.Resources.Powered_by_www_killproof_me
            };

            if (Uri.IsWellFormedUriString(currentAccountProofUrl.Text, UriKind.Absolute))
            {
                currentAccountProofUrl.MouseEntered += (o, e) => currentAccountProofUrl.TextColor = Color.LightBlue;
                currentAccountProofUrl.MouseLeft += (o, e) => currentAccountProofUrl.TextColor = Color.White;
                currentAccountProofUrl.LeftMouseButtonPressed += (o, e) => currentAccountProofUrl.TextColor = new Color(206, 174, 250);
                currentAccountProofUrl.LeftMouseButtonReleased += (o, e) => {
                    currentAccountProofUrl.TextColor = Color.LightBlue;
                    System.Diagnostics.Process.Start(currentAccountProofUrl.Text);
                };
            }
        }

        private void RepositionKillProofs()
        {
            var pos = 0;
            foreach (var e in _displayedKillProofs)
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
