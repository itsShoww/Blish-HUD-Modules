using System.Drawing;
using Blish_HUD;
using Blish_HUD.ArcDps.Common;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Screenshot_Manager_Module.Controls
{
    public class ScreenshotNotification : Panel
    {

        private const int HEADING_HEIGHT = 20;

        private readonly AsyncTexture2D _thumbnail;

        private static int _visibleNotifications = 0;

        private const int PanelMargin = 10;

        private readonly Point _thumbnailSize;
        private readonly Texture2D _inspectIcon;

        private ScreenshotNotification(AsyncTexture2D thumbnail, string message) {
            _thumbnail = thumbnail;
            _thumbnailSize = ScreenshotManagerModule.ModuleInstance.GetThumbnailSize(_thumbnail);
            _inspectIcon = ScreenshotManagerModule.ModuleInstance._inspectIcon;

            Opacity = 0f;

            Size = new Point(_thumbnailSize.X + PanelMargin, _thumbnailSize.Y + HEADING_HEIGHT + PanelMargin);
            
            Location = new Point(60, 60 + (Size.Y + 15) * _visibleNotifications);

            ShowBorder = true;
            ShowTint = true;

            var borderPanel = new Panel
            {
                Parent = this,
                Size = new Point(this.Size.X, _thumbnailSize.Y + PanelMargin),
                Location = new Point(0, HEADING_HEIGHT),
                BackgroundColor = Color.Black,
                ShowTint = true,
                ShowBorder = true
            };
            var messageLbl = new Label {
                Parent = this,
                Location = new Point(0,2),
                Size = this.Size,
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size14, ContentService.FontStyle.Regular),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = message,
            };
            _visibleNotifications++;
            this.Click += delegate
            {
                ScreenshotManagerModule.ModuleInstance.CreateInspectionPanel(_thumbnail);
                GameService.Overlay.BlishHudWindow.Show();
                GameService.Overlay.BlishHudWindow.Navigate(ScreenshotManagerModule.ModuleInstance.modulePanel);
                this.Dispose();
            };
        }

        protected override CaptureType CapturesInput() {
            return CaptureType.Mouse;
        }

        private Rectangle _layoutThumbnailBounds;
        private Rectangle _layoutInspectIconBounds;

        /// <inheritdoc />
        public override void RecalculateLayout()
        {
            _layoutThumbnailBounds = new Rectangle(PanelMargin / 2,HEADING_HEIGHT + (PanelMargin / 2), _thumbnailSize.X, _thumbnailSize.Y);
            _layoutInspectIconBounds = new Rectangle((Size.X / 2) - 32, (Size.Y / 2) - 32 + HEADING_HEIGHT, 64, 64);
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds) {
            spriteBatch.DrawOnCtrl(this,
                                   ScreenshotManagerModule.ModuleInstance._notificationBackroundTexture,
                                   bounds,
                                   Color.White * 0.85f);
        }
        public override void PaintAfterChildren(SpriteBatch spriteBatch, Rectangle bound)
        {
            spriteBatch.DrawOnCtrl(this,
                _thumbnail,
                _layoutThumbnailBounds);

            spriteBatch.DrawOnCtrl(this,
                _inspectIcon,
                _layoutInspectIconBounds);
        }
        private void Show(float duration) {
            Content.PlaySoundEffectByName(@"audio/color-change");

            Animation.Tweener
                     .Tween(this, new { Opacity = 1f }, 0.2f)
                     .Repeat(1)
                     .RepeatDelay(duration)
                     .Reflect()
                     .OnComplete(Dispose);
        }

        public static void ShowNotification(AsyncTexture2D icon, string message, float duration) {
            var notif = new ScreenshotNotification(icon, message) {
                Parent = Graphics.SpriteScreen
            };

            notif.Show(duration);
        }

        protected override void DisposeControl() {
            _visibleNotifications--;

            base.DisposeControl();
        }

    }
}
