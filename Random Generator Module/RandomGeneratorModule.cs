using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using static Blish_HUD.GameService;

namespace Nekres.Random_Generator_Module
{
    [Export(typeof(Module))]
    public class RandomGeneratorModule : Module
    {
        //private static readonly Logger Logger = Logger.GetLogger(typeof(RandomGeneratorModule));

        internal static RandomGeneratorModule ModuleInstance;

        #region Service Managers

        internal SettingsManager SettingsManager => ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => ModuleParameters.Gw2ApiManager;

        #endregion

        [ImportingConstructor]
        public RandomGeneratorModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        protected override void DefineSettings(SettingCollection settings)
        {
            ToggleShowDieSetting = settings.DefineSetting("ShowDie", true, "Show die", "Whether a die should be displayed to the right of your skill bar.");
            ToggleSendToChatSetting = settings.DefineSetting("SendToChat", false, "Send to chat", "Whether results should be displayed and emphasised in chat.\nWarning: Can trigger supression of similar messages if results are generated too frequently.");
            var selfManagedSettings = settings.AddSubCollection("ManagedSettings", false, false);
            DieSides = selfManagedSettings.DefineSetting("DieSides", 6, "Die Sides", "Indicates the amount of sides the die has.");
        }

        #region Textures

        private List<Texture2D> _dieTextures = new List<Texture2D>();
        //private List<Texture2D> _coinTextures = new List<Texture2D>();

        #endregion

        #region Controls

        private Panel Die;

        #endregion

        #region Settings

        private SettingEntry<int> DieSides;
        private SettingEntry<bool> ToggleShowDieSetting;
        private SettingEntry<bool> ToggleSendToChatSetting;

        #endregion


        protected override void Initialize()
        {
            LoadTextures();

            CreateDie();
        }


        private void LoadTextures() {
            for (var i = 0; i < 7; i++) _dieTextures.Add(ContentsManager.GetTexture($"dice/side{i}.png"));

            /*_coinTextures.Add(ContentsManager.GetTexture("coin/heads.png"));
            _coinTextures.Add(ContentsManager.GetTexture("coin/tails.png"));*/
        }


        protected override void OnModuleLoaded(EventArgs e)
        {
            ToggleShowDieSetting.SettingChanged += OnShowDieSettingChanged;

            Gw2Mumble.UI.IsMapOpenChanged += OnIsMapOpenChanged;
            GameIntegration.IsInGameChanged += OnIsInGameChanged;

            // Base handler must be called
            base.OnModuleLoaded(e);
        }


        protected override void Update(GameTime gameTime)
        {
            if (Die == null) return;
            Die.Location = new Point(Graphics.SpriteScreen.Width - 480, Graphics.SpriteScreen.Height - Die.Height - 25);
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            ToggleShowDieSetting.SettingChanged -= OnShowDieSettingChanged;
            Gw2Mumble.UI.IsMapOpenChanged -= OnIsMapOpenChanged;
            GameIntegration.IsInGameChanged -= OnIsInGameChanged;
            Die?.Dispose();
            _dieTextures.Clear();
            _dieTextures = null;
            // All static members must be manually unset
            ModuleInstance = null;
        }
        
        private bool IsUiAvailable() => Gw2Mumble.IsAvailable && GameIntegration.IsInGame && !Gw2Mumble.UI.IsMapOpen;

        private void OnIsMapOpenChanged(object o, ValueEventArgs<bool> e) => ToggleControls(!e.Value, 0.45f);
        private void OnIsInGameChanged(object o, ValueEventArgs<bool> e) => ToggleControls(e.Value, 0.1f);
        private void OnShowDieSettingChanged(object o, ValueChangedEventArgs<bool> e) => ToggleControls(e.NewValue, 0.1f);

        private void ToggleControls(bool enabled, float tDuration) {
            if (enabled)
                CreateDie();
            else if (Die != null)
                Animation.Tweener.Tween(Die, new {Opacity = 0.0f}, tDuration).OnComplete(() => Die?.Dispose());
        }

