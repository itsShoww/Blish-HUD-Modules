using Blish_HUD;
using Blish_HUD.ArcDps;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using static Blish_HUD.GameService;

namespace Nekres.Music_Mixer
{
    internal class Gw2StateService
    {
        private bool _toggleSubmergedPlaylist => MusicMixerModule.ModuleInstance.ToggleSubmergedPlaylist.Value;
        private bool _toggleMountedPlaylist => MusicMixerModule.ModuleInstance.ToggleMountedPlaylist.Value;
        private IReadOnlyList<EncounterData> _encounterData => MusicMixerModule.ModuleInstance.EncounterData;

        private static readonly Logger Logger = Logger.GetLogger(typeof(Gw2StateService));

        #region _Enums

        public enum State {
            StandBy,
            Mounted,
            OpenWorld,
            Combat,
            Encounter,
            CompetitiveMode,
            WorldVsWorld,
            StoryInstance,
            Submerged
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
            Death
        }

        #endregion

        #region _Events

        public event EventHandler<ValueEventArgs<TyrianTime>> TyrianTimeChanged;
        public event EventHandler<ValueChangedEventArgs<State>> StateChanged;
        public event EventHandler<ValueEventArgs<bool>> IsSubmergedChanged;
        public event EventHandler<ValueChangedEventArgs<Encounter>> EncounterChanged;

        #endregion

        #region _Constants

        private const int _combatDelayMs = 10000;
        private const int _arcDpsDelayMs = 2500;

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
        private Timer _combatDelay;
        private Timer _arcDpsTimeOut;

        public Gw2StateService() {
            _combatDelay = new Timer(_combatDelayMs){ AutoReset = false };
            _combatDelay.Elapsed += (o, e) => {
                if (CurrentEncounter != null) return;
                _stateMachine.Fire(Gw2Mumble.PlayerCharacter.IsInCombat ? Trigger.InCombat : Trigger.OutOfCombat);
            };

            _arcDpsTimeOut = new Timer(_arcDpsDelayMs){ AutoReset = false };
            _arcDpsTimeOut.Elapsed += (o, e) => ArcDps.RawCombatEvent += CombatEventReceived;

            InitializeStateMachine();

            ArcDps.Common.Activate();
            ArcDps.RawCombatEvent += CombatEventReceived;
            Gw2Mumble.PlayerCharacter.CurrentMountChanged += OnMountChanged;
            Gw2Mumble.PlayerCharacter.IsInCombatChanged += OnIsInCombatChanged;
            Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;

            GameIntegration.Gw2Closed += OnGw2Closed;
        }


        public void Unload() {
            _combatDelay?.Dispose();
            _arcDpsTimeOut?.Dispose();
            GameIntegration.Gw2Closed -= OnGw2Closed;
            ArcDps.RawCombatEvent -= CombatEventReceived;
            Gw2Mumble.PlayerCharacter.CurrentMountChanged -= OnMountChanged;
            Gw2Mumble.PlayerCharacter.IsInCombatChanged -= OnIsInCombatChanged;
            Gw2Mumble.CurrentMap.MapChanged -= OnMapChanged;
        }


