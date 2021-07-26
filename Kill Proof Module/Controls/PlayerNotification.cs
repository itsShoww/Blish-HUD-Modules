using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nekres.Kill_Proof_Module.Controls.Views;
using Nekres.Kill_Proof_Module.Models;
using static Blish_HUD.GameService;
using static Nekres.Kill_Proof_Module.KillProofModule;

namespace Nekres.Kill_Proof_Module.Controls
{
    public class PlayerNotification : Container
    {
        private const int NOTIFICATION_WIDTH = 264;
        private const int NOTIFICATION_HEIGHT = 64;

        private static int _visibleNotifications;
        private Texture2D _notificationBackroundTexture;

        private AsyncTexture2D _icon;

        private Rectangle _layoutIconBounds;

        private PlayerNotification(PlayerProfile profile, string message)
        {
            _notificationBackroundTexture = ModuleInstance.ContentsManager.GetTexture("ns-button.png");
            _icon = ModuleInstance.GetProfessionRender(profile.Player);

            Opacity = 0f;
            Size = new Point(NOTIFICATION_WIDTH, NOTIFICATION_HEIGHT);
            Location = new Point(60, 60 + (NOTIFICATION_HEIGHT + 15) * _visibleNotifications);
            BasicTooltipText = "Click to view profile";

            var wrappedTitle = DrawUtil.WrapText(Content.DefaultFont14, profile.AccountName, Width - NOTIFICATION_HEIGHT - 20 - 32);
            var titleLbl = new Label
            {
                Parent = this,
                Location = new Point(NOTIFICATION_HEIGHT + 10, 0),
                Size = new Point(Width - NOTIFICATION_HEIGHT - 10 - 32, Height / 2),
                Font = Content.DefaultFont14,
                Text = wrappedTitle
            };

            var wrapped = DrawUtil.WrapText(Content.DefaultFont14, message, Width - NOTIFICATION_HEIGHT - 20 - 32);
            var messageLbl = new Label
            {
                Parent = this,
                Location = new Point(NOTIFICATION_HEIGHT + 10, Height / 2),
                Size = new Point(Width - NOTIFICATION_HEIGHT - 10 - 32, Height / 2),
                Text = wrapped
            };

            _visibleNotifications++;

            Click += delegate
            {
                Overlay.BlishHudWindow.Show();
                MainView.LoadProfileView(profile.AccountName);
                Dispose();
            };

            profile.PlayerChanged += (_, e) =>
            {
                _icon = ModuleInstance.GetProfessionRender(e.Value);
            };
        }

        protected override CaptureType CapturesInput()
        {
            return CaptureType.Mouse;
        }

        /// <inheritdoc />
        public override void RecalculateLayout()
        {
            var icoSize = 52;

            _layoutIconBounds = new Rectangle(NOTIFICATION_HEIGHT / 2 - icoSize / 2,
                NOTIFICATION_HEIGHT / 2 - icoSize / 2, icoSize, icoSize);
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            spriteBatch.DrawOnCtrl(this,
                _notificationBackroundTexture,
                bounds,
                Color.White * 0.85f);

            spriteBatch.DrawOnCtrl(this,
                _icon,
                _layoutIconBounds);
        }

        private void Show(float duration)
        {
            Content.PlaySoundEffectByName(@"audio/color-change");

            Animation.Tweener
                .Tween(this, new {Opacity = 1f}, 0.2f)
                .Repeat(1)
                .RepeatDelay(duration)
                .Reflect()
                .OnComplete(Dispose);
        }

        public static void ShowNotification(PlayerProfile profile, string message, float duration)
        {
            var notif = new PlayerNotification(profile, message)
            {
                Parent = Graphics.SpriteScreen
            };

            notif.Show(duration);
        }

        protected override void DisposeControl()
        {
            _visibleNotifications--;

            base.DisposeControl();
        }
    }
}