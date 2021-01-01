using Blish_HUD;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp.Models;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Nekres.Regions_Of_Tyria.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using static Blish_HUD.GameService;
namespace Nekres.Regions_Of_Tyria
{
    [Export(typeof(Module))]
    public class RegionsOfTyriaModule : Module
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(RegionsOfTyriaModule));

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
        private SettingEntry<bool> ToggleSectorsSetting;

        private float _showDuration;
        private float _fadeInDuration;
        private float _fadeOutDuration;

        private Thread _checkThread;
        private Map _currentMap;

        [ImportingConstructor]
        public RegionsOfTyriaModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        protected override void DefineSettings(SettingCollection settings) {
            ShowDurationSetting = settings.DefineSetting("ShowDuration", 40.0f, "Show duration", "The duration in which to stay in full opacity.");
            FadeInDurationSetting = settings.DefineSetting("FadeInDuration", 20.0f, "Fade-In duration", "The duration of the fade-in.");
            FadeOutDurationSetting = settings.DefineSetting("FadeOutDuration", 20.0f, "Fade-Out duration", "The duration of the fade-out.");
            ToggleSectorsSetting = settings.DefineSetting("EnableSectors", true, "Show sector transitions", "Whether sector labels should be shown on enter.");
        }

        protected override void Initialize() {
            
        }

        /*protected override void Update(GameTime gameTime) {
        }*/

        protected override void OnModuleLoaded(EventArgs e) {
            Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;

            OnShowDurationSettingChanged(ShowDurationSetting, new ValueChangedEventArgs<float>(0,ShowDurationSetting.Value));
            OnFadeInDurationSettingChanged(FadeInDurationSetting, new ValueChangedEventArgs<float>(0,FadeInDurationSetting.Value));
            OnFadeOutDurationSettingChanged(FadeOutDurationSetting, new ValueChangedEventArgs<float>(0,FadeOutDurationSetting.Value));

            ShowDurationSetting.SettingChanged += OnShowDurationSettingChanged;
            FadeInDurationSetting.SettingChanged += OnFadeInDurationSettingChanged;
            FadeOutDurationSetting.SettingChanged += OnFadeOutDurationSettingChanged;
            ToggleSectorsSetting.SettingChanged += OnToggleSectorsSettingChanged;

            GetCurrentMap(Gw2Mumble.CurrentMap.Id);

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        private void OnShowDurationSettingChanged(object o, ValueChangedEventArgs<float> e) => _showDuration = MathHelper.Clamp(e.NewValue, 0, 100) * 100;
        private void OnFadeInDurationSettingChanged(object o, ValueChangedEventArgs<float> e) => _fadeInDuration = MathHelper.Clamp(e.NewValue, 0, 100) * 100;
        private void OnFadeOutDurationSettingChanged(object o, ValueChangedEventArgs<float> e) => _fadeOutDuration = MathHelper.Clamp(e.NewValue, 0, 100) * 100;
        private void OnToggleSectorsSettingChanged(object o, ValueChangedEventArgs<bool> e) {
            if (e.NewValue) {
                GetSectors(_currentMap);
            } else if (!e.NewValue) {
                GetSectors(_currentMap);
            }
        }
        /// <inheritdoc />
        protected override void Unload() {
            ShowDurationSetting.SettingChanged -= OnShowDurationSettingChanged;
            FadeInDurationSetting.SettingChanged -= OnFadeInDurationSettingChanged;
            FadeOutDurationSetting.SettingChanged -= OnFadeOutDurationSettingChanged;
            ToggleSectorsSetting.SettingChanged -= OnToggleSectorsSettingChanged;
            Gw2Mumble.CurrentMap.MapChanged -= OnMapChanged;
            _checkThread?.Abort();
            _dataPanel?.Dispose();

            // All static members must be manually unset
            ModuleInstance = null;
        }

        private void OnMapChanged(object o, ValueEventArgs<int> e) {
            GetCurrentMap(e.Value);
        }
        private void BuildDataPanel(string header, string footer) {
            _dataPanel?.Dispose();
            var dataPanel = new DataPanel() {
                Parent = Graphics.SpriteScreen,
                Size = new Point(500, 500),
                Location = new Point((Graphics.SpriteScreen.Width / 2) - 250, (Graphics.SpriteScreen.Height / 2) - 500),
                ZIndex = -9999,
                Opacity = 0,
                Header = header,
                Footer = footer
            };

            DoFade(dataPanel);
            _dataPanel = dataPanel;
        }

        private async void GetCurrentMap(int id) {
            await Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(id)
                .ContinueWith(response => {
                    if (response.Exception != null || response.IsFaulted || response.IsCanceled) return;
                    var result = response.Result;
                    
                    _currentMap = result;

                    BuildDataPanel(result.RegionName, result.Name);
                    if (ToggleSectorsSetting.Value) {
                        Task.Delay(TimeSpan.FromMilliseconds(_fadeInDuration + _showDuration + _fadeOutDuration)).ContinueWith(o => {
                            GetSectors(result);
                        });
                    }
                });
        }

        private int GetCurrentFloor(Map map) {
            // Currently not working. Request for mumble exposion of the current floor is pending.
            float temp = 0;
            int currentFloor = 0;
            foreach (var floor in map.Floors) {
                var dist = Math.Abs((float)Gw2Mumble.RawClient.AvatarPosition.Y - floor);

                if (dist < temp || temp == 0) {
                    temp = dist;
                    currentFloor = floor;
                }
            }
            return currentFloor;
        }

        private async Task<IEnumerable<ContinentFloorRegionMapSector>> GetSectors(int continentId, int floor, int regionId, int mapId) {
            return await Gw2ApiManager.Gw2ApiClient.V2.Continents[continentId].Floors[floor].Regions[regionId].Maps[mapId].Sectors.AllAsync();
        }

        private void GetSectors(Map map) {
            _checkThread?.Abort();
            _checkThread = new Thread(async () => {

                /*var currentFloor = GetCurrentFloor(map);
                var sectors = await GetSectors(map.ContinentId, currentFloor, map.RegionId, map.Id);*/

                var sectors = new HashSet<ContinentFloorRegionMapSector>();
                foreach (var floor in map.Floors) {
                    sectors.UnionWith(await GetSectors(map.ContinentId, floor, map.RegionId, map.Id));
                }

                // Check in which sector the player is.
                string currentSector = "";
                while (ToggleSectorsSetting.Value && Gw2Mumble.CurrentMap.Id == map.Id) {

                    // Update sectors to check if floor has changed.
                    /*var tempFloor = GetCurrentFloor(map);
                    if (tempFloor != currentFloor) {
                        currentFloor = tempFloor;
                        sectors = await GetSectors(map.ContinentId, currentFloor, map.RegionId, map.Id);
                    }*/

                    // Check for sector change.
                    string tempSector = null;
                    foreach (var sector in sectors) {
                        var overlapCount = 0;
                            var playerLocation = Gw2Mumble.RawClient.AvatarPosition.ToContinentCoords(CoordsUnit.Mumble, map.MapRect, map.ContinentRect);
                            if (ConvexHullUtil.InBounds(new Coordinates2(playerLocation.X, playerLocation.Z), sector.Bounds)) {
                            overlapCount++;
                            if (overlapCount == 1)
                                tempSector = sector.Name;
                        }
                    }

                    // Display the name of the area when player enters.
                    if (tempSector != null && !tempSector.Equals(currentSector)) {
                        currentSector = tempSector;
                        BuildDataPanel(map.Name, currentSector);
                    }
                }
            });
            _checkThread.Start();
        }

        private async void DoFade(DataPanel panel) {
            panel.Fade(1, TimeSpan.FromMilliseconds(_fadeInDuration));
            await Task.Delay(TimeSpan.FromMilliseconds(_fadeInDuration + _showDuration))
                      .ContinueWith(_ => panel?.Fade(0, TimeSpan.FromMilliseconds(_fadeOutDuration), true));
        }

    }

}