        private void InitializeStateMachine() {
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
                        .Permit(Trigger.InCombat, State.Combat)
                        .Permit(Trigger.EncounterPull, State.Encounter)
                        .Ignore(Trigger.Submerging)
                        .Ignore(Trigger.Emerging)
                        .Ignore(Trigger.EncounterReset);

            _stateMachine.Configure(State.OpenWorld)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                        .Permit(Trigger.StandBy, State.StandBy)
                        .PermitIf(Trigger.Submerging, State.Submerged, () => _toggleSubmergedPlaylist)
                        .PermitIf(Trigger.Mounting, State.Mounted, () => _toggleMountedPlaylist)
                        .Permit(Trigger.InCombat, State.Combat)
                        .Permit(Trigger.EncounterPull, State.Encounter)
                        .Ignore(Trigger.Emerging)
                        .Ignore(Trigger.EncounterReset)
                        .Ignore(Trigger.OutOfCombat);

            _stateMachine.Configure(State.Mounted)
                        .Ignore(Trigger.Mounting)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .Permit(Trigger.StandBy, State.StandBy)
                        .PermitDynamic(Trigger.Unmounting, GameModeStateSelector)
                        .Permit(Trigger.EncounterPull, State.Encounter)
                        .Ignore(Trigger.Submerging)
                        .Ignore(Trigger.Emerging)
                        .Ignore(Trigger.EncounterReset)
                        .Ignore(Trigger.OutOfCombat)
                        .Ignore(Trigger.MapChanged);

            _stateMachine.Configure(State.Combat)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .Permit(Trigger.StandBy, State.StandBy)
                        .PermitDynamic(Trigger.OutOfCombat, GameModeStateSelector)
                        .Permit(Trigger.EncounterPull, State.Encounter)
                        .Ignore(Trigger.Submerging)
                        .Ignore(Trigger.Emerging)
                        .Ignore(Trigger.EncounterReset)
                        .Ignore(Trigger.InCombat);

            _stateMachine.Configure(State.Encounter)
                        .Ignore(Trigger.EncounterPull)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .Permit(Trigger.StandBy, State.StandBy)
                        .PermitDynamic(Trigger.EncounterReset, GameModeStateSelector)
                        .Ignore(Trigger.OutOfCombat)
                        .Ignore(Trigger.Emerging);

            _stateMachine.Configure(State.CompetitiveMode)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .Permit(Trigger.StandBy, State.StandBy)
                        .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                        .Permit(Trigger.EncounterPull, State.Encounter)
                        .Ignore(Trigger.Submerging)
                        .Ignore(Trigger.Emerging)
                        .Ignore(Trigger.EncounterReset)
                        .Ignore(Trigger.OutOfCombat);

            _stateMachine.Configure(State.StoryInstance)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .Permit(Trigger.StandBy, State.StandBy)
                        .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                        .PermitIf(Trigger.Submerging, State.Submerged, () => _toggleSubmergedPlaylist)
                        .Permit(Trigger.EncounterPull, State.Encounter)
                        .Permit(Trigger.InCombat, State.Combat)
                        .Ignore(Trigger.Emerging)
                        .Ignore(Trigger.EncounterReset)
                        .Ignore(Trigger.OutOfCombat);

            _stateMachine.Configure(State.WorldVsWorld)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .Permit(Trigger.StandBy, State.StandBy)
                        .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                        .PermitIf(Trigger.Submerging, State.Submerged, () => _toggleSubmergedPlaylist)
                        .PermitIf(Trigger.Mounting, State.Mounted, () => _toggleMountedPlaylist)
                        .Permit(Trigger.EncounterPull, State.Encounter)
                        .Permit(Trigger.InCombat, State.Combat)
                        .Ignore(Trigger.Emerging)
                        .Ignore(Trigger.EncounterReset)
                        .Ignore(Trigger.OutOfCombat);

            _stateMachine.Configure(State.Submerged)
                        .Ignore(Trigger.Submerging)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .PermitDynamic(Trigger.Emerging, GameModeStateSelector)
                        .Permit(Trigger.StandBy, State.StandBy)
                        .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                        .PermitIf(Trigger.Mounting, State.Mounted, () => _toggleMountedPlaylist)
                        .Permit(Trigger.EncounterPull, State.Encounter)
                        .Permit(Trigger.InCombat, State.Combat)
                        .Ignore(Trigger.EncounterReset)
                        .Ignore(Trigger.OutOfCombat);
        }

        public void CheckWaterLevel() => IsSubmerged = Gw2Mumble.PlayerCharacter.Position.Z < -1.25f;
        public void CheckTyrianTime() => TyrianTime = TyrianTimeUtil.GetCurrentDayCycle();
        public void CheckEncounterReset() {
            if (!ArcDps.Loaded || CurrentEncounter == null) return;
            if (CurrentEncounter.IsPlayerReset() || CurrentEncounter.IsDead)
                TimeOutCombatEvents();
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

            //TODO: StateChange.Reward for Victory fanfare!

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

        private void OnIsInCombatChanged(object o, ValueEventArgs<bool> e) {
            _combatDelay.Restart();
        }

        private void OnMountChanged(object o, ValueEventArgs<Gw2Sharp.Models.MountType> e) => _stateMachine.Fire(e.Value > 0 ? Trigger.Mounting : Trigger.Unmounting);
        private void OnMapChanged(object o, ValueEventArgs<int> e) => _stateMachine.Fire(Trigger.MapChanged);

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
