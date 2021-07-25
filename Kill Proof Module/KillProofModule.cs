using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.ArcDps.Common;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp.Models;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework.Graphics;
using Nekres.Kill_Proof_Module.Controls;
using Nekres.Kill_Proof_Module.Controls.Views;
using Nekres.Kill_Proof_Module.Manager;
using Nekres.Kill_Proof_Module.Models;
using static Blish_HUD.GameService;

namespace Nekres.Kill_Proof_Module
{
    [Export(typeof(Module))]
    public class KillProofModule : Module
    {
        internal static readonly Logger Logger = Logger.GetLogger(typeof(KillProofModule));

        internal static KillProofModule ModuleInstance;

        [ImportingConstructor]
        public KillProofModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        #region Service Managers

        internal SettingsManager SettingsManager => ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => ModuleParameters.Gw2ApiManager;

        #endregion

        #region Settings

        internal SettingEntry<bool> SmartPingMenuEnabled;
        internal SettingEntry<bool> AutomaticClearEnabled;
        internal SettingEntry<string> SPM_DropdownSelection;
        internal SettingEntry<string> SPM_WingSelection;
        internal SettingEntry<int> SPM_Repetitions;

        #endregion

        protected override void DefineSettings(SettingCollection settings)
        {
            SPM_Repetitions = settings.DefineSetting("SmartPingRepetitions", 10, "Smart Ping Repetitions", "Indicates how often a value should be repeated before proceeding to the next reduction.");

            var selfManagedSettings = settings.AddSubCollection("Managed Settings", false, false);
            SmartPingMenuEnabled = selfManagedSettings.DefineSetting("SmartPingMenuEnabled", false);
            AutomaticClearEnabled = selfManagedSettings.DefineSetting("AutomaticClearEnabled", false);

            SPM_DropdownSelection = selfManagedSettings.DefineSetting("SmartPingMenuDropdownSelection", "");
            SPM_WingSelection = selfManagedSettings.DefineSetting("SmartPingMenuWingSelection", "W1");
        }

        private Dictionary<int, AsyncTexture2D> _professionRenderRepository;
        private Dictionary<int, AsyncTexture2D> _eliteRenderRepository;
        private Dictionary<int, AsyncTexture2D> _tokenRenderRepository;

        private Texture2D _killProofIconTexture;

        internal string KillProofTabName = "KillProof";

        internal Resources Resources;

        internal PartyManager PartyManager;
        internal SmartPingMenu SmartPingMenu;

        private WindowTab _moduleTab;

        protected override void Initialize()
        {
            _tokenRenderRepository = new Dictionary<int, AsyncTexture2D>();
            _eliteRenderRepository = new Dictionary<int, AsyncTexture2D>();
            _professionRenderRepository = new Dictionary<int, AsyncTexture2D>();

            _killProofIconTexture = ContentsManager.GetTexture("killproof_icon.png");
        }

        protected override async Task LoadAsync()
        {
            Resources = await KillProofApi.LoadResources();
            await LoadTokenIcons();
            await LoadProfessionIcons();
            await LoadEliteIcons();
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            PartyManager = new PartyManager();
            SmartPingMenu = new SmartPingMenu();
            
            _moduleTab = Overlay.BlishHudWindow.AddTab(KillProofTabName, _killProofIconTexture, () => new MainView());
            
            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            Overlay.BlishHudWindow.RemoveTab(_moduleTab);
            SmartPingMenu?.Dispose();
            // All static members must be manually unset
            ModuleInstance = null;
        }

        #region Render Getters

        private async Task LoadTokenIcons()
        {
            var tokenRenderUrlRepository = Resources.GetAllTokens();
            foreach (var token in tokenRenderUrlRepository)
            {
                _tokenRenderRepository.Add(token.Id, new AsyncTexture2D());

                var renderUri = token.Icon;
                await Gw2ApiManager.Gw2ApiClient.Render.DownloadToByteArrayAsync(renderUri)
                    .ContinueWith(textureDataResponse =>
                    {
                        if (textureDataResponse.Exception != null) {
                            Logger.Warn(textureDataResponse.Exception, $"Request to render service for {renderUri} failed.");
                            return;
                        }
                        using (var textureStream = new MemoryStream(textureDataResponse.Result))
                        {
                            var loadedTexture = Texture2D.FromStream(Graphics.GraphicsDevice, textureStream);

                            _tokenRenderRepository[token.Id].SwapTexture(loadedTexture);
                        }
                    });
            }
        }


        private async Task<IReadOnlyList<Profession>> LoadProfessions()
        {
            return await Gw2ApiManager.Gw2ApiClient.V2.Professions.ManyAsync(Enum.GetValues(typeof(ProfessionType)).Cast<ProfessionType>());
        }


        private async Task LoadProfessionIcons()
        {
            var professions = await LoadProfessions();
            foreach (var profession in professions)
            {
                var id = (int) Enum.GetValues(typeof(ProfessionType)).Cast<ProfessionType>().ToList()
                    .Find(x => x.ToString().Equals(profession.Id, StringComparison.InvariantCultureIgnoreCase));

                _professionRenderRepository.Add(id, new AsyncTexture2D());

                var renderUri = (string) profession.IconBig;
                await Gw2ApiManager.Gw2ApiClient.Render.DownloadToByteArrayAsync(renderUri)
                    .ContinueWith(textureDataResponse =>
                    {
                        if (textureDataResponse.Exception != null) {
                            Logger.Warn(textureDataResponse.Exception, $"Request to render service for {renderUri} failed.");
                            return;
                        }
                        using (var textureStream = new MemoryStream(textureDataResponse.Result))
                        {
                            var loadedTexture = Texture2D.FromStream(Graphics.GraphicsDevice, textureStream);

                            _professionRenderRepository[id].SwapTexture(loadedTexture);
                        }
                    });
            }
        }


        private async Task LoadEliteIcons()
        {
            var ids = await Gw2ApiManager.Gw2ApiClient.V2.Specializations.IdsAsync();
            var specializations = await Gw2ApiManager.Gw2ApiClient.V2.Specializations.ManyAsync(ids);
            foreach (var specialization in specializations)
            {
                if (!specialization.Elite) continue;

                _eliteRenderRepository.Add(specialization.Id, new AsyncTexture2D());

                var renderUri = (string) specialization.ProfessionIconBig;
                await Gw2ApiManager.Gw2ApiClient.Render.DownloadToByteArrayAsync(renderUri)
                    .ContinueWith(textureDataResponse =>
                    {
                        if (textureDataResponse.Exception != null) {
                            Logger.Warn(textureDataResponse.Exception, $"Request to render service for {renderUri} failed.");
                            return;
                        }
                        using (var textureStream = new MemoryStream(textureDataResponse.Result))
                        {
                            var loadedTexture = Texture2D.FromStream(Graphics.GraphicsDevice, textureStream);

                            _eliteRenderRepository[specialization.Id].SwapTexture(loadedTexture);
                        }
                    });
            }
        }

        public AsyncTexture2D GetProfessionRender(CommonFields.Player player)
        {
            if (player.Elite != 0) return _eliteRenderRepository[(int)player.Elite];
            if (player.Profession > 0)
                return _professionRenderRepository[(int)player.Profession];
            return Content.GetTexture("common/733268");
        }

        public AsyncTexture2D GetTokenRender(int key)
        {
            if (_tokenRenderRepository.ContainsKey(key))
                return _tokenRenderRepository[key];
            return Content.GetTexture("deleted_item");
        }

        #endregion

    }
}