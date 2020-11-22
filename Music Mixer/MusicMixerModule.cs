using Blish_HUD;
using Blish_HUD.ArcDps;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using CSCore.Codecs.MP3;
using CSCore.SoundOut;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Stateless;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
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

        private SettingEntry<float> _masterVolume;

        #endregion

        private MusicPlayer _musicPlayer;
        private Gw2StateService _gw2State;

        private string _moduleDirectory;

        private const string _FFmpegPath = "bin/ffmpeg.exe";
        private const string _youtubeDLPath = "bin/youtube-dl.exe";

        protected override void DefineSettings(SettingCollection settings) {
            _masterVolume = settings.DefineSetting("MasterVolume.", 50.0f, "Master Volume", "Sets the audio volume.");
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


        protected override void Update(GameTime gameTime) {
            _gw2State.TyrianTime = TyrianTimeUtil.GetCurrentDayCycle();
            _gw2State.CheckWaterLevel();
            _musicPlayer.SetVolume(_masterVolume.Value / 100);
        }


        protected override void OnModuleLoaded(EventArgs e) {
            _gw2State.StateChanged += OnStateChanged;
            // Base handler must be called
            base.OnModuleLoaded(e);
        }


        private void OnStateChanged(object sender, ValueEventArgs<State> e) {
            switch (e.Value) {
                case State.StandBy:
                    _musicPlayer.Stop();
                    break;
                case State.Mounted:
                    //PlayMountTrack();
                    break;
                case State.OpenWorld:
                    //PlayOpenWorldTrack();
                    break;
                case State.Combat:
                    //PlayCombatTrack();
                    break;
                case State.CompetitiveMode:
                    //PlayCompetitiveTrack();
                    break;
                case State.WorldVsWorld:
                    //PlayWorldVsWorldTrack();
                    break;
                case State.StoryInstance:
                    //PlayWorldVsWorldTrack();
                    break;
                case State.Submerged:
                    break;
            }
        }

        /// <inheritdoc />
        protected override void Unload() { 
            _musicPlayer.Dispose();
            _gw2State.Unload();
            // All static members must be manually unset
            ModuleInstance = null;
        }

    }

}
