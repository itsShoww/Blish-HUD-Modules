using Blish_HUD;
using Blish_HUD.ArcDps.Common;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using Nekres.Kill_Proof_Module.Controls.Views;
using Nekres.Kill_Proof_Module.Models;
using static Nekres.Kill_Proof_Module.KillProofModule;

namespace Nekres.Kill_Proof_Module.Controls
{
    public class PlayerButton : DetailsButton
    {
        private const int DEFAULT_WIDTH = 327;
        private const int DEFAULT_HEIGHT = 100;
        private const int DEFAULT_BOTTOMSECTION_HEIGHT = 35;
        private readonly Texture2D BORDER_SPRITE;

        private readonly Texture2D PIXEL;
        private readonly Texture2D SEPARATOR;

        private BitmapFont _font;
        public BitmapFont Font
        {
            get => _font;
            set
            {
                if (_font == value) return;
                _font = value;
                OnPropertyChanged();
            }
        }

        private PlayerProfile _playerProfile;
        public PlayerProfile PlayerProfile
        {
            get => _playerProfile;
            private set
            {
                _playerProfile = value;
                OnPropertyChanged();
            }
        }

        private bool _isNew;
        public bool IsNew
        {
            get => _isNew;
            set
            {
                if (_isNew == value) return;
                _isNew = value;
                OnPropertyChanged();
            }
        }

        public PlayerButton(PlayerProfile playerProfile)
        {
            PlayerProfile = playerProfile;
            BORDER_SPRITE = BORDER_SPRITE ?? Content.GetTexture(@"controls/detailsbutton/605003");
            SEPARATOR = SEPARATOR ?? Content.GetTexture("157218");
            PIXEL = PIXEL ?? ContentService.Textures.Pixel;

            Size = new Point(DEFAULT_WIDTH, DEFAULT_HEIGHT);

            Icon = ModuleInstance.GetProfessionRender(playerProfile.Player);
            Font = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size16, ContentService.FontStyle.Regular);

            playerProfile.PlayerChanged += OnPlayerChanged;
            Click += OnClick;
        }

        private void OnClick(object o, MouseEventArgs e)
        {
            IsNew = false;
            MainView.LoadProfileView(PlayerProfile.AccountName);
        }

        private void OnPlayerChanged(object o, ValueEventArgs<CommonFields.Player> e)
        {
            Icon = ModuleInstance.GetProfessionRender(e.Value);
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            // Draw background
            spriteBatch.DrawOnCtrl(this, PIXEL, bounds, Color.Black * 0.25f);

            // Draw bottom section
            spriteBatch.DrawOnCtrl(this, PIXEL, ContentRegion, Color.Black * 0.1f);

            var iconSize = IconSize == DetailsIconSize.Large
                ? DEFAULT_HEIGHT
                : DEFAULT_HEIGHT - DEFAULT_BOTTOMSECTION_HEIGHT;

            // Draw bottom text
            spriteBatch.DrawStringOnCtrl(this, PlayerProfile.AccountName, Content.DefaultFont14,
                new Rectangle(iconSize + 20, iconSize - DEFAULT_BOTTOMSECTION_HEIGHT, DEFAULT_WIDTH - 40,
                    DEFAULT_BOTTOMSECTION_HEIGHT), Color.White, false, true, 2);

            if (Icon != null && Icon.HasTexture)
            {
                // Draw icon
                spriteBatch.DrawOnCtrl(this, Icon,
                    new Rectangle(iconSize / 2 - 64 / 2 + (IconSize == DetailsIconSize.Small ? 10 : 0),
                        iconSize / 2 - 64 / 2, 64, 64), Color.White);

                // Draw icon box
                if (IconSize == DetailsIconSize.Large)
                    spriteBatch.DrawOnCtrl(this, BORDER_SPRITE, new Rectangle(0, 0, iconSize, iconSize), Color.White);
            }

            // Draw bottom section seperator
            spriteBatch.DrawOnCtrl(this, SEPARATOR, new Rectangle(ContentRegion.X, bounds.Height - 40, bounds.Width, 8),
                Color.White);

            // Wrap text
            if (PlayerProfile.CharacterName != null && Font != null)
            {
                var wrappedText = DrawUtil.WrapText(Font, PlayerProfile.CharacterName, DEFAULT_WIDTH - 40 - iconSize - 20);

                // Draw name
                spriteBatch.DrawStringOnCtrl(this, wrappedText, Font,
                    new Rectangle(iconSize + 20, 0, DEFAULT_WIDTH - iconSize - 20,
                        Height - DEFAULT_BOTTOMSECTION_HEIGHT), Color.White, false, true, 2);
            }

            if (IsNew)
                spriteBatch.DrawStringOnCtrl(this, Properties.Resources.New, Content.DefaultFont14,
                    new Rectangle(iconSize + 18, 2, DEFAULT_WIDTH - iconSize - 20,
                        Height - DEFAULT_BOTTOMSECTION_HEIGHT), Color.Gold, false, true, 2, HorizontalAlignment.Right,
                    VerticalAlignment.Top);
        }
    }
}