using Blish_HUD;
using Blish_HUD.ArcDps;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Stateless;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoutubeDLSharp;
using CSCore;
using static Blish_HUD.GameService;
using CSCore.SoundOut;
using CSCore.Codecs.MP3;
using YoutubeDLSharp.Options;
using System.Text.RegularExpressions;

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


        private enum State {
            StandBy,
            Mounted,
            OpenWorld,
            Combat,
            CompetitiveMode,
            WorldVsWorld,
            StoryInstance
        }


        private enum Trigger
        {
            MapChanged,
            InCombat,
            OutOfCombat,
            Mounting,
            Unmounting
        }

        #region Settings

        private SettingEntry<float> _masterVolume;

        #endregion

        private WasapiOut _outputDevice;
        private YoutubeDL _youtubeDL;
        private OptionSet _youtubeDLOptions;
        private StateMachine<State, Trigger> _stateMachine;
        private IReadOnlyList<EncounterData> _encounterData;
        private Encounter _currentEncounter;
        private string _moduleDirectory;
        private const string _FFmpegPath = "bin/ffmpeg.exe";
        private const string _youtubeDLPath = "bin/youtube-dl.exe";
        private Regex _youtubeVideoID = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)");

        protected override void DefineSettings(SettingCollection settings) {
            _masterVolume = settings.DefineSetting("MasterVolume.", 50.0f, "Master Volume", "Sets the audio volume.");
        }


        private void ExtractFiles(string filePath) {
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


        protected override void Initialize() {

            _moduleDirectory = DirectoriesManager.GetFullDirectoryPath("music_mixer");

            _outputDevice = new WasapiOut();

            ExtractFiles(_FFmpegPath);
            ExtractFiles(_youtubeDLPath);

            _youtubeDL = new YoutubeDL();
            _youtubeDLOptions = new OptionSet()
            {
                NoContinue = true,
                Format = "best",
                NoPart = true,
                ExtractAudio = true
            };
            _youtubeDL.FFmpegPath = Path.Combine(_moduleDirectory, _FFmpegPath);
            _youtubeDL.YoutubeDLPath = Path.Combine(_moduleDirectory, _youtubeDLPath);
            _youtubeDL.OutputFolder = Directory.CreateDirectory(Path.Combine(_moduleDirectory, "cache")).FullName;

            ArcDps_OnFinishedLoading(null, null);
            Mumble_OnFinishedLoading(null, null);

            LoadEncounterData();
        }


        private async Task<string> DownloadTrack(string youtubeId, IProgress<DownloadProgress> progress = null) {
            var url = "https://youtu.be/" + youtubeId;
            var dir = Directory.CreateDirectory(Path.Combine(_youtubeDL.OutputFolder, youtubeId)).FullName;

            var result = "";
            await _youtubeDL.RunAudioDownload(url, AudioConversionFormat.Mp3, default, progress, null, _youtubeDLOptions).ContinueWith(response => {
                if (response.IsFaulted || !response.Result.Success) return;

                var filePath = response.Result.Data;
                var newPath = Path.Combine(dir, FileUtil.Sanitize(Path.GetFileName(filePath)));

                if (File.Exists(newPath)) File.Delete(newPath);
                File.Move(filePath, Path.Combine(dir, Path.GetFileName(filePath)));
            });
            return result;
        }

        private async void FetchTrack(string youtubeId) {
            var url = "https://youtu.be/" + youtubeId;
            var dir = Directory.CreateDirectory(Path.Combine(_youtubeDL.OutputFolder, youtubeId)).FullName;

            await _youtubeDL.RunVideoDataFetch(url, default, true, _youtubeDLOptions).ContinueWith(async response => {
                if (response.IsFaulted || !response.Result.Success) return;

                var result = response.Result;
                var filePath = Path.Combine(dir, Path.GetFileNameWithoutExtension(FileUtil.Sanitize(result.Data.Title)) + ".mp3");
                
                if (File.Exists(filePath)) return;
                await DownloadTrack(youtubeId);

            });
        }

        private void PlayTrack(string uri) {
            if (!FileUtil.IsLocalPath(uri)) {
                var youtubeMatch = _youtubeVideoID.Match(uri);
                if (!youtubeMatch.Success) return;

                var id = youtubeMatch.Groups[1].Value;
                var dir = Directory.CreateDirectory(Path.Combine(_youtubeDL.OutputFolder, id));

                var file = dir.GetFiles().FirstOrDefault(x => x.Extension.Equals(".mp3"));
                if (file == null) {
                    FetchTrack(id);
                    return;
                } else
                    uri = file.FullName;
            }

            if (!File.Exists(uri)) return;
            _outputDevice.Initialize(new Mp3MediafoundationDecoder(uri));
            _outputDevice.Play();
        }


        private void InitializeStateMachine() {
            _stateMachine = new StateMachine<State, Trigger>(GameModeStateSelector());

            _stateMachine.OnUnhandledTrigger((s, t) => {
                Logger.Info($"Warning: Trigger '{t}' was fired from state '{s}', but has no valid leaving transitions.");
            });
            _stateMachine.Configure(State.StandBy)
                        .PermitDynamic(Trigger.MapChanged, () => GameModeStateSelector());

            _stateMachine.Configure(State.OpenWorld)
                        .Permit(Trigger.Mounting, State.Mounted)
                        .Permit(Trigger.InCombat, State.Combat);

            _stateMachine.Configure(State.Mounted)
                        .OnEntry(() => PlayTrack("https://youtu.be/RHpn-o9n-cs"))
                        .OnExit(() => _outputDevice.Stop())
                        .PermitDynamic(Trigger.Unmounting, () => GameModeStateSelector());

            _stateMachine.Configure(State.Combat)
                        .PermitDynamicIf(Trigger.OutOfCombat, () => GameModeStateSelector());

            _stateMachine.Configure(State.CompetitiveMode)
                        .PermitDynamic(Trigger.MapChanged, () => GameModeStateSelector());

            _stateMachine.Configure(State.StoryInstance)
                        .PermitDynamic(Trigger.MapChanged, () => GameModeStateSelector())
                        .Permit(Trigger.InCombat, State.Combat);

            _stateMachine.Configure(State.WorldVsWorld)
                        .Permit(Trigger.Mounting, State.Mounted)
                        .Permit(Trigger.InCombat, State.Combat);
        }

        private void LoadEncounterData() {
            using (var fs = ContentsManager.GetFileStream("encounterData.json")) {
                fs.Position = 0;
                using (var jsonReader = new JsonTextReader(new StreamReader(fs)))
                {
                    var serializer = new JsonSerializer();
                    _encounterData = serializer.Deserialize<IReadOnlyList<EncounterData>>(jsonReader);
                }
            }
        }

        protected override void Update(GameTime gameTime) {
            if (_stateMachine != null) System.Diagnostics.Debug.WriteLine(_stateMachine.State);
            if (_outputDevice != null && _outputDevice.PlaybackState != PlaybackState.Stopped)
                _outputDevice.Volume = MathHelper.Clamp(_masterVolume.Value / 100, 0, 1);
        }

        protected override void OnModuleLoaded(EventArgs e) {
            GameIntegration.Gw2Closed += OnGw2Closed;
            GameIntegration.Gw2Started += OnGw2Started;

            // Base handler must be called
            base.OnModuleLoaded(e);
        }


        private void OnGw2Started(object sender, EventArgs e) => _outputDevice.Play();

        private void OnGw2Closed(object sender, EventArgs e) => _outputDevice.Stop();

        /// <inheritdoc />
        protected override void Unload() { 
            _outputDevice.Stop();
            _outputDevice.Dispose();
            _outputDevice = null;
            // All static members must be manually unset
            ModuleInstance = null;
        }


        #region ArcDps Events

        private void ArcDps_OnFinishedLoading(object o, EventArgs e) {
            ArcDps.Common.Activate();
            ArcDps.RawCombatEvent += CombatEventReceived;
        }


        private void CombatEventReceived(object o, RawCombatEventArgs e) {
            if (!_stateMachine.IsInState(State.Combat)) return;
            if (e.CombatEvent == null || e.CombatEvent.Dst == null) return;
            var encounterData = _encounterData.FirstOrDefault(x => x.Ids.Any(y => y.Equals(e.CombatEvent.Dst.Profession)));
            if (encounterData == null) return;

            if (_currentEncounter != null && _currentEncounter.Name.Equals(encounterData.Name) && _currentEncounter.SessionId.Equals(e.CombatEvent.Dst.Id))
                _currentEncounter.DoDamage(e.CombatEvent.Ev);
            else
                _currentEncounter = new Encounter(encounterData, e.CombatEvent.Dst.Id);
            
        }

        #endregion

        #region Mumble Events

        private void OnMountChanged(object o, ValueEventArgs<Gw2Sharp.Models.MountType> e) {
            if (e.Value.Equals(Gw2Sharp.Models.MountType.None))
                _stateMachine.Fire(Trigger.Unmounting);
            else
                _stateMachine.Fire(Trigger.Mounting);
        }


        private void OnIsInCombatChanged(object o, ValueEventArgs<bool> e) {
            _stateMachine.Fire(e.Value ? Trigger.InCombat : Trigger.OutOfCombat);
        }


        private void OnIsMapOpenChanged(object o, ValueEventArgs<bool> e) {
            //TODO: Muffle current song, lower volume. No state needed.
        }


        private void OnMapChanged(object o, ValueEventArgs<int> e) {
            _stateMachine.Fire(Trigger.MapChanged);
        }


        private void OnIsInGameChanged(object o, ValueEventArgs<bool> e) {
            //TODO: Loadingscreen, mainmenu differentation.
        }


        private void Mumble_OnFinishedLoading(object o, EventArgs e) {
            InitializeStateMachine();

            Gw2Mumble.PlayerCharacter.CurrentMountChanged += OnMountChanged;
            Gw2Mumble.PlayerCharacter.IsInCombatChanged += OnIsInCombatChanged;
            Gw2Mumble.UI.IsMapOpenChanged += OnIsMapOpenChanged;
            Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;
            GameIntegration.IsInGameChanged += OnIsInGameChanged;
        }

        #endregion

        #region State Guards
        private State GameModeStateSelector() {
            if (Gw2Mumble.PlayerCharacter.IsInCombat) return State.Combat;
            switch (Gw2Mumble.CurrentMap.Type) {
                case Gw2Sharp.Models.MapType.Unknown:
                    return State.StandBy;
                case Gw2Sharp.Models.MapType.Redirect:
                    return State.StandBy;
                case Gw2Sharp.Models.MapType.CharacterCreate:
                    return State.StandBy;
                case Gw2Sharp.Models.MapType.Pvp:
                    return State.CompetitiveMode;
                case Gw2Sharp.Models.MapType.Gvg:
                    return State.CompetitiveMode;
                case Gw2Sharp.Models.MapType.Instance:
                    return State.StoryInstance;
                case Gw2Sharp.Models.MapType.Public:
                    return State.OpenWorld;
                case Gw2Sharp.Models.MapType.Tournament:
                    return State.CompetitiveMode;
                case Gw2Sharp.Models.MapType.Tutorial:
                    return State.OpenWorld;
                case Gw2Sharp.Models.MapType.UserTournament:
                    return State.CompetitiveMode;
                case Gw2Sharp.Models.MapType.Center:
                    return State.WorldVsWorld;
                case Gw2Sharp.Models.MapType.BlueHome:
                    return State.WorldVsWorld;
                case Gw2Sharp.Models.MapType.GreenHome:
                    return State.WorldVsWorld;
                case Gw2Sharp.Models.MapType.RedHome:
                    return State.WorldVsWorld;
                case Gw2Sharp.Models.MapType.FortunesVale:
                    return State.StoryInstance;
                case Gw2Sharp.Models.MapType.JumpPuzzle:
                    return State.WorldVsWorld;
                case Gw2Sharp.Models.MapType.EdgeOfTheMists:
                    return State.WorldVsWorld;
                case Gw2Sharp.Models.MapType.PublicMini:
                    return State.OpenWorld;
                case Gw2Sharp.Models.MapType.WvwLounge:
                    return State.WorldVsWorld;
                default: return State.StandBy;
            }
        }
        #endregion

    }

}
