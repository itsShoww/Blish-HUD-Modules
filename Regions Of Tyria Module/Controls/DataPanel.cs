using Blish_HUD;
using Blish_HUD.Controls;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using static Blish_HUD.GameService;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Nekres.Regions_Of_Tyria.Controls
{
    internal class DataPanel : Container
    {
         
        public string Header;
        public string Footer;

        private BitmapFont _smallFont;
        private BitmapFont _mediumFont;
        private const int _topMargin = 20;
        private const int _strokeDist = 1;
        private const int _underlineSize = 1;
        private Color _brightGold = new Color(223, 194, 149, 255);

        public DataPanel() {
            _smallFont = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size24, ContentService.FontStyle.Regular);
            _mediumFont = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size36, ContentService.FontStyle.Regular);

            UpdateLocation(null, null);
            Graphics.SpriteScreen.Resized += UpdateLocation;
        }

        public void Fade(float to, TimeSpan duration, bool dispose = false) {
            var fade = Animation.Tweener.Tween(this, new { Opacity = to }, (float)duration.TotalSeconds);
            if (!dispose) return;
            fade.OnComplete(() => {
                this?.Dispose();
            });
        }

        protected override CaptureType CapturesInput() => CaptureType.ForceNone;

        private void UpdateLocation(object sender, EventArgs e) => Location = new Point(0, 0);

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds) {
            if (!GameIntegration.Gw2IsRunning || Header == null || Footer == null) return;

            var center = HorizontalAlignment.Center;
            var top = VerticalAlignment.Top;

            string text;
            int height;
            int width;
            Rectangle rect;

            text = Header;
            width = (int)_smallFont.MeasureString(text).Width;
            height = (int)_smallFont.MeasureString(text).Height;

            rect = new Rectangle(0, 0, bounds.Width, bounds.Height);
            spriteBatch.DrawStringOnCtrl(this, text, _smallFont, rect, _brightGold, false, true, _strokeDist, center, top);

            rect = new Rectangle((Size.X / 2) - (width / 2) - 1, rect.Y + height + 2, width + 2, _underlineSize + 2);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, rect, new Color(0,0,0,200));
            rect = new Rectangle(rect.X + 1, rect.Y + 1, width, _underlineSize);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, rect, _brightGold);

            text = Footer;
            height = (int)_smallFont.MeasureString(text).Height;
            rect = new Rectangle(0, _topMargin + height, bounds.Width, bounds.Height);
            spriteBatch.DrawStringOnCtrl(this, text, _mediumFont, rect, _brightGold, false, true, _strokeDist, center, top);
        }
    }
}
