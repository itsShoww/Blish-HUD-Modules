using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Nekres.Notes_Module
{

    [Export(typeof(Module))]
    public class NotesModule : Module
    {

        internal static readonly Logger Logger = Logger.GetLogger(typeof(NotesModule));

        internal static NotesModule ModuleInstance;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        [ImportingConstructor]
        public NotesModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        protected override void DefineSettings(SettingCollection settings) {

        }

        private Texture2D _icon64;
        //private Texture2D _icon128;

        private CornerIcon moduleCornerIcon;

        protected override void Initialize() {
            LoadTextures();
            moduleCornerIcon = new CornerIcon
            {
                IconName = Name,
                Icon = _icon64,
                Priority = Name.GetHashCode()
            };
        }

        private void LoadTextures()
        {
            _icon64 = ContentsManager.GetTexture("notes_icon_64x64.png");
            //_icon128 = ContentsManager.GetTexture("notes_icon_128x128.png");
        }

        protected override async Task LoadAsync() {
        }

        protected override void OnModuleLoaded(EventArgs e) {

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        protected override void Update(GameTime gameTime) {

        }

        /// <inheritdoc />
        protected override void Unload() {

            // All static members must be manually unset
            ModuleInstance = null;
        }

    }

}
