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
            EncounterPull
        }

        #endregion

        #region Events

        public event EventHandler<ValueEventArgs<TyrianTime>> TyrianTimeChanged;
        public event EventHandler<ValueChangedEventArgs<State>> StateChanged;
        public event EventHandler<ValueEventArgs<bool>> IsSubmergedChanged;

        #endregion

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

        private StateMachine<State, Trigger> _stateMachine;

        private IReadOnlyList<EncounterData> _encounterData;

        private Timer _combatDelay;
        private int _combatDelayMs = 5000;

        public Encounter CurrentEncounter;
        public State CurrentState => _stateMachine?.State ?? State.StandBy;

        public Gw2StateService(IReadOnlyList<EncounterData> encounterData) {
            _encounterData = encounterData;
            InitializeStateMachine();

            ArcDps.Common.Activate();
            ArcDps.RawCombatEvent += CombatEventReceived;
            Gw2Mumble.PlayerCharacter.CurrentMountChanged += OnMountChanged;
            Gw2Mumble.PlayerCharacter.IsInCombatChanged += OnIsInCombatChanged;
            Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;
            GameIntegration.IsInGameChanged += OnIsInGameChanged;

            GameIntegration.Gw2Closed += OnGw2Closed;
        }


        public void Unload() {
            GameIntegration.Gw2Closed -= OnGw2Closed;
            ArcDps.RawCombatEvent -= CombatEventReceived;
            Gw2Mumble.PlayerCharacter.CurrentMountChanged -= OnMountChanged;
            Gw2Mumble.PlayerCharacter.IsInCombatChanged -= OnIsInCombatChanged;
            Gw2Mumble.CurrentMap.MapChanged -= OnMapChanged;
            GameIntegration.IsInGameChanged -= OnIsInGameChanged;
        }


        private void InitializeStateMachine() {
            _stateMachine = new StateMachine<State, Trigger>(GameModeStateSelector());

            _stateMachine.OnUnhandledTrigger((s, t) => {
                Logger.Info($"Warning: Trigger '{t}' was fired from state '{s}', but has no valid leaving transitions.");
            });
            _stateMachine.Configure(State.StandBy)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                        .Ignore(Trigger.Submerging)
                        .Ignore(Trigger.Emerging)
                        .Ignore(Trigger.OutOfCombat)
                        .Ignore(Trigger.StandBy);

            _stateMachine.Configure(State.OpenWorld)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .Permit(Trigger.StandBy, State.StandBy)
                        .Permit(Trigger.Mounting, State.Mounted)
                        .Permit(Trigger.InCombat, State.Combat)
                        .PermitIf(Trigger.Submerging, State.Submerged, () => _toggleSubmergedPlaylist)
                        .Ignore(Trigger.Emerging)
                        .Ignore(Trigger.OutOfCombat);

            _stateMachine.Configure(State.Mounted)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .Permit(Trigger.StandBy, State.StandBy)
                        .PermitDynamic(Trigger.Unmounting, GameModeStateSelector)
                        .Ignore(Trigger.Submerging)
                        .Ignore(Trigger.Emerging)
                        .Ignore(Trigger.Mounting)
                        .Ignore(Trigger.OutOfCombat);

            _stateMachine.Configure(State.Combat)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .Permit(Trigger.StandBy, State.StandBy)
                        .PermitDynamic(Trigger.OutOfCombat, GameModeStateSelector)
                        .Permit(Trigger.EncounterPull, State.Encounter)
                        .Ignore(Trigger.Submerging)
                        .Ignore(Trigger.Emerging)
                        .Ignore(Trigger.InCombat);

            _stateMachine.Configure(State.Encounter)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .Permit(Trigger.StandBy, State.StandBy)
                        .PermitDynamic(Trigger.OutOfCombat, GameModeStateSelector)
                        .Ignore(Trigger.Emerging)
                        .Ignore(Trigger.EncounterPull);

            _stateMachine.Configure(State.CompetitiveMode)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .Permit(Trigger.StandBy, State.StandBy)
                        .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                        .Ignore(Trigger.Submerging)
                        .Ignore(Trigger.Emerging)
                        .Ignore(Trigger.OutOfCombat);

            _stateMachine.Configure(State.StoryInstance)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .Permit(Trigger.StandBy, State.StandBy)
                        .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                        .Permit(Trigger.InCombat, State.Combat)
                        .PermitIf(Trigger.Submerging, State.Submerged, () => _toggleSubmergedPlaylist)
                        .Ignore(Trigger.Emerging)
                        .Ignore(Trigger.OutOfCombat);

            _stateMachine.Configure(State.WorldVsWorld)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .Permit(Trigger.StandBy, State.StandBy)
                        .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                        .Permit(Trigger.Mounting, State.Mounted)
                        .Permit(Trigger.InCombat, State.Combat)
                        .PermitIf(Trigger.Submerging, State.Submerged, () => _toggleSubmergedPlaylist)
                        .Ignore(Trigger.Emerging)
                        .Ignore(Trigger.OutOfCombat);

            _stateMachine.Configure(State.Submerged)
                        .OnEntry(t => StateChanged?.Invoke(this, new ValueChangedEventArgs<State>(t.Source, t.Destination)))
                        .Permit(Trigger.StandBy, State.StandBy)
                        .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                        .Permit(Trigger.Mounting, State.Mounted)
                        .Permit(Trigger.InCombat, State.Combat)
                        .PermitDynamic(Trigger.Emerging, GameModeStateSelector)
                        .Ignore(Trigger.Submerging)
                        .Ignore(Trigger.OutOfCombat);
        }

        public void CheckWaterLevel() => IsSubmerged = Gw2Mumble.PlayerCharacter.Position.Z < -1.25f;
        public void CheckTyrianTime() => TyrianTime = TyrianTimeUtil.GetCurrentDayCycle();

        private void OnGw2Closed(object sender, EventArgs e) => _stateMachine.Fire(Trigger.StandBy);

        #region ArcDps Events

        private void CombatEventReceived(object o, RawCombatEventArgs e) {
            if (!_stateMachine.IsInState(State.Combat)) return;
            if (e.CombatEvent == null || e.CombatEvent.Dst == null) return;
            var encounterData = _encounterData.FirstOrDefault(x => x.Ids.Any(y => y.Equals(e.CombatEvent.Dst.Profession)));
            if (encounterData == null) return;

            if (CurrentEncounter != null && CurrentEncounter.Name.Equals(encounterData.Name) && CurrentEncounter.SessionId.Equals(e.CombatEvent.Dst.Id))
                CurrentEncounter.DoDamage(e.CombatEvent.Ev);
            else {
                CurrentEncounter = new Encounter(encounterData, e.CombatEvent.Dst.Id);
                _stateMachine.Fire(Trigger.EncounterPull);
            }
            
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
            _combatDelay?.Dispose();
            _combatDelay = new Timer(_combatDelayMs);
            _combatDelay.Elapsed += delegate {
                _stateMachine.Fire(Gw2Mumble.PlayerCharacter.IsInCombat ? Trigger.InCombat : Trigger.OutOfCombat);
                _combatDelay.Dispose();
            };
            _combatDelay.Start();
        }


        private void OnMapChanged(object o, ValueEventArgs<int> e) {
            _stateMachine.Fire(Trigger.MapChanged);
        }


        private void OnIsInGameChanged(object o, ValueEventArgs<bool> e) {
            _stateMachine.Fire(e.Value ? Trigger.MapChanged : Trigger.StandBy);
        }

        #endregion

        #region State Guards

        private State GameModeStateSelector() {
            if (Gw2Mumble.PlayerCharacter.IsInCombat) 
                return State.Combat;
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
