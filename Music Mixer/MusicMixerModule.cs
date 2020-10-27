using Blish_HUD;
using Blish_HUD.ArcDps;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp.WebApi.Caching;
using Microsoft.Xna.Framework;
using Music_Mixer.Persistance;
using Newtonsoft.Json;
using Stateless;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Blish_HUD.GameService;

namespace Music_Mixer
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


        private StateMachine<State, Trigger> StateMachine;
        private IReadOnlyList<EncounterData> EncounterData;
        private Encounter CurrentEncounter;


        protected override void DefineSettings(SettingCollection settings) {

        }


        protected override void Initialize() {
            ArcDps_OnFinishedLoading(null, null);
            Mumble_OnFinishedLoading(null, null);

            LoadEncounterData();
        }


        private void InitializeStateMachine() {
            StateMachine = new StateMachine<State, Trigger>(GameModeStateSelector());

            //TODO: IsAvailableChanged
            StateMachine.OnUnhandledTrigger((s, t) => {
                Logger.Info($"Warning: Trigger '{t}' was fired from state '{s}', but has no valid leaving transitions.");
            });
            StateMachine.Configure(State.StandBy)
                        .PermitDynamic(Trigger.MapChanged, () => GameModeStateSelector());

            StateMachine.Configure(State.OpenWorld)
                        .Permit(Trigger.Mounting, State.Mounted)
                        .Permit(Trigger.InCombat, State.Combat);

            StateMachine.Configure(State.Mounted)
                        .PermitDynamic(Trigger.Unmounting, () => GameModeStateSelector());

            StateMachine.Configure(State.Combat)
                        .PermitDynamicIf(Trigger.OutOfCombat, () => GameModeStateSelector());

            StateMachine.Configure(State.CompetitiveMode)
                        .PermitDynamic(Trigger.MapChanged, () => GameModeStateSelector());

            StateMachine.Configure(State.StoryInstance)
                        .PermitDynamic(Trigger.MapChanged, () => GameModeStateSelector())
                        .Permit(Trigger.InCombat, State.Combat);

            StateMachine.Configure(State.WorldVsWorld)
                        .Permit(Trigger.Mounting, State.Mounted)
                        .Permit(Trigger.InCombat, State.Combat);
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


        protected override async Task LoadAsync() {

        }


        protected override void OnModuleLoaded(EventArgs e) {
            // Base handler must be called
            base.OnModuleLoaded(e);
        }


        protected override void Update(GameTime gameTime) {
            if (StateMachine != null) System.Diagnostics.Debug.WriteLine(StateMachine.State);
        }


        /// <inheritdoc />
        protected override void Unload() { 
            // Unload

            // All static members must be manually unset
            ModuleInstance = null;
        }


        #region ArcDps Events

        private void ArcDps_OnFinishedLoading(object o, EventArgs e) {
            ArcDps.Common.Activate();
            ArcDps.RawCombatEvent += CombatEventReceived;
        }


        private void CombatEventReceived(object o, RawCombatEventArgs e) {
            if (!StateMachine.IsInState(State.Combat)) return;
            if (e.CombatEvent == null || e.CombatEvent.Dst == null) return;
            var encounterData = EncounterData.FirstOrDefault(x => x.Ids.Any(y => y.Equals(e.CombatEvent.Dst.Profession)));
            if (encounterData == null) return;

            if (CurrentEncounter != null && CurrentEncounter.Name.Equals(encounterData.Name) && CurrentEncounter.SessionId.Equals(e.CombatEvent.Dst.Id))
                CurrentEncounter.DoDamage(e.CombatEvent.Ev);
            else
                CurrentEncounter = new Encounter(encounterData.Name, encounterData.Ids, encounterData.Health, encounterData.EnrageTimer, e.CombatEvent.Dst.Id);
            
        }

        #endregion

        #region Mumble Events

        private void OnMountChanged(object o, ValueEventArgs<Gw2Sharp.Models.MountType> e) {
            if (e.Value.Equals(Gw2Sharp.Models.MountType.None))
                StateMachine.Fire(Trigger.Unmounting);
            else
                StateMachine.Fire(Trigger.Mounting);
        }


        private void OnIsInCombatChanged(object o, ValueEventArgs<bool> e) {
            StateMachine.Fire(e.Value ? Trigger.InCombat : Trigger.OutOfCombat);
        }


        private void OnIsMapOpenChanged(object o, ValueEventArgs<bool> e) {
            //TODO: Muffle current song, lower volume. No state needed.
        }


        private void OnMapChanged(object o, ValueEventArgs<int> e) {
            StateMachine.Fire(Trigger.MapChanged);
        }


        private void Mumble_OnFinishedLoading(object o, EventArgs e) {
            InitializeStateMachine();

            Gw2Mumble.PlayerCharacter.CurrentMountChanged += OnMountChanged;
            Gw2Mumble.PlayerCharacter.IsInCombatChanged += OnIsInCombatChanged;
            Gw2Mumble.UI.IsMapOpenChanged += OnIsMapOpenChanged;
            Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;
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
