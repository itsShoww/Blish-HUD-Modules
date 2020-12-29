using Blish_HUD;
using Blish_HUD.ArcDps;
using Microsoft.Xna.Framework;
using Stateless;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Blish_HUD.GameService;
using Timer = System.Timers.Timer;
namespace Nekres.Music_Mixer
{
    internal class Gw2StateService
    {
        private bool _toggleSubmergedPlaylist => MusicMixerModule.ModuleInstance.ToggleSubmergedPlaylist.Value;
        private bool _toggleMountedPlaylist => MusicMixerModule.ModuleInstance.ToggleMountedPlaylist.Value;
        private IReadOnlyList<EncounterData> _encounterData => MusicMixerModule.ModuleInstance.EncounterData;

        private static readonly Logger Logger = Logger.GetLogger(typeof(Gw2StateService));

        private Dictionary<State, Trigger> _gw2SupportedContexts = new Dictionary<State, Trigger>() {
            { State.MainMenu, Trigger.MainMenu },
            { State.Crafting, Trigger.Crafting },
            { State.Victory, Trigger.Victory },
            { State.BossBattle, Trigger.BossBattle },
        };
        private string[] _gw2SupportedPlaylistFormats = new [] {".m3u", ".asx", ".pls", ".wax", ".wpl"};
        private string _silenceWavPath = Path.Combine(MusicMixerModule.ModuleInstance.ModuleDirectory, "bin\\silence{0}.wav");
        private Dictionary<State, int> _lockCount = new Dictionary<State, int>() {
            { State.MainMenu, 0 },
            { State.Crafting, 0 },
            { State.Victory, 0 },
            { State.BossBattle, 0 },
        };

        private bool _unload;
        private int _enemyCount;

        #region _Enums

        public enum State {
            StandBy,
            Mounted,
            OpenWorld,
            Battle,
            Encounter,
            CompetitiveMode,
            WorldVsWorld,
            StoryInstance,
            Submerged,
            Downed,
            Defeated,
            Victory,
            MainMenu,
            Crafting,
            BossBattle
        }
        private enum Trigger
        {
            StandBy,
            MapChanged,
            InCombat,
            OutOfCombat,
            Mounting,
            Unmounting,
            Submerging,
            Emerging,
            EncounterPull,
            EncounterReset,
            Victory,
            Downed,
            Death,
            MainMenu,
            Crafting,
            BossBattle
        }

        #endregion

        #region _Events

        public event EventHandler<ValueEventArgs<TyrianTime>> TyrianTimeChanged;
        public event EventHandler<ValueChangedEventArgs<State>> StateChanged;
        public event EventHandler<ValueEventArgs<bool>> IsSubmergedChanged;
        public event EventHandler<ValueChangedEventArgs<Encounter>> EncounterChanged;

        #endregion

        #region _Constants

        private const int _mainMenuDelayMs = 10000;
        private const int _arcDpsDelayMs = 2500;
        private const int _enemyThreshold = 6;
        #endregion

        #region Public Fields

        private TyrianTime _prevTyrianTime = TyrianTime.None;
        public TyrianTime TyrianTime { 
            get => _prevTyrianTime;
            private set {
                if (_prevTyrianTime == value) return; 

                _prevTyrianTime = value;

                TyrianTimeChanged?.Invoke(this, new ValueEventArgs<TyrianTime>(value));
            }
        }

        private bool _prevIsSubmerged = Gw2Mumble.PlayerCharacter.Position.Z < -1.25f;
        public bool IsSubmerged {
            get => _prevIsSubmerged; 
            private set {
                if (_prevIsSubmerged == value) return; 

                _prevIsSubmerged = value;

                IsSubmergedChanged?.Invoke(this, new ValueEventArgs<bool>(value));

                _stateMachine?.Fire(value ? Trigger.Submerging : Trigger.Emerging);
            }
        }

        private Encounter _prevEncounter;
        public Encounter CurrentEncounter { 
            get => _prevEncounter; 
            private set {
                if (_prevEncounter == value) return;

                EncounterChanged?.Invoke(this, new ValueChangedEventArgs<Encounter>(_prevEncounter, value));

                _prevEncounter = value;

                _stateMachine?.Fire(value != null ? Trigger.EncounterPull : Trigger.EncounterReset);
            }
        }

