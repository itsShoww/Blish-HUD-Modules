using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Nekres.Regions_Of_Tyria.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using static Blish_HUD.GameService;
namespace Nekres.Regions_Of_Tyria
{
    [Export(typeof(Module))]
    public class RegionsOfTyriaModule : Module
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(RegionsOfTyriaModule));

        public Dictionary<int, Map> MapRepository { get; private set; }

        internal static RegionsOfTyriaModule ModuleInstance;

        private DataPanel _dataPanel;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        private SettingEntry<float> ShowDurationSetting;
        private SettingEntry<float> FadeInDurationSetting;
        private SettingEntry<float> FadeOutDurationSetting;

        private float _showDuration;
        private float _fadeInDuration;
        private float _fadeOutDuration;

        [ImportingConstructor]
        public RegionsOfTyriaModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        protected override void DefineSettings(SettingCollection settings) {
            ShowDurationSetting = settings.DefineSetting("ShowDuration", 40.0f, "Show duration", "The duration in which to stay in full opacity.");
            FadeInDurationSetting = settings.DefineSetting("FadeInDuration", 20.0f, "Fade-In duration", "The duration of the fade-in.");
            FadeOutDurationSetting = settings.DefineSetting("FadeOutDuration", 20.0f, "Fade-Out duration", "The duration of the fade-out.");
        }

        protected override void Initialize() {
            MapRepository = new Dictionary<int, Map>();
        }

        protected override void OnModuleLoaded(EventArgs e) {
            Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;

            OnShowDurationSettingChanged(ShowDurationSetting, new ValueChangedEventArgs<float>(0,ShowDurationSetting.Value));
            OnFadeInDurationSettingChanged(FadeInDurationSetting, new ValueChangedEventArgs<float>(0,FadeInDurationSetting.Value));
            OnFadeOutDurationSettingChanged(FadeOutDurationSetting, new ValueChangedEventArgs<float>(0,FadeOutDurationSetting.Value));

            ShowDurationSetting.SettingChanged += OnShowDurationSettingChanged;
            FadeInDurationSetting.SettingChanged += OnFadeInDurationSettingChanged;
            FadeOutDurationSetting.SettingChanged += OnFadeOutDurationSettingChanged;

            BuildDataPanel();

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        private void OnShowDurationSettingChanged(object o, ValueChangedEventArgs<float> e) => _showDuration = MathHelper.Clamp(e.NewValue, 0, 100) * 100;
        private void OnFadeInDurationSettingChanged(object o, ValueChangedEventArgs<float> e) => _fadeInDuration = MathHelper.Clamp(e.NewValue, 0, 100) * 100;
        private void OnFadeOutDurationSettingChanged(object o, ValueChangedEventArgs<float> e) => _fadeOutDuration = MathHelper.Clamp(e.NewValue, 0, 100) * 100;

        /// <inheritdoc />
        protected override void Unload() {
            ShowDurationSetting.SettingChanged -= OnShowDurationSettingChanged;
            FadeInDurationSetting.SettingChanged -= OnFadeInDurationSettingChanged;
            FadeOutDurationSetting.SettingChanged -= OnFadeOutDurationSettingChanged;
            Gw2Mumble.CurrentMap.MapChanged -= OnMapChanged;
            _dataPanel?.Dispose();

            // All static members must be manually unset
            ModuleInstance = null;
        }

        private void OnMapChanged(object o, ValueEventArgs<int> e) {
            BuildDataPanel();
        }
        private void BuildDataPanel() {
            _dataPanel?.Dispose();
            _dataPanel = new DataPanel() {
                Parent = Graphics.SpriteScreen,
                Size = new Point(500, 500),
                Location = new Point((Graphics.SpriteScreen.Width / 2) - 250, (Graphics.SpriteScreen.Height / 2) - 500),
                ZIndex = -9999,
                Opacity = 0
            };
            GetCurrentMap(Gw2Mumble.CurrentMap.Id);
        }

        private async void GetCurrentMap(int id) {
            if (MapRepository.ContainsKey(id)) {
                if (_dataPanel == null) return;
                _dataPanel.CurrentMap = MapRepository[id];
                DoFade();
            } else {
                await Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(id)
                    .ContinueWith(response => {
                        if (response.Exception != null || response.IsFaulted || response.IsCanceled) return;
                        var result = response.Result;
                        MapRepository.Add(result.Id, result);
                        if (_dataPanel == null) return;
                        _dataPanel.CurrentMap = result;
                        DoFade();
                });
            }
        }

        private async void DoFade() {
            _dataPanel.Fade(1, TimeSpan.FromMilliseconds(_fadeInDuration));
            await Task.Delay(TimeSpan.FromMilliseconds(_fadeInDuration + _showDuration))
                      .ContinueWith(_ => _dataPanel?.Fade(0, TimeSpan.FromMilliseconds(_fadeOutDuration), true));
        }

    }

}
