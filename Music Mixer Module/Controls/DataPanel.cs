using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Runtime.InteropServices;
using static Blish_HUD.GameService;
using static Nekres.Music_Mixer.Gw2StateService;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Nekres.Music_Mixer.Controls
{
    internal class DataPanel : Container
    {
        public State CurrentState;

        private BitmapFont _font;
        private const int _leftMargin = 10;
        private const int _rightMargin = 10;
        private const int _topMargin = 20;
        private const int _strokeDist = 1;

        public DataPanel() {
            _font = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size36, ContentService.FontStyle.Regular);

            CurrentState = 0;
            UpdateLocation(null, null);
            Graphics.SpriteScreen.Resized += UpdateLocation;
        }

        protected override CaptureType CapturesInput() => CaptureType.ForceNone;

        private void UpdateLocation(object sender, EventArgs e) => Location = new Point(0, 0);

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds) {
            if (!GameIntegration.Gw2IsRunning) return;

            var left = HorizontalAlignment.Left;
            var top = VerticalAlignment.Top;


            string text;
            int height;
            int width;
            Rectangle rect;

            var calcTopMargin = _topMargin;
            var calcLeftMargin = _leftMargin;
            
            text = $"State: " + CurrentState.ToString();
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.Yellow, false, true, _strokeDist, left, top);
        }
    }
}
