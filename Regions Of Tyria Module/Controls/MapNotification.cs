using Blish_HUD;
using Blish_HUD.Controls;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using static Blish_HUD.GameService;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Nekres.Regions_Of_Tyria.Controls
{
    internal class MapNotification : Container
    {
        private static BitmapFont _smallFont;
        private static BitmapFont _mediumFont;
        private const int _topMargin = 20;
        private const int _strokeDist = 1;
        private const int _underlineSize = 1;
        private static Color _brightGold;

        private static readonly SynchronizedCollection<MapNotification> _activeMapNotifications;

        static MapNotification() {
            _activeMapNotifications = new SynchronizedCollection<MapNotification>();

            _smallFont = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size24, ContentService.FontStyle.Regular);
            _mediumFont = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size36, ContentService.FontStyle.Regular);
            _brightGold = new Color(223, 194, 149, 255);
        }

        #region Public Fields

        private string _header;
        public string Header {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        private string _footer;
        public string Footer {
            get => _footer;
            set => SetProperty(ref _footer, value);
        }

        private float _showDuration;
        public float ShowDuration {
            get => _showDuration;
            set => SetProperty(ref _showDuration, value);
        }


        private float _fadeInDuration;
        public float FadeInDuration {
            get => _fadeInDuration;
            set => SetProperty(ref _fadeInDuration, value);
        }

        private float _fadeOutDuration;
        public float FadeOutDuration {
            get => _fadeOutDuration;
            set => SetProperty(ref _fadeOutDuration, value);
        }

        #endregion

        private Glide.Tween _animFadeLifecycle;
        private int _targetTop = 0;

        private MapNotification(string header, string footer, Texture2D icon = null, float showDurationMs = 4000, float fadeInDurationMs = 2000, float fadeOutDurationMs = 2000) {
            _header = header;
            _footer = footer;
            _showDuration = showDurationMs;
            _fadeInDuration = fadeInDurationMs;
            _fadeOutDuration = fadeOutDurationMs;

            this.Opacity = 0f;
            this.Size = new Point(500, 500);
            this.ZIndex = Screen.TOOLTIP_BASEZINDEX;
            this.Location = new Point(Graphics.SpriteScreen.Width / 2 - this.Size.X / 2, Graphics.SpriteScreen.Height / 4 - this.Size.Y / 4);

            _targetTop = this.Top;

            this.Resized += UpdateLocation;
        }

        public void UpdateLocation(object o, ResizedEventArgs e) => this.Location = new Point(Graphics.SpriteScreen.Width / 2 - this.Size.X / 2, Graphics.SpriteScreen.Height / 4 - this.Size.Y / 2);

        protected override CaptureType CapturesInput() => CaptureType.ForceNone;

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

        /// <inheritdoc />
        public override void Show() {
            _animFadeLifecycle = Animation.Tweener
                                          .Tween(this, new { Opacity = 1f }, this.FadeInDuration)
                                          .OnComplete(() => {
                                            _animFadeLifecycle = Animation.Tweener.Tween(this, new { Opacity = 1f }, this.ShowDuration)
                                                                                  .OnComplete(() => {
                                                                                    _animFadeLifecycle = Animation.Tweener.Tween(this, new { Opacity = 0f }, this.FadeOutDuration)
                                                                                                                          .OnComplete(Dispose);
                                                                                  });
                                          });



            base.Show();
        }

        private void SlideDown(int distance) {
            _targetTop += distance;

            Animation.Tweener.Tween(this, new {Top = _targetTop}, this.FadeOutDuration);

            if (_opacity < 1f) return;

            _animFadeLifecycle = Animation.Tweener
                                          .Tween(this, new {Opacity = 0f}, this.FadeOutDuration)
                                          .OnComplete(Dispose);
        }

        /// <inheritdoc />
        protected override void DisposeControl() {
            _activeMapNotifications.Remove(this);

            base.DisposeControl();
        }

        public static void ShowNotification(string header, string footer, Texture2D icon = null, float showDurationMs = 4000, float fadeInDurationMs = 2000, float fadeOutDurationMs = 2000) {
            var nNot = new MapNotification(header, footer, icon, showDurationMs, fadeInDurationMs, fadeOutDurationMs) {
                Parent = Graphics.SpriteScreen
            };

            nNot.ZIndex = _activeMapNotifications.DefaultIfEmpty(nNot).Max(n => n.ZIndex) + 1;

            foreach (var activeScreenNotification in _activeMapNotifications) {
                activeScreenNotification.SlideDown((int)(_smallFont.LineHeight + _mediumFont.LineHeight + _topMargin * 1.05f));
            }

            _activeMapNotifications.Add(nNot);

            nNot.Show();
        }
    }
}
