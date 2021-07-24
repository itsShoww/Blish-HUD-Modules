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

        protected override void DefineSettings(SettingCollection settings)
        {
            SPM_Repetitions = settings.DefineSetting("SmartPingRepetitions", 10, "Smart Ping Repetitions", "Indicates how often a value should be repeated before proceeding to the next reduction.");

            var selfManagedSettings = settings.AddSubCollection("Managed Settings", false, false);
            SmartPingMenuEnabled = selfManagedSettings.DefineSetting("SmartPingMenuEnabled", false);
            AutomaticClearEnabled = selfManagedSettings.DefineSetting("AutomaticClearEnabled", false);

            SPM_DropdownSelection = selfManagedSettings.DefineSetting("SmartPingMenuDropdownSelection", "");
            SPM_WingSelection = selfManagedSettings.DefineSetting("SmartPingMenuWingSelection", "W1");
        }

        private const string KILLPROOF_API_URL = "https://killproof.me/api/";

        private Dictionary<int, AsyncTexture2D> ProfessionRenderRepository;
        private Dictionary<int, AsyncTexture2D> EliteRenderRepository;
        private Dictionary<int, AsyncTexture2D> TokenRenderRepository;

        #region Settings

        internal SettingEntry<bool> SmartPingMenuEnabled;
        internal SettingEntry<bool> AutomaticClearEnabled;
        internal SettingEntry<string> SPM_DropdownSelection;
        internal SettingEntry<string> SPM_WingSelection;
        internal SettingEntry<int> SPM_Repetitions;

        #endregion

        private Texture2D _killProofIconTexture;

        internal string KillProofTabName = "KillProof";

        internal Resources Resources;

        internal PartyManager PartyManager;
        internal SmartPingMenu SmartPingMenu;

        private WindowTab _moduleTab;

        protected override void Initialize()
        {
            TokenRenderRepository = new Dictionary<int, AsyncTexture2D>();
            EliteRenderRepository = new Dictionary<int, AsyncTexture2D>();
            ProfessionRenderRepository = new Dictionary<int, AsyncTexture2D>();

            _killProofIconTexture = ContentsManager.GetTexture("killproof_icon.png");
        }

        protected override async Task LoadAsync()
        {
            Resources = await KillProofApi.LoadResources();
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
                TokenRenderRepository.Add(token.Id, new AsyncTexture2D());

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
                            var loadedTexture =
                                Texture2D.FromStream(Graphics.GraphicsDevice, textureStream);

                            TokenRenderRepository[token.Id].SwapTexture(loadedTexture);
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

                ProfessionRenderRepository.Add(id, new AsyncTexture2D());

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
                            var loadedTexture =
                                Texture2D.FromStream(Graphics.GraphicsDevice, textureStream);

                            ProfessionRenderRepository[id].SwapTexture(loadedTexture);
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

                EliteRenderRepository.Add(specialization.Id, new AsyncTexture2D());

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
                            var loadedTexture =
                                Texture2D.FromStream(Graphics.GraphicsDevice, textureStream);

                            EliteRenderRepository[specialization.Id].SwapTexture(loadedTexture);
                        }
                    });
            }
        }

        public AsyncTexture2D GetProfessionRender(CommonFields.Player player)
        {
            if (player.Elite == 0) 
                return ProfessionRenderRepository[(int)player.Profession];
            return EliteRenderRepository[(int)player.Elite];
        }

        public AsyncTexture2D GetTokenRender(int key)
        {
            return TokenRenderRepository[key];
        }

        #endregion
    }
}