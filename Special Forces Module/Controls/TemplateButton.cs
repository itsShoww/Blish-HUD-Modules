using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nekres.Special_Forces_Module.Persistance;

namespace Nekres.Special_Forces_Module.Controls
{
    // TODO: Show "Edit" button when music sheet creator correlates to account name from ApiService. Navigates to composer.
    internal class TemplateButton : DetailsButton
    {
        private const int SHEETBUTTON_WIDTH = 327;
        private const int SHEETBUTTON_HEIGHT = 100;

        private const int USER_WIDTH = 75;
        private const int BOTTOMSECTION_HEIGHT = 35;
        private readonly Texture2D BackgroundSprite;
        private readonly Texture2D ClipboardSprite;
        private readonly Texture2D DividerSprite;
        private readonly Texture2D GlowClipboardSprite;
        private readonly Texture2D GlowPlaySprite;
        private readonly Texture2D GlowUtilitySprite;
        private readonly Texture2D IconBoxSprite;
        private readonly Texture2D PlaySprite;
        private readonly Texture2D UtilitySprite;

        private bool _mouseOverPlay;

        private bool _mouseOverTemplate;

        private bool _mouseOverUtility1;

        private bool _mouseOverUtility2;

        private bool _mouseOverUtility3;

        private RawTemplate _template;

        internal TemplateButton(RawTemplate template)
        {
            if (template == null) return;
            Template = template;
            if (Template.Utilitykeys == null) Template.Utilitykeys = new int[3] {1, 2, 3};

            UtilitySprite = UtilitySprite ??
                            SpecialForcesModule.ModuleInstance.ContentsManager.GetTexture("skill_frame.png");
            GlowUtilitySprite = GlowUtilitySprite ??
                                SpecialForcesModule.ModuleInstance.ContentsManager.GetTexture("skill_frame.png");
            ClipboardSprite = ClipboardSprite ??
                              SpecialForcesModule.ModuleInstance.ContentsManager.GetTexture("clipboard.png");
            GlowClipboardSprite = GlowClipboardSprite ??
                                  SpecialForcesModule.ModuleInstance.ContentsManager.GetTexture("glow_clipboard.png");
            PlaySprite = PlaySprite ?? SpecialForcesModule.ModuleInstance.ContentsManager.GetTexture("play.png");
            GlowPlaySprite = GlowPlaySprite ??
                             SpecialForcesModule.ModuleInstance.ContentsManager.GetTexture("glow_play.png");
            BackgroundSprite = BackgroundSprite ?? ContentService.Textures.Pixel;
            DividerSprite = DividerSprite ?? GameService.Content.GetTexture("157218");
            IconBoxSprite = IconBoxSprite ?? GameService.Content.GetTexture("controls/detailsbutton/605003");
            MouseMoved += TemplateButton_MouseMoved;
            MouseLeft += TemplateButton_MouseLeft;
            LeftMouseButtonPressed += TemplateButton_LeftMouseButtonClicked;
            Size = new Point(SHEETBUTTON_WIDTH, SHEETBUTTON_HEIGHT);
        }

        internal string BottomText { get; set; }

        internal RawTemplate Template
        {
            get => _template;
            set
            {
                if (_template == value) return;

                _template = value;
                OnPropertyChanged();
            }
        }

        internal bool MouseOverPlay
        {
            get => _mouseOverPlay;
            set
            {
                if (_mouseOverPlay == value) return;
                _mouseOverPlay = value;
                Invalidate();
            }
        }

        internal bool MouseOverTemplate
        {
            get => _mouseOverTemplate;
            set
            {
                if (_mouseOverTemplate == value) return;
                _mouseOverTemplate = value;
                Invalidate();
            }
        }

        internal bool MouseOverUtility1
        {
            get => _mouseOverUtility1;
            set
            {
                if (_mouseOverUtility1 == value) return;
                _mouseOverUtility1 = value;
                Invalidate();
            }
        }

        internal bool MouseOverUtility2
        {
            get => _mouseOverUtility2;
            set
            {
                if (_mouseOverUtility2 == value) return;
                _mouseOverUtility2 = value;
                Invalidate();
            }
        }

        internal bool MouseOverUtility3
        {
            get => _mouseOverUtility3;
            set
            {
                if (_mouseOverUtility3 == value) return;
                _mouseOverUtility3 = value;
                Invalidate();
            }
        }