        public State CurrentState => _stateMachine?.State ?? State.StandBy;

        #endregion

        private StateMachine<State, Trigger> _stateMachine;
        private Timer _arcDpsTimeOut;

        public Gw2StateService() {
            _arcDpsTimeOut = new Timer(_arcDpsDelayMs){ AutoReset = false };
            _arcDpsTimeOut.Elapsed += (o, e) => ArcDps.RawCombatEvent += CombatEventReceived;

            InitializeStateMachine();
        }

        private void CreatePseudoNativePlaylists() {
            // Backup existing native gw2 playlists.
            var backupDir = Directory.CreateDirectory(Path.Combine(DirectoryUtil.MusicPath, "%DISABLED%")).FullName;
            if (Directory.Exists(backupDir)) {
                var files = new DirectoryInfo(DirectoryUtil.MusicPath).GetFiles();
                foreach (var file in files) {
                    if (!_gw2SupportedPlaylistFormats.Any(x => x.ToLower().Equals(file.Extension))) continue;
                    try {
                        file.MoveTo(Path.Combine(backupDir, file.Name));
                    } catch (IOException) {} // Target file already exists. We don't want to overwrite an existing backup.
                }
            }
            // Create pseudo playlists with 1 second silence.wav for detecting states.
            var silenceDir = Directory.CreateDirectory(Path.Combine(MusicMixerModule.ModuleInstance.ModuleDirectory, "bin/silence")).FullName;
            foreach (var ctx in _gw2SupportedContexts) {
                var wavFileName = string.Format(Path.GetFileName(_silenceWavPath), ctx.Key);
                // Making sure there are no other "{0}" in the path.
                try {
                    File.Copy(Path.Combine(Path.GetDirectoryName(_silenceWavPath), string.Format(Path.GetFileName(_silenceWavPath), "")), Path.Combine(silenceDir, wavFileName));
                } catch (IOException) {} // Target file exists. Just skip operation.
                File.WriteAllText(Path.Combine(DirectoryUtil.MusicPath, ctx.Key + ".m3u"), Path.Combine(silenceDir, wavFileName)+Environment.NewLine, Encoding.Default);
            }
        }

        public void Unload() {
            _unload = true;
            _arcDpsTimeOut?.Dispose();
            GameIntegration.Gw2Closed -= OnGw2Closed;
            ArcDps.RawCombatEvent -= CombatEventReceived;
            Gw2Mumble.PlayerCharacter.CurrentMountChanged -= OnMountChanged;
            Gw2Mumble.CurrentMap.MapChanged -= OnMapChanged;
            Gw2Mumble.PlayerCharacter.IsInCombatChanged -= OnIsInCombatChanged;
        }


