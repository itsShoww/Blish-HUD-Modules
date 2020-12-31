using Blish_HUD;
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

        [ImportingConstructor]
        public RegionsOfTyriaModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        protected override void DefineSettings(SettingCollection settings) {
            /* NOOP */
        }

        protected override void Initialize() {
            MapRepository = new Dictionary<int, Map>();
        }

        protected override void OnModuleLoaded(EventArgs e) {
            Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;
            BuildDataPanel();

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        /// <inheritdoc />
        protected override void Unload() {
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
            _dataPanel.Fade(1, TimeSpan.FromMilliseconds(2000));
            await Task.Delay(4000).ContinueWith(_ => _dataPanel?.Fade(0, TimeSpan.FromMilliseconds(2000), true));
        }

    }

}
