using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.ArcDps;
using Blish_HUD.ArcDps.Models;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Stateless;
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

        internal enum State {
            MainMenu,
            MapOpen,
            Mounted,
            OpenWorld,
            Combat,
            BossEncounter,
            CompetitiveMode,
            WorldVsWorld,
            StoryInstance
        }
        private StateMachine<State, bool> StateMachine;
        private CombatEvent CurrentCombatEvent;

        protected override void DefineSettings(SettingCollection settings) {

        }

        protected override void Initialize() {
            ArcDps.Common.Activate();
            ArcDps.RawCombatEvent += CombatEventChanged;
            StateMachine.Configure(State.MainMenu).PermitIf(true, State.MainMenu, () => !Gw2Mumble.IsAvailable);
            StateMachine.Configure(State.OpenWorld)
                            .PermitIf(true, State.Combat, () => Gw2Mumble.PlayerCharacter.IsInCombat)
                            .PermitIf(true, State.Mounted, () => Gw2Mumble.PlayerCharacter.CurrentMount != Gw2Sharp.Models.MountType.None)
                            .PermitIf(true, State.CompetitiveMode, () => Gw2Mumble.CurrentMap.IsCompetitiveMode)
                            .PermitIf(true, State.MapOpen, () => Gw2Mumble.UI.IsMapOpen)
                            .PermitIf(true, State.WorldVsWorld, () => IsWvwMode(Gw2Mumble.CurrentMap.Type))
                            .PermitIf(true, State.StoryInstance, () => Gw2Mumble.CurrentMap.Type == Gw2Sharp.Models.MapType.Instance)
                            .PermitIf(true, State.BossEncounter, () => IsBossEncounter());
        }
        private void CombatEventChanged(object o, RawCombatEventArgs e) {
            CurrentCombatEvent = e.CombatEvent;
        }
        private bool IsBossEncounter() {
            return false;
        }
        private bool IsWvwMode(Gw2Sharp.Models.MapType mapType) {
            switch (mapType) {
                case Gw2Sharp.Models.MapType.Center:
                    return true;
                case Gw2Sharp.Models.MapType.BlueHome:
                    return true;
                case Gw2Sharp.Models.MapType.GreenHome:
                    return true;
                case Gw2Sharp.Models.MapType.RedHome:
                    return true;
                case Gw2Sharp.Models.MapType.JumpPuzzle:
                    return true;
                case Gw2Sharp.Models.MapType.EdgeOfTheMists:
                    return true;
                case Gw2Sharp.Models.MapType.WvwLounge:
                    return true;
                default: return false;
            }
        }
        protected override async Task LoadAsync() {

        }

        protected override void OnModuleLoaded(EventArgs e) {

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        protected override void Update(GameTime gameTime) {
            var currentTime = DateTime.Now.ToUniversalTime();
            // How far along are we (in  % ) of the current 2 hour event cycles?
            var percentOfTwoHours = Math.Abs((currentTime.Hour % 2) + (currentTime.Minute / 60) - 1);
            if (percentOfTwoHours > 0.98) { //TODO: Next hour
                
            }
        }

        /// <inheritdoc />
        protected override void Unload() {
            // Unload

            // All static members must be manually unset
            ModuleInstance = null;
        }
    }

}