        private async void InitializeStateMachine() {
            await Task.Run(() => {
                _stateMachine = new StateMachine<State, Trigger>(GameModeStateSelector());

                _stateMachine.OnUnhandledTrigger((s, t) => {
                    switch (t) {
                        case Trigger.Mounting: 
                        case Trigger.Unmounting:
                            if (!_toggleMountedPlaylist) return; break;
                        case Trigger.Submerging: 
                        case Trigger.Emerging:
                            if (!_toggleSubmergedPlaylist) return; break;
                        default: break;
                    }
                    Logger.Info($"Warning: Trigger '{t}' was fired from state '{s}', but has no valid leaving transitions.");
                });
                _stateMachine.Configure(State.StandBy)
                            .Ignore(Trigger.StandBy)
                            .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                            .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                            .PermitDynamic(Trigger.OutOfCombat, GameModeStateSelector)
                            .PermitIf(Trigger.Submerging, State.Submerged, () => _toggleSubmergedPlaylist)
                            .PermitIf(Trigger.Mounting, State.Mounted, () => _toggleMountedPlaylist)
                            .Permit(Trigger.BossBattle, State.BossBattle)
                            .Permit(Trigger.Victory, State.Victory)
                            .Permit(Trigger.MainMenu, State.MainMenu)
                            .Permit(Trigger.InCombat, State.Battle)
                            .Permit(Trigger.Death, State.Defeated)
                            .Permit(Trigger.Crafting, State.Crafting)
                            .Permit(Trigger.EncounterPull, State.Encounter)
                            .Ignore(Trigger.Submerging)
                            .Ignore(Trigger.Emerging)
                            .Ignore(Trigger.EncounterReset);

                _stateMachine.Configure(State.OpenWorld)
                            .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                            .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                            .PermitDynamic(Trigger.StandBy, GameModeStateSelector)
                            .PermitIf(Trigger.Submerging, State.Submerged, () => _toggleSubmergedPlaylist)
                            .PermitIf(Trigger.Mounting, State.Mounted, () => _toggleMountedPlaylist)
                            .Permit(Trigger.MainMenu, State.MainMenu)
                            .Permit(Trigger.Victory, State.Victory)
                            .Permit(Trigger.BossBattle, State.BossBattle)
                            .Permit(Trigger.InCombat, State.Battle)
                            .Permit(Trigger.Downed, State.Downed)
                            .Permit(Trigger.Death, State.Defeated)
                            .Permit(Trigger.Crafting, State.Crafting)
                            .Permit(Trigger.EncounterPull, State.Encounter)
                            .Ignore(Trigger.Emerging)
                            .Ignore(Trigger.EncounterReset)
                            .Ignore(Trigger.OutOfCombat);

                _stateMachine.Configure(State.Mounted)
                            .Ignore(Trigger.Mounting)
                            .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                            .PermitDynamic(Trigger.StandBy, GameModeStateSelector)
                            .Permit(Trigger.MainMenu, State.MainMenu)
                            .Permit(Trigger.Victory, State.Victory)
                            .Permit(Trigger.BossBattle, State.BossBattle)
                            .Permit(Trigger.Downed, State.Downed)
                            .Permit(Trigger.Death, State.Defeated)
                            .PermitDynamic(Trigger.Unmounting, GameModeStateSelector)
                            .Permit(Trigger.EncounterPull, State.Encounter)
                            .Permit(Trigger.Crafting, State.Crafting)
                            .Ignore(Trigger.Submerging)
                            .Ignore(Trigger.Emerging)
                            .Ignore(Trigger.EncounterReset)
                            .Ignore(Trigger.OutOfCombat)
                            .Ignore(Trigger.MapChanged);

                _stateMachine.Configure(State.Battle)
                            .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                            .PermitDynamic(Trigger.StandBy, GameModeStateSelector)
                            .Permit(Trigger.MainMenu, State.MainMenu)
                            .Permit(Trigger.Victory, State.Victory)
                            .Permit(Trigger.BossBattle, State.BossBattle)
                            .Permit(Trigger.Downed, State.Downed)
                            .Permit(Trigger.Death, State.Defeated)
                            .PermitDynamic(Trigger.OutOfCombat, GameModeStateSelector)
                            .Permit(Trigger.EncounterPull, State.Encounter)
                            .Ignore(Trigger.Submerging)
                            .Ignore(Trigger.Emerging)
                            .Ignore(Trigger.EncounterReset)
                            .Ignore(Trigger.InCombat);

                _stateMachine.Configure(State.Encounter)
                            .Ignore(Trigger.EncounterPull)
                            .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                            .PermitDynamic(Trigger.StandBy, GameModeStateSelector)
                            .Permit(Trigger.MainMenu, State.MainMenu)
                            .Permit(Trigger.Victory, State.Victory)
                            .Permit(Trigger.Downed, State.Downed)
                            .Permit(Trigger.BossBattle, State.BossBattle)
                            .PermitDynamic(Trigger.EncounterReset, GameModeStateSelector)
                            .Ignore(Trigger.OutOfCombat)
                            .Ignore(Trigger.Emerging);

                _stateMachine.Configure(State.CompetitiveMode)
                            .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                            .PermitDynamic(Trigger.StandBy, GameModeStateSelector)
                            .Permit(Trigger.MainMenu, State.MainMenu)
                            .Permit(Trigger.Victory, State.Victory)
                            .Permit(Trigger.BossBattle, State.BossBattle)
                            .Permit(Trigger.Downed, State.Downed)
                            .Permit(Trigger.Death, State.Defeated)
                            .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                            .Permit(Trigger.EncounterPull, State.Encounter)
                            .Ignore(Trigger.Submerging)
                            .Ignore(Trigger.Emerging)
                            .Ignore(Trigger.EncounterReset)
                            .Ignore(Trigger.OutOfCombat);

                _stateMachine.Configure(State.StoryInstance)
                            .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                            .PermitDynamic(Trigger.StandBy, GameModeStateSelector)
                            .Permit(Trigger.MainMenu, State.MainMenu)
                            .Permit(Trigger.Victory, State.Victory)
                            .Permit(Trigger.BossBattle, State.BossBattle)
                            .Permit(Trigger.Downed, State.Downed)
                            .Permit(Trigger.Death, State.Defeated)
                            .Permit(Trigger.Crafting, State.Crafting)
                            .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                            .PermitIf(Trigger.Submerging, State.Submerged, () => _toggleSubmergedPlaylist)
                            .Permit(Trigger.EncounterPull, State.Encounter)
                            .Permit(Trigger.InCombat, State.Battle)
                            .Ignore(Trigger.Emerging)
                            .Ignore(Trigger.EncounterReset)
                            .Ignore(Trigger.OutOfCombat);

                _stateMachine.Configure(State.WorldVsWorld)
                            .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                            .PermitDynamic(Trigger.StandBy, GameModeStateSelector)
                            .Permit(Trigger.MainMenu, State.MainMenu)
                            .Permit(Trigger.Victory, State.Victory)
                            .Permit(Trigger.BossBattle, State.BossBattle)
                            .Permit(Trigger.Downed, State.Downed)
                            .Permit(Trigger.Death, State.Defeated)
                            .Permit(Trigger.Crafting, State.Crafting)
                            .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                            .PermitIf(Trigger.Submerging, State.Submerged, () => _toggleSubmergedPlaylist)
                            .PermitIf(Trigger.Mounting, State.Mounted, () => _toggleMountedPlaylist)
                            .Permit(Trigger.EncounterPull, State.Encounter)
                            .Permit(Trigger.InCombat, State.Battle)
                            .Ignore(Trigger.Emerging)
                            .Ignore(Trigger.EncounterReset)
                            .Ignore(Trigger.OutOfCombat);

                _stateMachine.Configure(State.Submerged)
                            .Ignore(Trigger.Submerging)
                            .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                            .PermitDynamic(Trigger.Emerging, GameModeStateSelector)
                            .PermitDynamic(Trigger.StandBy, GameModeStateSelector)
                            .Permit(Trigger.MainMenu, State.MainMenu)
                            .Permit(Trigger.Victory, State.Victory)
                            .Permit(Trigger.BossBattle, State.BossBattle)
                            .Permit(Trigger.Downed, State.Downed)
                            .Permit(Trigger.Death, State.Defeated)
                            .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                            .PermitIf(Trigger.Mounting, State.Mounted, () => _toggleMountedPlaylist)
                            .Permit(Trigger.EncounterPull, State.Encounter)
                            .Permit(Trigger.InCombat, State.Battle)
                            .Ignore(Trigger.EncounterReset)
                            .Ignore(Trigger.OutOfCombat);

                _stateMachine.Configure(State.Victory)
                            .Ignore(Trigger.Victory)
                            .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                            .PermitDynamic(Trigger.StandBy, GameModeStateSelector)
                            .Permit(Trigger.MainMenu, State.MainMenu)
                            .Permit(Trigger.BossBattle, State.BossBattle)
                            .Permit(Trigger.Downed, State.Downed)
                            .Permit(Trigger.Death, State.Defeated)
                            .Permit(Trigger.Crafting, State.Crafting);

                _stateMachine.Configure(State.MainMenu)
                            .Ignore(Trigger.MainMenu)
                            .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                            .PermitDynamic(Trigger.StandBy, GameModeStateSelector)
                            .Permit(Trigger.Victory, State.Victory)
                            .Permit(Trigger.BossBattle, State.BossBattle)
                            .Permit(Trigger.Downed, State.Downed)
                            .Permit(Trigger.Death, State.Defeated)
                            .Permit(Trigger.Crafting, State.Crafting);

                _stateMachine.Configure(State.BossBattle)
                            .Ignore(Trigger.BossBattle)
                            .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                            .PermitDynamic(Trigger.StandBy, GameModeStateSelector)
                            .Permit(Trigger.MainMenu, State.MainMenu)
                            .Permit(Trigger.Victory, State.Victory)
                            .Permit(Trigger.Downed, State.Downed)
                            .Permit(Trigger.Death, State.Defeated);

                _stateMachine.Configure(State.Defeated)
                            .Ignore(Trigger.Death)
                            .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                            .PermitDynamic(Trigger.StandBy, GameModeStateSelector)
                            .Permit(Trigger.MainMenu, State.MainMenu)
                            .Permit(Trigger.Victory, State.Victory)
                            .Permit(Trigger.BossBattle, State.BossBattle)
                            .Permit(Trigger.Downed, State.Downed)
                            .Permit(Trigger.Crafting, State.Crafting);

                _stateMachine.Configure(State.Crafting)
                            .Ignore(Trigger.Crafting)
                            .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                            .PermitDynamic(Trigger.StandBy, GameModeStateSelector)
                            .Permit(Trigger.MainMenu, State.MainMenu)
                            .Permit(Trigger.Victory, State.Victory)
                            .Permit(Trigger.BossBattle, State.BossBattle)
                            .Permit(Trigger.Downed, State.Downed)
                            .Permit(Trigger.Death, State.Defeated);

                _stateMachine.Configure(State.Downed)
                            .Ignore(Trigger.Downed)
                            .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                            .PermitDynamic(Trigger.StandBy, GameModeStateSelector)
                            .Permit(Trigger.MainMenu, State.MainMenu)
                            .Permit(Trigger.Victory, State.Victory)
                            .Permit(Trigger.BossBattle, State.BossBattle)
                            .Permit(Trigger.Death, State.Defeated);

                ArcDps.Common.Activate();
                ArcDps.RawCombatEvent += CombatEventReceived;
                Gw2Mumble.PlayerCharacter.CurrentMountChanged += OnMountChanged;
                Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;
                Gw2Mumble.PlayerCharacter.IsInCombatChanged += OnIsInCombatChanged;
                GameIntegration.Gw2Closed += OnGw2Closed;

                CreatePseudoNativePlaylists();
                var updateTask = new Task(() => {
                    while (!_unload) {
                        Update(null);
                    }
                });
                updateTask.Start();
            });
        }

