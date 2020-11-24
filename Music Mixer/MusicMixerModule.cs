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

        #endregion

        private MusicPlayer _musicPlayer;
        private Gw2StateService _gw2State;

        private string _moduleDirectory;

        private const string _FFmpegPath = "bin/ffmpeg.exe";
        private const string _youtubeDLPath = "bin/youtube-dl.exe";

        protected override void DefineSettings(SettingCollection settings) {
            MasterVolume = settings.DefineSetting("MasterVolume", 50.0f, "Master Volume", "Sets the audio volume.");
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

            MasterVolume.SettingChanged += OnMasterVolumeSettingChanged;
        }

        private void OnMasterVolumeSettingChanged(object o, ValueChangedEventArgs<float> e) {
            _musicPlayer.SetVolume(e.NewValue / 100);
        }

        protected override void Update(GameTime gameTime) {
            _gw2State.TyrianTime = TyrianTimeUtil.GetCurrentDayCycle();
            _gw2State.CheckWaterLevel();
        }


        protected override void OnModuleLoaded(EventArgs e) {
            _gw2State.StateChanged += OnStateChanged;
            // Base handler must be called
            base.OnModuleLoaded(e);
        }


        private void OnStateChanged(object sender, ValueChangedEventArgs<State> e) {
            /**
             * Stop or fade depending on previous state.
             */
            switch (e.PreviousValue) {
                case State.Mounted:
                    _musicPlayer.FadeOut();
                    break;
                case State.Combat:
                    _musicPlayer.FadeOut();
                    break;
                case State.Encounter:
                    _musicPlayer.FadeOut();
                    break;
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
            _gw2State.StateChanged -= OnStateChanged;
            _gw2State.Unload();
            _musicPlayer.Dispose();
            // All static members must be manually unset
            ModuleInstance = null;
        }
    }
}