        private void TemplateButton_LeftMouseButtonClicked(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            //TODO: Implement ClipboardUtil
            if (MouseOverTemplate)
                ScreenNotification.ShowNotification("Not yet implemented!");
            else if (MouseOverPlay) GameService.Overlay.BlishHudWindow.Hide();
        }

        private void TemplateButton_MouseLeft(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            MouseOverPlay = false;
            MouseOverTemplate = false;
            MouseOverUtility1 = false;
            MouseOverUtility2 = false;
            MouseOverUtility3 = false;
        }

        private void TemplateButton_MouseMoved(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            var relPos = e.MouseState.Position - AbsoluteBounds.Location;

            if (MouseOver && relPos.Y > Height - BOTTOMSECTION_HEIGHT)
            {
                MouseOverPlay = relPos.X < SHEETBUTTON_WIDTH - 36 + 32 && relPos.X > SHEETBUTTON_WIDTH - 36;
                MouseOverTemplate = relPos.X < SHEETBUTTON_WIDTH - 73 + 32 && relPos.X > SHEETBUTTON_WIDTH - 73;
                MouseOverUtility3 = relPos.X < SHEETBUTTON_WIDTH - 109 + 32 && relPos.X > SHEETBUTTON_WIDTH - 109;
                MouseOverUtility2 = relPos.X < SHEETBUTTON_WIDTH - 145 + 32 && relPos.X > SHEETBUTTON_WIDTH - 145;
                MouseOverUtility1 = relPos.X < SHEETBUTTON_WIDTH - 181 + 32 && relPos.X > SHEETBUTTON_WIDTH - 181;
            }
            else
            {
                MouseOverPlay = false;
                MouseOverTemplate = false;
                MouseOverUtility1 = false;
                MouseOverUtility2 = false;
                MouseOverUtility3 = false;
            }

            if (MouseOverPlay)
                BasicTooltipText = "Practice!";
            else if (MouseOverTemplate)
                BasicTooltipText = "Copy Template";
            else if (MouseOverUtility1)
                BasicTooltipText = "Assign Utility Key 1";
            else if (MouseOverUtility2)
                BasicTooltipText = "Assign Utility Key 2";
            else if (MouseOverUtility3)
                BasicTooltipText = "Assign Utility Key 3";
            else
                BasicTooltipText = Title;
        }