        public void Update(GameTime gameTime) {
            CheckTyrianTime();
            CheckWaterLevel();
            CheckEncounterReset();
            CheckNativeStates();
        }
        private void CheckWaterLevel() => IsSubmerged = Gw2Mumble.PlayerCharacter.Position.Z < -1.25f;
        private void CheckTyrianTime() => TyrianTime = TyrianTimeUtil.GetCurrentDayCycle();
        private void CheckEncounterReset() {
            if (!ArcDps.Loaded || CurrentEncounter == null) return;
            if (CurrentEncounter.IsPlayerReset() || CurrentEncounter.IsDead)
                TimeOutCombatEvents();
        }
        private void CheckNativeStates() {
            foreach (var ctx in _gw2SupportedContexts) {
                var wavPath = Path.Combine(Path.GetDirectoryName(_silenceWavPath), "silence\\"+string.Format(Path.GetFileName(_silenceWavPath), ctx.Key));
                if (_stateMachine.IsInState(ctx.Key) && !File.Exists(wavPath))
                    _stateMachine.Fire(Trigger.StandBy);

                var lockedByGw2 = FileUtil.WhoIsLocking(wavPath).Any(x => x.Id.Equals(GameIntegration.Gw2Process.Id));
                var lockCount = _lockCount[ctx.Key];
                if (lockedByGw2 && lockCount < 5) {
                    lockCount++;
                } else if (lockCount > 0) {
                    lockCount--;
                }

                _lockCount[ctx.Key] = lockCount;

                if (!_stateMachine.IsInState(ctx.Key) && lockCount == 5) {
                    _stateMachine.Fire(ctx.Value);
                } else if (_stateMachine.IsInState(ctx.Key) && lockCount == 0) {
                    _stateMachine.Fire(Trigger.StandBy);
                }
            }
        }

