using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp.ChatLinks;
using Gw2Sharp.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using static Blish_HUD.GameService;

namespace Nekres.Quick_Surrender_Module
{

    [Export(typeof(Module))]
    public class QuickSurrenderModule : Module
    {
        //private static readonly Logger Logger = Logger.GetLogger(typeof(QuickSurrenderModule));

        internal static QuickSurrenderModule ModuleInstance;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        [ImportingConstructor]
        public QuickSurrenderModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        protected override void DefineSettings(SettingCollection settings) {
            SurrenderButtonEnabled = settings.DefineSetting("SurrenderButtonEnabled", true, "Show Surrender Skill",
                "Shows a skill with a white flag to the right of\nyour skill bar while in an instance. Clicking it defeats you.\n(Sends \"/gg\" into chat when in supported modes.)");

            //var keyBindingCol = settings.AddSubCollection("Hotkey", true, false);
            SurrenderBinding = settings.DefineSetting("SurrenderButtonKey", new KeyBinding(Keys.OemPeriod),
                "Surrender Hotkey", "Defeats you.\n(Sends \"/gg\" into chat when in supported modes.)");
        }

        #region PInvoke

            [DllImport("USER32.dll")]
            private static extern short GetKeyState(uint vk);
            private bool IsPressed(uint key){
                return Convert.ToBoolean(GetKeyState(key) & KEY_PRESSED);
            }
            private const uint KEY_PRESSED = 0x8000;
            private const uint VK_LCONTROL = 0xA2;
            private const uint VK_LSHIFT = 0xA0;

        #endregion

        #region Textures

        private Texture2D _surrenderTooltip_texture;
        private Texture2D _surrenderFlag_hover;
        private Texture2D _surrenderFlag;
        private Texture2D _surrenderFlag_pressed;

        #endregion

        #region Controls

        private Image _surrenderButton;

        #endregion

        #region Settings

        private SettingEntry<bool> SurrenderButtonEnabled;
        private SettingEntry<KeyBinding> SurrenderBinding;

        #endregion

        private DateTime _lastSurrenderTime;
        private int _cooldown; //in milliseconds
        private int _skillId; //used when pinging the surrender skill with [Ctrl] + [LeftMouse].

        protected override void Initialize() {
            LoadTextures();

            _lastSurrenderTime = DateTime.Now;
            _cooldown = 2000;
            _skillId = 50347;

            BuildSurrenderButton();
        }


        private void LoadTextures() {
            _surrenderTooltip_texture = ContentsManager.GetTexture("surrender_tooltip.png");
            _surrenderFlag = ContentsManager.GetTexture("surrender_flag.png");
            _surrenderFlag_hover = ContentsManager.GetTexture("surrender_flag_hover.png");
            _surrenderFlag_pressed = ContentsManager.GetTexture("surrender_flag_pressed.png");
        }


        protected override void OnModuleLoaded(EventArgs e) {
            SurrenderBinding.Value.Enabled = true;
            SurrenderBinding.Value.Activated += OnSurrenderBindingActivated;
            SurrenderButtonEnabled.SettingChanged += OnSurrenderButtonEnabledSettingChanged;

            Gw2Mumble.UI.IsMapOpenChanged += OnIsMapOpenChanged;
            GameIntegration.IsInGameChanged += OnIsInGameChanged;

            // Base handler must be called
            base.OnModuleLoaded(e);
        }


        protected override void Update(GameTime gameTime) {
            if (_surrenderButton == null) return;
            _surrenderButton.Location = new Point(Graphics.SpriteScreen.Width / 2 - _surrenderButton.Width / 2 + 431, Graphics.SpriteScreen.Height - _surrenderButton.Height * 2 + 7);
        }


        /// <inheritdoc />
        protected override void Unload() {
            SurrenderBinding.Value.Activated -= OnSurrenderBindingActivated;
            SurrenderButtonEnabled.SettingChanged -= OnSurrenderButtonEnabledSettingChanged;

            Gw2Mumble.UI.IsMapOpenChanged -= OnIsMapOpenChanged;
            GameIntegration.IsInGameChanged -= OnIsInGameChanged;

            _surrenderButton?.Dispose();
            // All static members must be manually unset
            ModuleInstance = null;
        }


