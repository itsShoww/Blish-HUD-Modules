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
        internal SettingEntry<bool> ToggleMountedPlaylist;
        internal SettingEntry<bool> ToggleFourDayCycle;
        internal SettingEntry<bool> ToggleKeepAudioFiles;

        #endregion

        public float MasterVolume => MathHelper.Clamp(MasterVolumeSetting.Value / 1000f, 0, 1);
        private MusicPlayer _musicPlayer;
        private PlaylistManager _playlistManager;
        private Gw2StateService _gw2State;

        public string ModuleDirectory { get; private set; }

        private const string _FFmpegPath = "bin/ffmpeg.exe";
        private const string _youtubeDLPath = "bin/youtube-dl.exe";
        private const string _silenceWavPath = "bin/silence.wav";

        internal IReadOnlyList<EncounterData> EncounterData;

        protected override void DefineSettings(SettingCollection settings) {
            MasterVolumeSetting = settings.DefineSetting("MasterVolume", 50f, "Master Volume", "Sets the audio volume.");
            ToggleSubmergedPlaylist = settings.DefineSetting("EnableSubmergedPlaylist", false, "Use submerged playlist", "Whether songs of the underwater playlist should be played while submerged.");
            ToggleMountedPlaylist = settings.DefineSetting("EnableMountedPlaylist", true, "Use mounted playlist", "Whether songs of the mounted playlist should be played while mounted.");
            ToggleFourDayCycle = settings.DefineSetting("EnableFourDayCycle", false, "Use dusk and dawn day cycles", "Whether dusk and dawn track attributes should be interpreted as unique day cycles.\nOtherwise dusk and dawn will be interpreted as night and day respectively.");
            ToggleKeepAudioFiles = settings.DefineSetting("KeepAudioFiles", false, "Keep audio files on disk", "Whether streamed audio should be kept on disk.\nReduces delay for all future playback events after the first at the expense of disk space.\nMay also result in better audio quality.");
        }

        protected override void Initialize() {
            ModuleDirectory = DirectoriesManager.GetFullDirectoryPath("music_mixer");
            _gw2State = new Gw2StateService();
        }


        private void OnMasterVolumeSettingChanged(object o, ValueChangedEventArgs<float> e) {
            _musicPlayer?.SetVolume(e.NewValue / 1000f);
        }


        /*protected override void Update(GameTime gameTime) {
        }*/


        private void OnGw2Closed(object sender, EventArgs e) {
            _musicPlayer?.Stop();
        }


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
                ExtractFile(_silenceWavPath);
                _playlistManager = new PlaylistManager(ModuleDirectory);
                _musicPlayer = new MusicPlayer(Path.Combine(ModuleDirectory, "cache"));
                _musicPlayer.AudioEnded += OnAudioEnded;
            });
        }

        private void OnIsMapOpenChanged(object o, ValueEventArgs<bool> e) {
            if (e.Value)
                _musicPlayer.Fade(null, 0.5f, TimeSpan.FromSeconds(0.45));
            else
                _musicPlayer.Fade(null, 1, TimeSpan.FromSeconds(0.45));
        }

        private void OnIsSubmergedChanged(object o, ValueEventArgs<bool> e) {
            if (ToggleSubmergedPlaylist.Value) return;
            _musicPlayer?.ToggleSubmergedFx(e.Value);
        }

        private void OnIsDownedChanged(object o, ValueEventArgs<bool> e) {
            _musicPlayer?.ToggleSubmergedFx(e.Value);
        }

        protected override void OnModuleLoaded(EventArgs e) {
            MasterVolumeSetting.Value = MathHelper.Clamp(MasterVolumeSetting.Value, 0f, 100f);
            OnStateChanged(_gw2State, new ValueChangedEventArgs<State>(_gw2State.CurrentState,_gw2State.CurrentState));

            MasterVolumeSetting.SettingChanged += OnMasterVolumeSettingChanged;
            GameIntegration.Gw2Closed += OnGw2Closed;
            Gw2Mumble.UI.IsMapOpenChanged += OnIsMapOpenChanged;
            _gw2State.IsSubmergedChanged += OnIsSubmergedChanged;
            _gw2State.IsDownedChanged += OnIsDownedChanged;
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
            _musicPlayer.PlayTrack(_playlistManager.SelectTrack());
        }

        private void OnAudioEnded(object o, ValueEventArgs<string> e) => PlayNext();
        private void OnPhaseChanged(object o, ValueEventArgs<int> e) => PlayNext();
        private void OnStateChanged(object sender, ValueChangedEventArgs<State> e) {
            if (_musicPlayer == null) return;

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
                case State.Battle:
                    _playlistManager.SetPlaylist(PlaylistManager.Playlist.Battle);
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
                case State.Defeated:
                    _playlistManager.SetPlaylist(PlaylistManager.Playlist.Defeated);
                    break;
                case State.Victory:
                    _playlistManager.SetPlaylist(PlaylistManager.Playlist.Victory);
                    break;
                case State.MainMenu:
                    _playlistManager.SetPlaylist(PlaylistManager.Playlist.MainMenu);
                    break;
                case State.Crafting:
                    _playlistManager.SetPlaylist(PlaylistManager.Playlist.Crafting);
                    break;
                case State.BossBattle:
                    _playlistManager.SetPlaylist(PlaylistManager.Playlist.BossBattle);
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
            _gw2State.IsDownedChanged -= OnIsDownedChanged;
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
            var fullPath = Path.Combine(ModuleDirectory, filePath);
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
