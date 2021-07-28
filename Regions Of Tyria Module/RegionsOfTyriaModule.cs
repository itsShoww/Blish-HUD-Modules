using Blish_HUD;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Nekres.Regions_Of_Tyria.Controls;
using Nekres.Regions_Of_Tyria.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
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
        private SettingEntry<bool> IncludeRegionInMapNotification;
        private SettingEntry<bool> IncludeMapInSectorNotification;

        private float _showDuration;
        private float _fadeInDuration;
        private float _fadeOutDuration;

        private AsyncCache<int, Map> _mapRepository;
        private AsyncCache<int, HashSet<ContinentFloorRegionMapSector>> _sectorRepository;
        private int _prevSectorId;
        private int _prevMapId;

        [ImportingConstructor]
        public RegionsOfTyriaModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        protected override void DefineSettings(SettingCollection settings) {
            ShowDurationSetting = settings.DefineSetting("ShowDuration", 40.0f, "Show Duration", "The duration in which to stay in full opacity.");
            FadeInDurationSetting = settings.DefineSetting("FadeInDuration", 20.0f, "Fade-In Duration", "The duration of the fade-in.");
            FadeOutDurationSetting = settings.DefineSetting("FadeOutDuration", 20.0f, "Fade-Out Duration", "The duration of the fade-out.");
            ToggleMapNotificationSetting = settings.DefineSetting("EnableMapChangedNotification", true, "Notify Map Change", "Whether a map's name should be shown when entering a map.");
            IncludeRegionInMapNotification = settings.DefineSetting("IncludeRegionInMapNotification", true,
                "Include Region Name in Map Notification",
                "Whether the corresponding region name of a map should be shown above a map's name.");
            ToggleSectorNotificationSetting = settings.DefineSetting("EnableSectorChangedNotification", true, "Notify Sector Change", "Whether a sector's name should be shown when entering a sector.");
            IncludeMapInSectorNotification = settings.DefineSetting("IncludeMapInSectorNotification", true,
                "Include Map Name in Sector Notification",
                "Whether the corresponding map name of a sector should be shown above a sector's name.");
        }

        protected override void Initialize()
        {
            _mapRepository = new AsyncCache<int, Map>(id => Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(id));
            _sectorRepository = new AsyncCache<int, HashSet<ContinentFloorRegionMapSector>>(RequestSectors);
        }

        protected override async void Update(GameTime gameTime) {
            
            if (!ToggleSectorNotificationSetting.Value || !Gw2Mumble.IsAvailable || !GameIntegration.IsInGame) 
                return;

            //Check in which sector the player is.
            //var currentFloor = Gw2Mumble.      Not enough data exposed to calculate floor.
            //var sectors = _currentSectors[currentFloor]
            //Note: overlapCount can be removed once current sector is determinable by current floor.

            var currentMap = await _mapRepository.GetItem(Gw2Mumble.CurrentMap.Id);

            // Check for sector change.
            ContinentFloorRegionMapSector currentSector = null;
            var overlapCount = 0;
            foreach (var sector in await _sectorRepository.GetItem(Gw2Mumble.CurrentMap.Id))
            {
                var playerLocation = Gw2Mumble.RawClient.AvatarPosition.ToContinentCoords(CoordsUnit.Mumble, currentMap.MapRect, currentMap.ContinentRect).SwapYZ();
                if (ConvexHullUtil.InBounds(playerLocation.ToPlane(), sector.Bounds))
                {
                    overlapCount++;
                    if (overlapCount == 1)
                        currentSector = sector;
                }
            }

            if (overlapCount > 2 || currentSector == null || currentSector.Id == _prevSectorId)
                return;
            _prevSectorId = currentSector.Id;

            // Display the name of the area when player enters.
            MapNotification.ShowNotification(IncludeMapInSectorNotification.Value ? currentMap.Name : null, currentSector.Name, null, _showDuration, _fadeInDuration, _fadeOutDuration);
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;
            Overlay.UserLocaleChanged += OnUserLocaleChanged;

            OnShowDurationSettingChanged(ShowDurationSetting, new ValueChangedEventArgs<float>(0,ShowDurationSetting.Value));
            OnFadeInDurationSettingChanged(FadeInDurationSetting, new ValueChangedEventArgs<float>(0,FadeInDurationSetting.Value));
            OnFadeOutDurationSettingChanged(FadeOutDurationSetting, new ValueChangedEventArgs<float>(0,FadeOutDurationSetting.Value));

            ShowDurationSetting.SettingChanged += OnShowDurationSettingChanged;
            FadeInDurationSetting.SettingChanged += OnFadeInDurationSettingChanged;
            FadeOutDurationSetting.SettingChanged += OnFadeOutDurationSettingChanged;

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        private void OnShowDurationSettingChanged(object o, ValueChangedEventArgs<float> e) => _showDuration = MathHelper.Clamp(e.NewValue, 0, 100) / 10;
        private void OnFadeInDurationSettingChanged(object o, ValueChangedEventArgs<float> e) => _fadeInDuration = MathHelper.Clamp(e.NewValue, 0, 100) / 10;
        private void OnFadeOutDurationSettingChanged(object o, ValueChangedEventArgs<float> e) => _fadeOutDuration = MathHelper.Clamp(e.NewValue, 0, 100) / 10;

        /// <inheritdoc />
        protected override void Unload() {
            ShowDurationSetting.SettingChanged -= OnShowDurationSettingChanged;
            FadeInDurationSetting.SettingChanged -= OnFadeInDurationSettingChanged;
            FadeOutDurationSetting.SettingChanged -= OnFadeOutDurationSettingChanged;
            Gw2Mumble.CurrentMap.MapChanged -= OnMapChanged;
            Overlay.UserLocaleChanged -= OnUserLocaleChanged;

            // All static members must be manually unset
            ModuleInstance = null;
        }

        private void OnUserLocaleChanged(object o, ValueEventArgs<System.Globalization.CultureInfo> e)
        {
            _mapRepository = new AsyncCache<int, Map>(id => Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(id));
            _sectorRepository = new AsyncCache<int, HashSet<ContinentFloorRegionMapSector>>(RequestSectors);
        }

        private async void OnMapChanged(object o, ValueEventArgs<int> e)
        {
            if (!ToggleMapNotificationSetting.Value) 
                return;

            var currentMap = await _mapRepository.GetItem(e.Value);

            if (currentMap == null || currentMap.Id == _prevMapId)
                return;

            _prevMapId = currentMap.Id;

            MapNotification.ShowNotification(IncludeRegionInMapNotification.Value ? currentMap.RegionName : null, currentMap.Name, null, _showDuration, _fadeInDuration, _fadeOutDuration);
        }

        private async Task<HashSet<ContinentFloorRegionMapSector>> RequestSectors(int mapId)
        {
            return await await _mapRepository.GetItem(mapId).ContinueWith(async result =>
            {
                if (!result.IsCompleted || result.IsFaulted) 
                    return null;
                var map = result.Result;
                var sectors = new HashSet<ContinentFloorRegionMapSector>();
                foreach (var floor in map.Floors)
                {
                    sectors.UnionWith(await RequestSectorsForFloor(map.ContinentId, floor, map.RegionId, map.Id));
                }
                return sectors.DistinctBy(x => x.Id).ToHashSet();
            });
        }

        private async Task<IEnumerable<ContinentFloorRegionMapSector>> RequestSectorsForFloor(int continentId, int floor, int regionId, int mapId) {
            try {
                return await Gw2ApiManager.Gw2ApiClient.V2.Continents[continentId].Floors[floor].Regions[regionId].Maps[mapId].Sectors.AllAsync();
            } catch (Gw2Sharp.WebApi.Exceptions.BadRequestException e) {
                Logger.Debug("{0} | The map id {1} does not exist on floor {2}.", e.GetType().FullName, mapId, floor);
                return new HashSet<ContinentFloorRegionMapSector>();
            }
        }
    }
}