        protected override CaptureType CapturesInput()
        {
            return CaptureType.Mouse | CaptureType.Filter;
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            var iconSize = IconSize == DetailsIconSize.Large
                ? SHEETBUTTON_HEIGHT
                : SHEETBUTTON_HEIGHT - BOTTOMSECTION_HEIGHT;

            // Draw background
            spriteBatch.DrawOnCtrl(this, BackgroundSprite, bounds, Color.Black * 0.25f);

            // Draw bottom section (overlap to make background darker here)
            spriteBatch.DrawOnCtrl(this, BackgroundSprite,
                new Rectangle(0, bounds.Height - BOTTOMSECTION_HEIGHT, bounds.Width - BOTTOMSECTION_HEIGHT,
                    BOTTOMSECTION_HEIGHT), Color.Black * 0.1f);

            // Draw icons

            #region Icons

            if (MouseOverPlay)
                spriteBatch.DrawOnCtrl(this, GlowPlaySprite,
                    new Rectangle(SHEETBUTTON_WIDTH - 36, bounds.Height - BOTTOMSECTION_HEIGHT + 1, 32, 32),
                    Color.White);
            else
                spriteBatch.DrawOnCtrl(this, PlaySprite,
                    new Rectangle(SHEETBUTTON_WIDTH - 36, bounds.Height - BOTTOMSECTION_HEIGHT + 1, 32, 32),
                    Color.White);

            if (MouseOverTemplate)
                spriteBatch.DrawOnCtrl(this, GlowClipboardSprite,
                    new Rectangle(SHEETBUTTON_WIDTH - 73, bounds.Height - BOTTOMSECTION_HEIGHT + 1, 32, 32),
                    Color.White);
            else
                spriteBatch.DrawOnCtrl(this, ClipboardSprite,
                    new Rectangle(SHEETBUTTON_WIDTH - 73, bounds.Height - BOTTOMSECTION_HEIGHT + 1, 32, 32),
                    Color.White);

            if (MouseOverUtility3)
                spriteBatch.DrawOnCtrl(this, GlowUtilitySprite,
                    new Rectangle(SHEETBUTTON_WIDTH - 109, bounds.Height - BOTTOMSECTION_HEIGHT + 1, 32, 32),
                    Color.White);
            else
                spriteBatch.DrawOnCtrl(this, UtilitySprite,
                    new Rectangle(SHEETBUTTON_WIDTH - 109, bounds.Height - BOTTOMSECTION_HEIGHT + 1, 32, 32),
                    Color.White);

            spriteBatch.DrawStringOnCtrl(this, Template.Utilitykeys[2] + "", Content.DefaultFont14,
                new Rectangle(SHEETBUTTON_WIDTH - 109, bounds.Height - BOTTOMSECTION_HEIGHT + 1, 32, 32), Color.White,
                false, true, 2, Blish_HUD.Controls.HorizontalAlignment.Center, VerticalAlignment.Bottom);

            if (MouseOverUtility2)
                spriteBatch.DrawOnCtrl(this, GlowUtilitySprite,
                    new Rectangle(SHEETBUTTON_WIDTH - 145, bounds.Height - BOTTOMSECTION_HEIGHT + 1, 32, 32),
                    Color.White);
            else
                spriteBatch.DrawOnCtrl(this, UtilitySprite,
                    new Rectangle(SHEETBUTTON_WIDTH - 145, bounds.Height - BOTTOMSECTION_HEIGHT + 1, 32, 32),
                    Color.White);

            spriteBatch.DrawStringOnCtrl(this, Template.Utilitykeys[1] + "", Content.DefaultFont14,
                new Rectangle(SHEETBUTTON_WIDTH - 145, bounds.Height - BOTTOMSECTION_HEIGHT + 1, 32, 32), Color.White,
                false, true, 2, Blish_HUD.Controls.HorizontalAlignment.Center, VerticalAlignment.Bottom);

            if (MouseOverUtility1)
                spriteBatch.DrawOnCtrl(this, GlowUtilitySprite,
                    new Rectangle(SHEETBUTTON_WIDTH - 181, bounds.Height - BOTTOMSECTION_HEIGHT + 1, 32, 32),
                    Color.White);
            else
                spriteBatch.DrawOnCtrl(this, UtilitySprite,
                    new Rectangle(SHEETBUTTON_WIDTH - 181, bounds.Height - BOTTOMSECTION_HEIGHT + 1, 32, 32),
                    Color.White);

            spriteBatch.DrawStringOnCtrl(this, Template.Utilitykeys[0] + "", Content.DefaultFont14,
                new Rectangle(SHEETBUTTON_WIDTH - 181, bounds.Height - BOTTOMSECTION_HEIGHT + 1, 32, 32), Color.White,
                false, true, 2, Blish_HUD.Controls.HorizontalAlignment.Center, VerticalAlignment.Bottom);

            #endregion

            // Draw bottom section seperator
            spriteBatch.DrawOnCtrl(this, DividerSprite, new Rectangle(0, bounds.Height - 40, bounds.Width, 8),
                Color.White);

            // Draw icon
            if (Icon != null)
            {
                spriteBatch.DrawOnCtrl(this, Icon,
                    new Rectangle((bounds.Height - BOTTOMSECTION_HEIGHT) / 2 - 32, (bounds.Height - 35) / 2 - 32, 64,
                        64), Color.White);
                // Draw icon box
                spriteBatch.DrawOnCtrl(this, IconBoxSprite, new Rectangle(0, 0, iconSize, iconSize), Color.White);
            }

            // Wrap text
            var text = Text;
            var wrappedText =
                DrawUtil.WrapText(Content.DefaultFont14, text, SHEETBUTTON_WIDTH - 40 - iconSize - 20);
            spriteBatch.DrawStringOnCtrl(this, wrappedText, Content.DefaultFont14,
                new Rectangle(89, 0, 216, Height - BOTTOMSECTION_HEIGHT), Color.White, false, true, 2);

            // Draw the profession;
            spriteBatch.DrawStringOnCtrl(this, BottomText, Content.DefaultFont14,
                new Rectangle(5, bounds.Height - BOTTOMSECTION_HEIGHT, USER_WIDTH, 35), Color.White, false, false, 0);
        }
    }
}