        private void DoSurrender() {
            if (!IsUiAvailable() || Gw2Mumble.UI.IsTextInputFocused || Gw2Mumble.CurrentMap.Type != MapType.Instance) return;
            if (DateTimeOffset.Now.Subtract(_lastSurrenderTime).TotalMilliseconds < _cooldown) {
                ScreenNotification.ShowNotification("Skill recharging.", ScreenNotification.NotificationType.Error);
                return;
            }
            GameIntegration.Chat.Send("/gg");
            _lastSurrenderTime = DateTime.Now;
        }


        private bool IsUiAvailable() => Gw2Mumble.IsAvailable && GameIntegration.IsInGame && !Gw2Mumble.UI.IsMapOpen;

        private void OnSurrenderBindingActivated(object o, EventArgs e) => DoSurrender();

        private void OnIsMapOpenChanged(object o, ValueEventArgs<bool> e) => ToggleSurrenderButton(!e.Value, 0.45f);
        private void OnIsInGameChanged(object o, ValueEventArgs<bool> e) => ToggleSurrenderButton(e.Value, 0.1f);
        private void OnSurrenderButtonEnabledSettingChanged(object o, ValueChangedEventArgs<bool> e) => ToggleSurrenderButton(e.NewValue, 0.1f);

        private void ToggleSurrenderButton(bool enabled, float tDuration) {
            if (enabled)
                BuildSurrenderButton();
            else if (_surrenderButton != null)
                Animation.Tweener.Tween(_surrenderButton, new {Opacity = 0.0f}, tDuration).OnComplete(() => _surrenderButton?.Dispose());
        }


        private void BuildSurrenderButton()
        {
            _surrenderButton?.Dispose();

            if (!SurrenderButtonEnabled.Value || !IsUiAvailable() || Gw2Mumble.CurrentMap.Type != MapType.Instance) return;

            var tooltip_size = new Point(_surrenderTooltip_texture.Width, _surrenderTooltip_texture.Height);
            var surrenderButtonTooltip = new Tooltip
            {
                Size = tooltip_size
            };
            var surrenderButtonTooltipImage = new Image(_surrenderTooltip_texture)
            {
                Parent = surrenderButtonTooltip,
                Location = new Point(0, 0),
                Visible = surrenderButtonTooltip.Visible
            };
            _surrenderButton = new Image
            {
                Parent = Graphics.SpriteScreen,
                Size = new Point(45, 45),
                Location = new Point(Graphics.SpriteScreen.Width / 2 - 22, Graphics.SpriteScreen.Height - 45),
                Texture = _surrenderFlag,
                Tooltip = surrenderButtonTooltip,
                Opacity = 0.0f
            };

            _surrenderButton.MouseEntered += delegate { _surrenderButton.Texture = _surrenderFlag_hover; };
            _surrenderButton.MouseLeft += delegate { _surrenderButton.Texture = _surrenderFlag; };

            _surrenderButton.LeftMouseButtonPressed += delegate
            {
                _surrenderButton.Size = new Point(43, 43);
                _surrenderButton.Texture = _surrenderFlag_pressed;
            };

            _surrenderButton.LeftMouseButtonReleased += delegate
            {
                _surrenderButton.Size = new Point(45, 45);
                _surrenderButton.Texture = _surrenderFlag;
            };

            _surrenderButton.RightMouseButtonPressed += delegate (object o, MouseEventArgs e) {
                _surrenderButton.Size = new Point(43, 43);
                _surrenderButton.Texture = _surrenderFlag_pressed;
            };

            _surrenderButton.RightMouseButtonReleased += delegate
            {
                _surrenderButton.Size = new Point(45, 45);
                _surrenderButton.Texture = _surrenderFlag;

                GameIntegration.Chat.Send(" /gg");
            };

            _surrenderButton.Click += delegate (object o, MouseEventArgs e) {
                if (IsPressed(VK_LCONTROL))
                    GameIntegration.Chat.Send(new SkillChatLink(){ SkillId = _skillId }.ToString());
                else
                    OnSurrenderBindingActivated(o, e);
            };

            Animation.Tweener.Tween(_surrenderButton, new {Opacity = 1.0f}, 0.35f);
        }
    }

}
