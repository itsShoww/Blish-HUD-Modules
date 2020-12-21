using Blish_HUD;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Nekres.Music_Mixer.Player;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
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

        private SettingEntry<float> MasterVolumeSetting;
        internal SettingEntry<bool> ToggleSubmergedPlaylist;
        internal SettingEntry<bool> ToggleFourDayCycle;
        internal SettingEntry<bool> ToggleKeepAudioFiles;

        #endregion

        public float MasterVolume => MathHelper.Clamp(MasterVolumeSetting.Value / 500, 0, 1);
        private MusicPlayer _musicPlayer;
        private PlaylistManager _playlistManager;
        private Gw2StateService _gw2State;

        private string _moduleDirectory;

        private const string _FFmpegPath = "bin/ffmpeg.exe";
        private const string _youtubeDLPath = "bin/youtube-dl.exe";

        internal IReadOnlyList<EncounterData> EncounterData;

        protected override void DefineSettings(SettingCollection settings) {
            MasterVolumeSetting = settings.DefineSetting("MasterVolume", 50f, "Master Volume", "Sets the audio volume.");
            ToggleSubmergedPlaylist = settings.DefineSetting("EnableSubmergedPlaylist", false, "Use submerged playlist", "Whether songs of the underwater playlist should be played while submerged.");
            ToggleFourDayCycle = settings.DefineSetting("EnableFourDayCycle", false, "Use dusk and dawn day cycles", "Whether dusk and dawn track attributes should be interpreted as unique day cycles.\nOtherwise dusk and dawn will be interpreted as night and day respectively.");
            ToggleKeepAudioFiles = settings.DefineSetting("KeepAudioFiles", false, "Keep audio files on disk", "Whether streamed audio should be kept on disk.\nReduces delay for all future playback events after the first at the expense of disk space.");
        }


        protected override void Initialize() {
            _moduleDirectory = DirectoriesManager.GetFullDirectoryPath("music_mixer");

            _gw2State = new Gw2StateService();
        }


        private void OnMasterVolumeSettingChanged(object o, ValueChangedEventArgs<float> e) {
            _musicPlayer?.SetVolume(e.NewValue / 500);
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
            _musicPlayer.PlayTrack(_playlistManager.SelectTrack());
        }


        protected override async Task LoadAsync() {
            await Task.Run(LoadEncounterData);
            await Task.Run(() => {
                ExtractFile(_FFmpegPath);
                ExtractFile(_youtubeDLPath);
                _playlistManager = new PlaylistManager(_moduleDirectory);
                _musicPlayer = new MusicPlayer(Path.Combine(_moduleDirectory, "cache"), 
                                               Path.Combine(_moduleDirectory, _FFmpegPath), 
                                               Path.Combine(_moduleDirectory, _youtubeDLPath));
                _musicPlayer.AudioEnded += OnAudioEnded;
            });
        }

        private void OnIsMapOpenChanged(object o, ValueEventArgs<bool> e) {
            if (e.Value)
                _musicPlayer.Fade(null, 0.4f * MasterVolume, TimeSpan.FromSeconds(0.45));
            else
                _musicPlayer.Fade(null, MasterVolume, TimeSpan.FromSeconds(0.45));
        }

        private void OnIsSubmergedChanged(object o, ValueEventArgs<bool> e) {
            if (ToggleSubmergedPlaylist.Value) return;
            _musicPlayer?.ToggleSubmergedFx(e.Value);
        }


        protected override void OnModuleLoaded(EventArgs e) {
            MasterVolumeSetting.Value = MathHelper.Clamp(MasterVolumeSetting.Value, 0f, 100f);
            OnStateChanged(this, new ValueChangedEventArgs<State>(0, _gw2State.CurrentState));

            MasterVolumeSetting.SettingChanged += OnMasterVolumeSettingChanged;
            GameIntegration.Gw2Closed += OnGw2Closed;
            Gw2Mumble.UI.IsMapOpenChanged += OnIsMapOpenChanged;
            _gw2State.IsSubmergedChanged += OnIsSubmergedChanged;
            _gw2State.TyrianTimeChanged += OnTyrianTimeChanged;
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

        private void PlayNext() {
            if (GameIntegration.IsInGame)
                _musicPlayer.PlayTrack(_playlistManager.SelectTrack());
        }

        private void OnAudioEnded(object o, ValueEventArgs<string> e) => PlayNext();
        private void OnPhaseChanged(object o, ValueEventArgs<int> e) => PlayNext();
        private void OnStateChanged(object sender, ValueChangedEventArgs<State> e) {
            if (_musicPlayer == null) return;

            // Fade out
            switch (e.PreviousValue) {
                case State.Mounted:
                case State.Combat:
                case State.Encounter:
                case State.Submerged:
                    break;
                default:
                    break;
            }

            // Set playlist
            switch (e.NewValue) {
                case State.Encounter:
                    _playlistManager.SetPlaylist(PlaylistManager.Playlist.Encounter);
                    break;
                case State.Mounted:
                    _playlistManager.SetPlaylist(PlaylistManager.Playlist.Mounted);
                    break;
                case State.OpenWorld:
                    _playlistManager.SetPlaylist(PlaylistManager.Playlist.OpenWorld);
                    break;
                case State.Combat:
                    _playlistManager.SetPlaylist(PlaylistManager.Playlist.Combat);
                    break;
                case State.CompetitiveMode:
                    _playlistManager.SetPlaylist(PlaylistManager.Playlist.Pvp);
                    break;
                case State.WorldVsWorld:
                    _playlistManager.SetPlaylist(PlaylistManager.Playlist.Wvw);
                    break;
                case State.StoryInstance:
                    _playlistManager.SetPlaylist(PlaylistManager.Playlist.Instance);
                    break;
                case State.Submerged:
                    _playlistManager.SetPlaylist(PlaylistManager.Playlist.Submerged);
                    break;
                default: break;
            }

            // Select track from playlist and play it.
            PlayNext();
        }

        /// <inheritdoc />
        protected override void Unload() { 
            MasterVolumeSetting.SettingChanged -= OnMasterVolumeSettingChanged;
            GameIntegration.Gw2Closed -= OnGw2Closed;
            Gw2Mumble.UI.IsMapOpenChanged -= OnIsMapOpenChanged;
            _gw2State.StateChanged -= OnStateChanged;
            _gw2State.IsSubmergedChanged -= OnIsSubmergedChanged;
            _gw2State.TyrianTimeChanged -= OnTyrianTimeChanged;
            _gw2State.EncounterChanged -= OnEncounterChanged;
            _musicPlayer.AudioEnded -= OnAudioEnded;
            if (_gw2State.CurrentEncounter != null)
                _gw2State.CurrentEncounter.PhaseChanged -= OnPhaseChanged;
            _gw2State.Unload();
            _musicPlayer?.Dispose();
            // All static members must be manually unset
            ModuleInstance = null;
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


        private void LoadEncounterData() {
            using (var fs = ContentsManager.GetFileStream("encounterData.json")) {
                fs.Position = 0;
                using (var jsonReader = new JsonTextReader(new StreamReader(fs)))
                {
                    var serializer = new JsonSerializer();
                    EncounterData = serializer.Deserialize<IReadOnlyList<EncounterData>>(jsonReader);
                }
            }
        }
    }
}
