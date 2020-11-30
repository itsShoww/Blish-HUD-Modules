using Blish_HUD;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using static Blish_HUD.GameService;
using static Nekres.Music_Mixer.Gw2StateService;

namespace Nekres.Music_Mixer
{

    [Export(typeof(Module))]
    public class MusicMixerModule : Module
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(MusicMixerModule));

        internal static MusicMixerModule ModuleInstance;

        #region Service Managers

        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;

        #endregion

        [ImportingConstructor]
        public MusicMixerModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        #region Settings

        internal SettingEntry<float> MasterVolume;
        internal SettingEntry<bool> ToggleSubmergedPlaylist;
        internal SettingEntry<bool> ToggleFourDayCycle;

        #endregion

        private MusicPlayer _musicPlayer;
        private Gw2StateService _gw2State;

        private string _moduleDirectory;

        private const string _FFmpegPath = "bin/ffmpeg.exe";
        private const string _youtubeDLPath = "bin/youtube-dl.exe";

        protected override void DefineSettings(SettingCollection settings) {
            MasterVolume = settings.DefineSetting("MasterVolume", 50.0f, "Master Volume", "Sets the audio volume.");
            ToggleSubmergedPlaylist = settings.DefineSetting("EnableSubmergedPlaylist", false, "Use submerged playlist", "If songs from the underwater playlist should be used while submerged.");
            ToggleFourDayCycle = settings.DefineSetting("EnableFourDayCycle", false, "Use dusk and dawn day cycles", "If dusk and dawn track attributes should be interpreted as unique day cycles.\nOtherwise dusk and dawn will be interpreted as night and day respectively.");
        }


        private void ExtractFile(string filePath) {
            var fullPath = Path.Combine(_moduleDirectory, filePath);
            if (File.Exists(fullPath)) return;
            using (var fs = ContentsManager.GetFileStream(filePath)) {
                fs.Position = 0;
                byte[] buffer = new byte[fs.Length];
                var content = fs.Read(buffer, 0, (int)fs.Length);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllBytes(fullPath, buffer);
            }
        }


        private IReadOnlyList<EncounterData> LoadEncounterData() {
            using (var fs = ContentsManager.GetFileStream("encounterData.json")) {
                fs.Position = 0;
                using (var jsonReader = new JsonTextReader(new StreamReader(fs)))
                {
                    var serializer = new JsonSerializer();
                    return serializer.Deserialize<IReadOnlyList<EncounterData>>(jsonReader);
                }
            }
        }


        protected override void Initialize() {
            _moduleDirectory = DirectoriesManager.GetFullDirectoryPath("music_mixer");

            ExtractFile(_FFmpegPath);
            ExtractFile(_youtubeDLPath);

            _musicPlayer = new MusicPlayer(_moduleDirectory, _FFmpegPath, _youtubeDLPath);
            _gw2State = new Gw2StateService(LoadEncounterData());
        }

        private void OnMasterVolumeSettingChanged(object o, ValueChangedEventArgs<float> e) {
            _musicPlayer.SetVolume(e.NewValue / 1000);
        }

        protected override void Update(GameTime gameTime) {
            _gw2State.CheckTyrianTime();
            _gw2State.CheckWaterLevel();
            _gw2State.CheckEncounterReset();
        }

        private void OnGw2Closed(object sender, EventArgs e) => _musicPlayer?.Stop();

        private void OnTyrianTimeChanged(object sender, ValueEventArgs<TyrianTime> e) {
            switch (e.Value) {
                case TyrianTime.None: 
                    return;
                case TyrianTime.Dawn:
                case TyrianTime.Dusk:
                    if (!ToggleFourDayCycle.Value) 
                        return;
                    break;
                default: break;
            }
            _musicPlayer.PlayNext();
        }


        private void OnIsSubmergedChanged(object o, ValueEventArgs<bool> e) {
            if (ToggleSubmergedPlaylist.Value) return;
            _musicPlayer.ToggleSubmergedFx(e.Value);
        }


        protected override void OnModuleLoaded(EventArgs e) {
            MasterVolume.SettingChanged += OnMasterVolumeSettingChanged;
            _gw2State.IsSubmergedChanged += OnIsSubmergedChanged;
            _gw2State.TyrianTimeChanged += OnTyrianTimeChanged;
            GameIntegration.Gw2Closed += OnGw2Closed;
            _gw2State.StateChanged += OnStateChanged;
            _gw2State.EncounterChanged += OnEncounterChanged;
            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        private void OnEncounterChanged(object o, ValueChangedEventArgs<Encounter> e) {
            if (e.PreviousValue != null)
                e.PreviousValue.PhaseChanged -= OnPhaseChanged;
            if (e.NewValue != null)
                e.NewValue.PhaseChanged += OnPhaseChanged;
        }
        private void OnPhaseChanged(object o, ValueEventArgs<int> e) => _musicPlayer.PlayEncounterTrack(_gw2State.CurrentEncounter);

        private void OnStateChanged(object sender, ValueChangedEventArgs<State> e) {
            /**
             * Stop or fade depending on previous state.
             */
            if (_musicPlayer.IsFading)
                _musicPlayer.Stop();

            switch (e.PreviousValue) {
                case State.Mounted:
                case State.Combat:
                case State.Encounter:
                case State.Submerged:
                    _musicPlayer.FadeOut();
                    break;
                default:
                    _musicPlayer.Stop(); 
                    break;
            }
            /**
             * Start playing a track.
             */
            switch (e.NewValue) {
                case State.Encounter:
                    _musicPlayer.PlayEncounterTrack(_gw2State.CurrentEncounter);
                    break;
                case State.Mounted:
                    _musicPlayer.PlayMountTrack(Gw2Mumble.PlayerCharacter.CurrentMount);
                    break;
                case State.OpenWorld:
                    _musicPlayer.PlayOpenWorldTrack();
                    break;
                case State.Combat:
                    _musicPlayer.PlayCombatTrack();
                    break;
                case State.CompetitiveMode:
                    _musicPlayer.PlayCompetitiveTrack();
                    break;
                case State.WorldVsWorld:
                    _musicPlayer.PlayWorldVsWorldTrack();
                    break;
                case State.StoryInstance:
                    _musicPlayer.PlayInstanceTrack();
                    break;
                case State.Submerged:
                    _musicPlayer.PlaySubmergedTrack();
                    break;
                default: break;
            }
        }

        /// <inheritdoc />
        protected override void Unload() { 
            MasterVolume.SettingChanged -= OnMasterVolumeSettingChanged;
            GameIntegration.Gw2Closed -= OnGw2Closed;
            _gw2State.StateChanged -= OnStateChanged;
            _gw2State.IsSubmergedChanged -= OnIsSubmergedChanged;
            _gw2State.TyrianTimeChanged -= OnTyrianTimeChanged;
            _gw2State.EncounterChanged -= OnEncounterChanged;
            if (_gw2State.CurrentEncounter != null)
                _gw2State.CurrentEncounter.PhaseChanged -= OnPhaseChanged;
            _gw2State.Unload();
            _musicPlayer.Dispose();
            // All static members must be manually unset
            ModuleInstance = null;
        }
    }
}