        private void OnGw2Closed(object sender, EventArgs e) => _stateMachine.Fire(Trigger.StandBy);

        #region ArcDps Events

        private void TimeOutCombatEvents() {
            ArcDps.RawCombatEvent -= CombatEventReceived;
            CurrentEncounter = null;
            _arcDpsTimeOut.Restart();
        }

        private void CombatEventReceived(object o, RawCombatEventArgs e) {
            if (_encounterData == null ||
                e.CombatEvent == null || 
                e.CombatEvent.Ev == null || 
                e.EventType == RawCombatEventArgs.CombatEventType.Local) return;

            var ev = e.CombatEvent.Ev;
            if (e.CombatEvent.Src.Self > 0) {
                switch (ev.IsStateChange) {
                    case ArcDpsEnums.StateChange.ChangeDead:
                        _stateMachine.Fire(Trigger.Death);
                        return;
                    case ArcDpsEnums.StateChange.ChangeDown:
                        _stateMachine.Fire(Trigger.Downed);
                        return;
                    case ArcDpsEnums.StateChange.ChangeUp:
                        _stateMachine.Fire(Trigger.StandBy);
                        return;
                    case ArcDpsEnums.StateChange.Reward:
                        _stateMachine.Fire(Trigger.Victory);
                        return;
                    default: break;
                }
            } else if (e.CombatEvent.Dst.Self > 0) {
                if (ev.Iff == ArcDpsEnums.IFF.Foe) {
                    if (_enemyCount < _enemyThreshold) {
                        _enemyCount++;
                        if (_enemyCount == _enemyThreshold) _stateMachine.Fire(Trigger.InCombat);
                    }
                }
            }

            var dstProf = e.CombatEvent.Dst?.Profession ?? 0;

            if (CurrentEncounter == null) {

                var srcProf = e.CombatEvent.Src?.Profession ?? 0;

                var encounterData = 
                    _encounterData.FirstOrDefault(x => x.Ids.Any(y => y.Equals(srcProf) || y.Equals(dstProf)));

                if (encounterData == null) return;

                CurrentEncounter = new Encounter(encounterData);

            } else if (CurrentEncounter.Ids.Any(x => x.Equals(dstProf))) {
                CurrentEncounter.CheckPhase(e);
            }
        }

