using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using static Blish_HUD.GameService;
namespace Nekres.Mumble_Info_Module
{
    [Export(typeof(Module))]
    public class MumbleInfoModule : Module
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(MumbleInfoModule));

        internal static MumbleInfoModule ModuleInstance;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        [ImportingConstructor]
        public MumbleInfoModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        #region Settings

        private SettingEntry<KeyBinding> ToggleInfoBinding;

        #endregion

        private bool _mumbleDataVisible;
        private List<Control> _moduleControls;

        private Panel _dataPanel; //TODO
        private Label _positionLabel;

        protected override void DefineSettings(SettingCollection settings) {
            ToggleInfoBinding = settings.DefineSetting("ToggleInfoBinding", new KeyBinding(Keys.F12),
                "Toggle Mumble Data", "Toggles the display of mumble data.");
        }


        protected override void Initialize() {
            _moduleControls = new List<Control>();
        }


        protected override async Task LoadAsync() {

        }


        protected override void OnModuleLoaded(EventArgs e) {
            ToggleInfoBinding.Value.Enabled = true;
            ToggleInfoBinding.Value.Activated += OnToggleInfoBindingActivated;
            // Base handler must be called
            base.OnModuleLoaded(e);
        }


        private void OnToggleInfoBindingActivated(object o, EventArgs e) {
            if (_mumbleDataVisible)
                DisposeModuleControls();
            else
                BuildDisplay();
        }


        protected override void Update(GameTime gameTime) {
            if (!_mumbleDataVisible) return;

            if (_positionLabel != null) {
                var pos = Gw2Mumble.PlayerCharacter.Position;
                _positionLabel.Text = $"X: {pos.X}\nY: {pos.Y}\nZ: {pos.Z}";
                _positionLabel.Location = new Point(10, (Graphics.SpriteScreen.Height / 2) + 100);
            }
        }


        private void BuildDisplay() {
            _positionLabel?.Dispose();
            _positionLabel = new Label() {
                Parent = Graphics.SpriteScreen,
                Size = new Point(200,200),
                Font = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size16, ContentService.FontStyle.Regular),
                AutoSizeHeight = true,
                AutoSizeWidth = true,
                ShowShadow = true,
                StrokeText = true
            };
            _moduleControls.Add(_positionLabel);

            _mumbleDataVisible = true;
        }


        private void DisposeModuleControls() {
            foreach (var c in _moduleControls)
                c?.Dispose();
            _moduleControls.Clear();
            _mumbleDataVisible = false;
        }


        /// <inheritdoc />
        protected override void Unload() {
            DisposeModuleControls();
            // All static members must be manually unset
            ModuleInstance = null;
        }

    }

}
