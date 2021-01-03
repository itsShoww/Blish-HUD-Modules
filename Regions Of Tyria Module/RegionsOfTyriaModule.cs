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

        private bool _isDisposing;

        private Map _currentMap;
        private HashSet<ContinentFloorRegionMapSector> _currentSectors;

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

            RequestMap(Gw2Mumble.CurrentMap.Id);
            StartSectorTask();

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        private void OnShowDurationSettingChanged(object o, ValueChangedEventArgs<float> e) => _showDuration = MathHelper.Clamp(e.NewValue, 0, 100) * 100 / 1000;
        private void OnFadeInDurationSettingChanged(object o, ValueChangedEventArgs<float> e) => _fadeInDuration = MathHelper.Clamp(e.NewValue, 0, 100) * 100 / 1000;
        private void OnFadeOutDurationSettingChanged(object o, ValueChangedEventArgs<float> e) => _fadeOutDuration = MathHelper.Clamp(e.NewValue, 0, 100) * 100 / 1000;

        /// <inheritdoc />
        protected override void Unload() {
            _isDisposing = true;
            ShowDurationSetting.SettingChanged -= OnShowDurationSettingChanged;
            FadeInDurationSetting.SettingChanged -= OnFadeInDurationSettingChanged;
            FadeOutDurationSetting.SettingChanged -= OnFadeOutDurationSettingChanged;
            Gw2Mumble.CurrentMap.MapChanged -= OnMapChanged;

            // All static members must be manually unset
            ModuleInstance = null;
        }

        private void OnMapChanged(object o, ValueEventArgs<int> e) {
            RequestMap(e.Value);
        }

        private async void RequestMap(int id) {
            await Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(id)
                .ContinueWith(async response => {
                    if (response.Exception != null || response.IsFaulted || response.IsCanceled) return;
                    var result = response.Result;

                    _currentMap = result;
                    MapNotification.ShowNotification(result.RegionName, result.Name, null, _showDuration, _fadeInDuration, _fadeOutDuration);

                    var sectors = new HashSet<ContinentFloorRegionMapSector>();
                    foreach (var floor in _currentMap.Floors) {
                        sectors.UnionWith(await RequestSectorsForFloor(_currentMap.ContinentId, floor, _currentMap.RegionId, _currentMap.Id));
                    }
                    _currentSectors = sectors;
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

        private async Task<IEnumerable<ContinentFloorRegionMapSector>> RequestSectorsForFloor(int continentId, int floor, int regionId, int mapId) {
            try {
                return await Gw2ApiManager.Gw2ApiClient.V2.Continents[continentId].Floors[floor].Regions[regionId].Maps[mapId].Sectors.AllAsync();
            } catch (Gw2Sharp.WebApi.Exceptions.BadRequestException e) {
                Logger.Info("The map id {0} does not exist on floor {1}. Ref.: {2}", mapId, floor, e.Request);
                return new HashSet<ContinentFloorRegionMapSector>();
            }
        }

        private void StartSectorTask() {
            new Task(() => {
                /*var currentFloor = GetCurrentFloor(map);
                var sectors = await GetSectors(map.ContinentId, currentFloor, map.RegionId, map.Id);*/

                // Check in which sector the player is.
                ContinentFloorRegionMapSector currentSector = null;
                while (!_isDisposing) {
                    if (!ToggleSectorsSetting.Value || _currentMap == null || _currentSectors == null) continue;

                    // Update sectors to check if floor has changed.
                    /*var tempFloor = GetCurrentFloor(map);
                    if (tempFloor != currentFloor) {
                        currentFloor = tempFloor;
                        sectors = await RequestSectorsForFloor(map.ContinentId, currentFloor, map.RegionId, map.Id);
                    }*/
                    // Check for sector change.
                    ContinentFloorRegionMapSector tempSector = null;
                    foreach (var sector in _currentSectors) {
                        var overlapCount = 0;
                            var playerLocation = Gw2Mumble.RawClient.AvatarPosition.ToContinentCoords(CoordsUnit.Mumble, _currentMap.MapRect, _currentMap.ContinentRect);
                            if (ConvexHullUtil.InBounds(new Coordinates2(playerLocation.X, playerLocation.Z), sector.Bounds)) {
                            overlapCount++;
                            if (overlapCount == 1)
                                tempSector = sector;
                        }
                    }
                    // Display the name of the area when player enters.
                    if (tempSector != null && !tempSector.Equals(currentSector)) {
                        currentSector = tempSector;
                        MapNotification.ShowNotification(_currentMap.Name, currentSector.Name, null, _showDuration, _fadeInDuration, _fadeOutDuration);
                    }
                }
            }).Start();
        }
    }
}