        #endregion

        #region Mumble Events

        private void OnMountChanged(object o, ValueEventArgs<Gw2Sharp.Models.MountType> e) => _stateMachine.Fire(e.Value > 0 ? Trigger.Mounting : Trigger.Unmounting);
        private void OnMapChanged(object o, ValueEventArgs<int> e) => _stateMachine.Fire(Trigger.MapChanged);
        private void OnIsInCombatChanged(object o, ValueEventArgs<bool> e) {
            if (!e.Value) {
                _enemyCount = 0;
                _stateMachine.Fire(Trigger.OutOfCombat);
            }
        }
        #endregion

        #region State Guards

        private State GameModeStateSelector() {
            if (Gw2Mumble.PlayerCharacter.CurrentMount > 0)
                return State.Mounted;
            if (_toggleSubmergedPlaylist && _prevIsSubmerged) 
                return State.Submerged;
            switch (Gw2Mumble.CurrentMap.Type) {
                case Gw2Sharp.Models.MapType.Pvp:
                case Gw2Sharp.Models.MapType.Gvg:
                case Gw2Sharp.Models.MapType.Tournament:
                case Gw2Sharp.Models.MapType.UserTournament:
                    return State.CompetitiveMode;
                case Gw2Sharp.Models.MapType.Instance:
                case Gw2Sharp.Models.MapType.FortunesVale:
                    return State.StoryInstance;
                case Gw2Sharp.Models.MapType.Tutorial:
                case Gw2Sharp.Models.MapType.Public:
                case Gw2Sharp.Models.MapType.PublicMini:
                    return State.OpenWorld;
                case Gw2Sharp.Models.MapType.Center:
                case Gw2Sharp.Models.MapType.BlueHome:
                case Gw2Sharp.Models.MapType.GreenHome:
                case Gw2Sharp.Models.MapType.RedHome:
                case Gw2Sharp.Models.MapType.JumpPuzzle:
                case Gw2Sharp.Models.MapType.EdgeOfTheMists:
                case Gw2Sharp.Models.MapType.WvwLounge:
                    return State.WorldVsWorld;
                default:
                    return State.StandBy;
            }
        }

        #endregion
    }
}
