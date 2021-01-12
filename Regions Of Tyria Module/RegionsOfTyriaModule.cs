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
        private SettingEntry<bool> ToggleMapNotificationSetting;
        private SettingEntry<bool> ToggleSectorNotificationSetting;

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
            ToggleMapNotificationSetting = settings.DefineSetting("EnableMapChangedNotification", true, "Notify map change", "Whether a map's name should be shown when entering a map.");
            ToggleSectorNotificationSetting = settings.DefineSetting("EnableSectorChangedNotification", true, "Notify sector change", "Whether a sector's name should be shown when entering a sector.");
        }

        /*protected override void Initialize() {
        }*/

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

        private void OnShowDurationSettingChanged(object o, ValueChangedEventArgs<float> e) => _showDuration = MathHelper.Clamp(e.NewValue, 0, 100) / 10;
        private void OnFadeInDurationSettingChanged(object o, ValueChangedEventArgs<float> e) => _fadeInDuration = MathHelper.Clamp(e.NewValue, 0, 100) / 10;
        private void OnFadeOutDurationSettingChanged(object o, ValueChangedEventArgs<float> e) => _fadeOutDuration = MathHelper.Clamp(e.NewValue, 0, 100) / 10;

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

                    if (ToggleMapNotificationSetting.Value)
                        MapNotification.ShowNotification(result.RegionName, result.Name, null, _showDuration, _fadeInDuration, _fadeOutDuration);

                    _currentSectors = null;
                    var sectors = new HashSet<ContinentFloorRegionMapSector>();
                    foreach (var floor in result.Floors) {
                        sectors.UnionWith(await RequestSectorsForFloor(result.ContinentId, floor, result.RegionId, result.Id));
                    }
                    _currentSectors = sectors;
                    _currentMap = result;
                });
        }

        private async Task<IEnumerable<ContinentFloorRegionMapSector>> RequestSectorsForFloor(int continentId, int floor, int regionId, int mapId) {
            try {
                return await Gw2ApiManager.Gw2ApiClient.V2.Continents[continentId].Floors[floor].Regions[regionId].Maps[mapId].Sectors.AllAsync();
            } catch (Gw2Sharp.WebApi.Exceptions.BadRequestException e) {
                Logger.Info("{0} | The map id {1} does not exist on floor {2}.", e.GetType().FullName, mapId, floor);
                return new HashSet<ContinentFloorRegionMapSector>();
            }
        }

        private void StartSectorTask() {
            new Task(() => {
                // Check in which sector the player is.
                ContinentFloorRegionMapSector currentSector = null;
                while (!_isDisposing) {
                    if (!ToggleSectorNotificationSetting.Value || !GameIntegration.IsInGame || _currentMap == null || _currentMap.Id != Gw2Mumble.CurrentMap.Id) continue;
                    
                    //var currentFloor = Gw2Mumble.      Not enough data exposed to calculate floor.
                    //var sectors = _currentSectors[currentFloor]

                    // Check for sector change.
                    ContinentFloorRegionMapSector tempSector = null;
                    foreach (var sector in _currentSectors) {
                        var overlapCount = 0;
                        var playerLocation = Gw2Mumble.RawClient.AvatarPosition.ToContinentCoords(CoordsUnit.Mumble, _currentMap.MapRect, _currentMap.ContinentRect).SwapYZ();
                        if (ConvexHullUtil.InBounds(playerLocation.ToPlane(), sector.Bounds)) {
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