        private void CreateDie()
        {
            Die?.Dispose();

            if (!ToggleShowDieSetting.Value || !IsUiAvailable()) return;

            DieSides.Value = DieSides.Value > 100 || DieSides.Value < 2 ? 6 : DieSides.Value;

            var rolling = false;
            Die = new Panel
            {
                Parent = Graphics.SpriteScreen,
                Size = new Point(64, 64),
                Location = new Point(0, 0),
                Opacity = 0.0f
            };
            var dieImage = new Image
            {
                Parent = Die,
                Texture = _dieTextures[0],
                Size = new Point(64, 64),
                Location = new Point(0, 0)
            };
            var dieLabel = new Label
            {
                Parent = Die,
                Size = Die.Size,
                Location = new Point(0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle,
                Font = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size22, ContentService.FontStyle.Regular),
                ShowShadow = true,
                TextColor = Color.Black,
                ShadowColor = Color.Black,
                StrokeText = false,
                Text = ""
            };

            int ApplyDieValue(bool reset = false)
            {
                var value = reset ? DieSides.Value : RandomUtil.GetRandom(1, DieSides.Value);
                if (value < 7) // Write text on a blank die side or apply texture with up to six dots.
                {
                    dieLabel.Text = "";
                    dieImage.Texture = _dieTextures[value];
                }
                else
                {
                    dieImage.Texture = _dieTextures[0];
                    dieLabel.Text = $"{value}";
                }

                return value;
            }

            ApplyDieValue();

            var dieSettingsOpen = false;
            Die.RightMouseButtonPressed += delegate
            {
                if (rolling || dieSettingsOpen) return;
                dieSettingsOpen = true;
                var sidesTotalPanel = new Panel
                {
                    Parent = Graphics.SpriteScreen,
                    Size = new Point(200, 120),
                    Location = new Point(Graphics.SpriteScreen.Width / 2 - 100, Graphics.SpriteScreen.Height / 2 - 60),
                    Opacity = 0.0f,
                    BackgroundTexture = Content.GetTexture("controls/window/502049"),
                    ShowBorder = true,
                    Title = "Die Sides"
                };
                var counter = new CounterBox
                {
                    Parent = sidesTotalPanel,
                    Size = new Point(100, 100),
                    ValueWidth = 60,
                    Location = new Point(sidesTotalPanel.ContentRegion.Width / 2 - 50, sidesTotalPanel.Height / 2 - 50),
                    MaxValue = 100,
                    MinValue = 2,
                    Value = DieSides.Value,
                    Numerator = 1,
                    Suffix = " sides"
                };
                var applyButton = new StandardButton
                {
                    Parent = sidesTotalPanel,
                    Size = new Point(50, 30),
                    Location = new Point(sidesTotalPanel.ContentRegion.Width / 2 - 25,
                        sidesTotalPanel.ContentRegion.Height - 35),
                    Text = "Apply"
                };
                applyButton.LeftMouseButtonPressed += delegate
                {
                    DieSides.Value = counter.Value;
                    dieSettingsOpen = false;
                    ApplyDieValue(true);
                    Animation.Tweener.Tween(sidesTotalPanel, new {Opacity = 0.0f}, 0.2f)
                                     .OnComplete(() => { sidesTotalPanel.Dispose(); });
                };
                Animation.Tweener.Tween(sidesTotalPanel, new {Opacity = 1.0f}, 0.2f);
            };

            Die.MouseEntered += delegate
            {
                Animation.Tweener.Tween(Die, new {Opacity = 1.0f}, 0.45f);
            };
            Die.MouseLeft += delegate
            {
                Animation.Tweener.Tween(Die, new {Opacity = 0.4f}, 0.45f);
            };

            Die.LeftMouseButtonPressed += delegate
            {
                if (rolling || dieSettingsOpen) return;
                rolling = true;

                var duration = new Stopwatch();
                var worker = new BackgroundWorker();
                var interval = new Timer(70);
                interval.Elapsed += delegate
                {
                    if (!worker.IsBusy)
                        worker.RunWorkerAsync();
                };
                worker.DoWork += delegate
                {
                    var value = ApplyDieValue();

                    if (duration.Elapsed > TimeSpan.FromMilliseconds(1200))
                    {
                        interval?.Stop();
                        interval?.Dispose();
                        duration?.Stop();
                        duration = null;
                        if (ToggleSendToChatSetting.Value && !Gw2Mumble.UI.IsTextInputFocused)
                            GameIntegration.Chat.Send($"/me rolls {value} on a {DieSides.Value} sided die.");
                        ScreenNotification.ShowNotification(
                            $"{(Gw2Mumble.IsAvailable ? Gw2Mumble.PlayerCharacter.Name : "You")} rolls {value} on a {DieSides.Value} sided die.");
                        rolling = false;
                        worker.Dispose();
                    }
                };
                interval.Start();
                duration.Start();
            };
            Animation.Tweener.Tween(Die, new {Opacity = 0.4f}, 0.35f);
        }
    }
}