using Blish_HUD;
using Blish_HUD.ArcDps;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using static Blish_HUD.GameService;
using System.IO;

namespace Nekres.Music_Mixer
{
    internal class Gw2StateService
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(Gw2StateService));

        #region _Enums

        public enum State {
            StandBy,
            Mounted,
            OpenWorld,
            Combat,
            CompetitiveMode,
            WorldVsWorld,
            StoryInstance,
            Submerged
        }
        private enum Trigger
        {
            MapChanged,
            InCombat,
            OutOfCombat,
            Mounting,
            Unmounting,
            Submerging,
            Emerging
        }

        #endregion

        #region Events
        /// <summary>
        /// Fires when the Tyrian time of day changes.
        /// </summary>
        public event EventHandler<ValueEventArgs<TyrianTime>> TyrianTimeChanged;
        public event EventHandler<ValueEventArgs<State>> StateChanged;
        #endregion

        private TyrianTime _prevTyrianTime = TyrianTime.None;
        public TyrianTime TyrianTime { 
            get => _prevTyrianTime; 
            set {
                if (_prevTyrianTime == value) return; 

                _prevTyrianTime = value;

                TyrianTimeChanged?.Invoke(this, new ValueEventArgs<TyrianTime>(value));
            }
        }

        private StateMachine<State, Trigger> _stateMachine;

        private IReadOnlyList<EncounterData> _encounterData;
        private Encounter _currentEncounter;

        public Gw2StateService(IReadOnlyList<EncounterData> encounterData) {
            _encounterData = encounterData;

            InitializeStateMachine();

            ArcDps_OnFinishedLoading(null, null);
            Mumble_OnFinishedLoading(null, null);

            TyrianTimeChanged += OnTyrianTimeChanged;
            GameIntegration.Gw2Closed += OnGw2Closed;
            GameIntegration.Gw2Started += OnGw2Started;
        }


        public void Unload() {
            GameIntegration.Gw2Closed -= OnGw2Closed;
            GameIntegration.Gw2Started -= OnGw2Started;
            ArcDps.RawCombatEvent -= CombatEventReceived;
            Gw2Mumble.PlayerCharacter.CurrentMountChanged -= OnMountChanged;
            Gw2Mumble.PlayerCharacter.IsInCombatChanged -= OnIsInCombatChanged;
            Gw2Mumble.UI.IsMapOpenChanged -= OnIsMapOpenChanged;
            Gw2Mumble.CurrentMap.MapChanged -= OnMapChanged;
            GameIntegration.IsInGameChanged -= OnIsInGameChanged;
            TyrianTimeChanged -= OnTyrianTimeChanged;
        }


        private void InitializeStateMachine() {
            _stateMachine = new StateMachine<State, Trigger>(GameModeStateSelector());

            _stateMachine.OnUnhandledTrigger((s, t) => {
                Logger.Info($"Warning: Trigger '{t}' was fired from state '{s}', but has no valid leaving transitions.");
            });
            _stateMachine.Configure(State.StandBy)
                        .OnEntry(() => StateChanged.Invoke(this, new ValueEventArgs<State>(State.StandBy)))
                        .PermitDynamic(Trigger.MapChanged, GameModeStateSelector);

            _stateMachine.Configure(State.OpenWorld)
                        .OnEntry(() => StateChanged.Invoke(this, new ValueEventArgs<State>(State.OpenWorld)))
                        .Permit(Trigger.Mounting, State.Mounted)
                        .Permit(Trigger.InCombat, State.Combat)
                        .Permit(Trigger.Submerging, State.Submerged);

            _stateMachine.Configure(State.Mounted)
                        .OnEntry(() => StateChanged.Invoke(this, new ValueEventArgs<State>(State.Mounted)))
                        .PermitDynamic(Trigger.Unmounting, GameModeStateSelector);

            _stateMachine.Configure(State.Combat)
                        .OnEntry(() => StateChanged.Invoke(this, new ValueEventArgs<State>(State.Combat)))
                        .PermitDynamicIf(Trigger.OutOfCombat, GameModeStateSelector);

            _stateMachine.Configure(State.CompetitiveMode)
                        .OnEntry(() => StateChanged.Invoke(this, new ValueEventArgs<State>(State.CompetitiveMode)))
                        .PermitDynamic(Trigger.MapChanged, GameModeStateSelector);

            _stateMachine.Configure(State.StoryInstance)
                        .OnEntry(() => StateChanged.Invoke(this, new ValueEventArgs<State>(State.StoryInstance)))
                        .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                        .Permit(Trigger.InCombat, State.Combat)
                        .Permit(Trigger.Submerging, State.Submerged);

            _stateMachine.Configure(State.WorldVsWorld)
                        .OnEntry(() => StateChanged.Invoke(this, new ValueEventArgs<State>(State.WorldVsWorld)))
                        .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                        .Permit(Trigger.Mounting, State.Mounted)
                        .Permit(Trigger.InCombat, State.Combat)
                        .Permit(Trigger.Submerging, State.Submerged);

            _stateMachine.Configure(State.Submerged)
                        .OnEntry(() => StateChanged.Invoke(this, new ValueEventArgs<State>(State.Submerged)))
                        .PermitDynamic(Trigger.MapChanged, GameModeStateSelector)
                        .Permit(Trigger.Mounting, State.Mounted)
                        .Permit(Trigger.InCombat, State.Combat)
                        .PermitDynamic(Trigger.Emerging, GameModeStateSelector);
        }


        public void CheckWaterLevel() {
            var zloc = Gw2Mumble.PlayerCharacter.Position.Z;
            if (zloc <= 0 && !_stateMachine.IsInState(State.Submerged))
                _stateMachine.Fire(Trigger.Submerging);
            else if (zloc > 0 && _stateMachine.IsInState(State.Submerged))
                _stateMachine.Fire(Trigger.Emerging);
        }


        private void OnTyrianTimeChanged(object sender, ValueEventArgs<TyrianTime> e) {
            System.Diagnostics.Debug.WriteLine(e.Value);
        }
        private void OnGw2Started(object sender, EventArgs e) { /*TODO OnGw2Started*/ }
        private void OnGw2Closed(object sender, EventArgs e) { /*TODO OnGw2Closed*/ }

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
            //TODO: Loadingscreen, mainmenu differentiation.
        }


        private void Mumble_OnFinishedLoading(object o, EventArgs e) {
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
            if (Gw2Mumble.PlayerCharacter.Position.Z <= 0) return State.Submerged;
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
