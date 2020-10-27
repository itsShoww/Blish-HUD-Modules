using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Intern;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp;
using Gw2Sharp.Models;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nekres.Special_Forces_Module.Controls;
using Nekres.Special_Forces_Module.Persistance;
using Nekres.Special_Forces_Module.Player;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;

namespace Nekres.Special_Forces_Module
{
    [Export(typeof(Module))]
    public class SpecialForcesModule : Module
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(SpecialForcesModule));

        private Texture2D ICON;
        private const int SCROLLBAR_WIDTH = 24;
        private const int TOP_MARGIN = 10;
        private const int RIGHT_MARGIN = 5;
        private const int BOTTOM_MARGIN = 10;
        private const int LEFT_MARGIN = 8;

        private const string DD_TITLE = "Title";
        private const string DD_PROFESSION = "Profession";

        private const int TimeOutGetRender = 5000;

        internal static SpecialForcesModule ModuleInstance;

        private List<TemplateButton> _displayedTemplates;
        private RawTemplate _editorTemplate;

        #region Rendercache

        private Dictionary<int, AsyncTexture2D> EliteRenderRepository;
        private Dictionary<int, AsyncTexture2D> ProfessionRenderRepository;
        private Dictionary<int, AsyncTexture2D> SkillRenderRepository;
        private IReadOnlyList<Profession> ProfessionRepository;
        private List<Skill> SkillRepository;
        private List<Skill> ChainSkillRepository;

        #endregion

        #region Textures

        private Texture2D _surrenderTooltip_texture;
        private Texture2D _surrenderFlag_hover;
        private Texture2D _surrenderFlag;
        private Texture2D _surrenderFlag_pressed;

        #endregion

        private WindowTab _specialForcesTab;
        private Image _surrenderButton;
        private TemplatePlayer _templatePlayer;
        private TemplateReader _templateReader;
        private List<RawTemplate> _templates;

        [ImportingConstructor]
        public SpecialForcesModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(
            moduleParameters)
        {
            ModuleInstance = this;
        }

        private void LoadTextures()
        {
            ICON = ICON ?? ContentsManager.GetTexture("specialforces_icon.png");
            _surrenderTooltip_texture = ContentsManager.GetTexture("surrender_tooltip.png");
            _surrenderFlag = ContentsManager.GetTexture("surrender_flag.png");
            _surrenderFlag_hover = ContentsManager.GetTexture("surrender_flag_hover.png");
            _surrenderFlag_pressed = ContentsManager.GetTexture("surrender_flag_pressed.png");

        }

        protected override void DefineSettings(SettingCollection settings)
        {
            var selfManagedSettings = settings.AddSubCollection("ManagedSettings", false, false);
            SurrenderButtonEnabled = selfManagedSettings.DefineSetting("SurrenderButtonEnabled", false, "Show Surrender Skill",
                "Shows a skill with a white flag to the right of your skill bar.\nClicking it defeats you. (Sends \"/gg\" into chat.)");
            SurrenderBinding = selfManagedSettings.DefineSetting("SurrenderButtonKey", new KeyBinding(Keys.None) {Enabled = true},
                "Surrender", "Defeats you.\n(Sends \"/gg\" into chat.)");
            LibraryShowAll = selfManagedSettings.DefineSetting("LibraryShowAll", false, "Show All Templates",
                "Show all templates no matter your current profession.");
            foreach (GuildWarsControls skill in Enum.GetValues(typeof(GuildWarsControls)))
            {
                if (skill == GuildWarsControls.None) continue;
                var friendlyName = Regex.Replace(skill.ToString(), "([A-Z]|[1-9])", " $1", RegexOptions.Compiled)
                    .Trim();
                SkillBindings.Add(skill,
                    selfManagedSettings.DefineSetting(skill.ToString(), new KeyBinding(Keys.None) {Enabled = true}, friendlyName,
                        "Your key binding for " + friendlyName));
            }

            InteractionBinding = selfManagedSettings.DefineSetting("InteractionKey", new KeyBinding(Keys.F) {Enabled = true},
                "Interact",
                "General context-sensitive interact prompt. Used for\ninteracting with the environment, including Talk,\nLoot Revive, etc.");
            DodgeBinding = selfManagedSettings.DefineSetting("DodgeKey", new KeyBinding(Keys.V) {Enabled = true},
                "Dodge",
                "Do an evasive dodge roll, negating damage, in the\ndirection your character is moving (backward if\nstationary).");
        }

        protected override void Initialize()
        {
            LoadTextures();

            EliteRenderRepository = new Dictionary<int, AsyncTexture2D>();
            ProfessionRenderRepository = new Dictionary<int, AsyncTexture2D>();
            SkillRenderRepository = new Dictionary<int, AsyncTexture2D>();
            SkillRepository = new List<Skill>();
            ChainSkillRepository = new List<Skill>();

            _templateReader = new TemplateReader();
            _templatePlayer = new TemplatePlayer();
            _templates = new List<RawTemplate>();
            _editorTemplate = new RawTemplate();
            _displayedTemplates = new List<TemplateButton>();
            _surrenderButton = SurrenderButtonEnabled.Value ? BuildSurrenderButton() : null;

            SurrenderBinding.Value.Activated += delegate { GameService.GameIntegration.Chat.Send("/gg"); };
        }

        protected override async Task LoadAsync()
        {
            _templates = await _templateReader.LoadDirectory(DirectoriesManager.GetFullDirectoryPath("specialforces"));
            ProfessionRepository = await GameService.Gw2WebApi.AnonymousConnection.Client.V2.Professions.AllAsync();
            await Task.Run(LoadProfessionIcons);
            await Task.Run(LoadEliteIcons);
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            _specialForcesTab = GameService.Overlay.BlishHudWindow.AddTab("Special Forces", ICON,
                BuildHomePanel(GameService.Overlay.BlishHudWindow), 0);
            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        protected override void Update(GameTime gameTime)
        {
            if (_surrenderButton != null)
            {
                _surrenderButton.Visible = GameService.GameIntegration.IsInGame;
                _surrenderButton.Location =
                    new Point(GameService.Graphics.SpriteScreen.Width / 2 - _surrenderButton.Width / 2 + 431,
                        GameService.Graphics.SpriteScreen.Height - _surrenderButton.Height * 2 + 7);
            }
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            // Unload
            _surrenderButton?.Dispose();
            _templatePlayer?.Dispose();

            GameService.Overlay.BlishHudWindow.RemoveTab(_specialForcesTab);

            // All static members must be manually unset
            ModuleInstance = null;
        }

        private Image BuildSurrenderButton()
        {
            var tooltip_size = new Point(_surrenderTooltip_texture.Width, _surrenderTooltip_texture.Height);
            var surrenderButtonTooltip = new Tooltip
            {
                Size = tooltip_size
            };
            var surrenderButtonTooltipImage = new Image(_surrenderTooltip_texture)
            {
                Parent = surrenderButtonTooltip,
                Location = new Point(0, 0),
                Visible = surrenderButtonTooltip.Visible
            };
            var surrenderButton = new Image
            {
                Parent = GameService.Graphics.SpriteScreen,
                Size = new Point(45, 45),
                Location = new Point(GameService.Graphics.SpriteScreen.Width / 2 - 22,
                    GameService.Graphics.SpriteScreen.Height - 45),
                Texture = _surrenderFlag,
                Visible = SurrenderButtonEnabled.Value,
                Tooltip = surrenderButtonTooltip
            };
            surrenderButton.MouseEntered += delegate { surrenderButton.Texture = _surrenderFlag_hover; };
            surrenderButton.MouseLeft += delegate { surrenderButton.Texture = _surrenderFlag; };
            surrenderButton.LeftMouseButtonPressed += delegate
            {
                surrenderButton.Size = new Point(43, 43);
                surrenderButton.Texture = _surrenderFlag_pressed;
            };
            surrenderButton.LeftMouseButtonReleased += delegate
            {
                surrenderButton.Size = new Point(45, 45);
                surrenderButton.Texture = _surrenderFlag;
                GameService.GameIntegration.Chat.Send("/gg");
            };
            GameService.Animation.Tweener.Tween(surrenderButton, new {Opacity = 1.0f}, 0.35f);
            return surrenderButton;
        }

        #region Service Managers

        internal SettingsManager SettingsManager => ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => ModuleParameters.Gw2ApiManager;

        #endregion

        #region Settings

        private SettingEntry<bool> SurrenderButtonEnabled;
        private SettingEntry<KeyBinding> SurrenderBinding;
        private SettingEntry<bool> LibraryShowAll;

        internal Dictionary<GuildWarsControls, SettingEntry<KeyBinding>> SkillBindings =
            new Dictionary<GuildWarsControls, SettingEntry<KeyBinding>>();

        internal SettingEntry<KeyBinding> InteractionBinding;
        internal SettingEntry<KeyBinding> DodgeBinding;

        #endregion

        #region Render Getters

        private async void LoadProfessionIcons()
        {
            foreach (Profession profession in ProfessionRepository)
            {
                var renderUri = (string) profession.IconBig;
                var id = (int) Enum.GetValues(typeof(ProfessionType)).Cast<ProfessionType>().ToList()
                    .Find(x => x.ToString().Equals(profession.Id, StringComparison.InvariantCultureIgnoreCase));
                if (ProfessionRenderRepository.Any(x => x.Key == id))
                {
                    try
                    {
                        var textureDataResponse = await GameService.Gw2WebApi.AnonymousConnection.Client.Render
                            .DownloadToByteArrayAsync(renderUri);

                        using (var textureStream = new MemoryStream(textureDataResponse))
                        {
                            var loadedTexture =
                                Texture2D.FromStream(GameService.Graphics.GraphicsDevice, textureStream);

                            ProfessionRenderRepository[id].SwapTexture(loadedTexture);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, $"Request to render service for {renderUri} failed.", renderUri);
                    }
                }
                else
                {
                    ProfessionRenderRepository.Add(id, GameService.Content.GetRenderServiceTexture(renderUri));
                }
            }
        }

        private async void LoadEliteIcons()
        {
            var ids = await GameService.Gw2WebApi.AnonymousConnection.Client.V2.Specializations.IdsAsync();
            var specializations = await GameService.Gw2WebApi.AnonymousConnection.Client.V2.Specializations.ManyAsync(ids);
            foreach (Specialization specialization in specializations)
            {
                if (!specialization.Elite) continue;
                if (EliteRenderRepository.Any(x => x.Key == specialization.Id))
                {
                    var renderUri = (string)specialization.ProfessionIconBig;
                    try
                    {
                        var textureDataResponse = await GameService.Gw2WebApi.AnonymousConnection
                            .Client
                            .Render.DownloadToByteArrayAsync(renderUri);

                        using (var textureStream = new MemoryStream(textureDataResponse))
                        {
                            var loadedTexture =
                                Texture2D.FromStream(GameService.Graphics.GraphicsDevice,
                                    textureStream);

                            EliteRenderRepository[specialization.Id].SwapTexture(loadedTexture);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, $"Request to render service for {renderUri} failed.", renderUri);
                    }
                }
                else
                {
                    EliteRenderRepository.Add(specialization.Id, GameService.Content.GetRenderServiceTexture(specialization.ProfessionIconBig));
                }
            }
        }

        private AsyncTexture2D GetProfessionRender(RawTemplate template)
        {
            if (ProfessionRenderRepository.All(x => x.Key != (int) template.BuildChatLink.Profession))
            {
                var render = new AsyncTexture2D();
                ProfessionRenderRepository.Add((int) template.BuildChatLink.Profession, render);
                return render;
            }
            return ProfessionRenderRepository[(int) template.BuildChatLink.Profession];
        }

        private AsyncTexture2D GetEliteRender(RawTemplate template)
        {
            if (template.Specialization != null && template.Specialization.Elite)
                return GetProfessionRender(template);
            if (EliteRenderRepository.All(x => x.Key != template.BuildChatLink.Specialization3Id))
            {
                var render = new AsyncTexture2D();
                EliteRenderRepository.Add(template.BuildChatLink.Specialization3Id, render);
                return render;
            } 
            return EliteRenderRepository[template.BuildChatLink.Specialization3Id];
        }
        #endregion

        #region Panel Stuff

        /*######################################
          # PANEL RELATED STUFF BELOW.
          ######################################*/

        #region Home Panel

        private Panel BuildHomePanel(WindowBase wndw)
        {
            var hPanel = new Panel
            {
                CanScroll = false,
                Size = wndw.ContentRegion.Size
            };

            var contentPanel = new Panel
            {
                Location = new Point(hPanel.Width - 630, 50),
                Size = new Point(630, hPanel.Size.Y - 50 - BOTTOM_MARGIN),
                Parent = hPanel,
                CanScroll = true
            };
            var menuSection = new Panel
            {
                ShowBorder = true,
                Size = new Point(hPanel.Width - contentPanel.Width - 10, contentPanel.Height + BOTTOM_MARGIN),
                Location = new Point(LEFT_MARGIN, 20),
                Parent = hPanel
            };
            var subCategories = new Menu
            {
                Parent = menuSection,
                Size = menuSection.ContentRegion.Size,
                MenuItemHeight = 40
            };
            var lPanel = BuildLibraryPanel(wndw);
            var library = subCategories.AddMenuItem("Library");
            library.LeftMouseButtonReleased += delegate { wndw.Navigate(lPanel); };
            var ePanel = BuildEditorPanel(wndw);
            var editor = subCategories.AddMenuItem("Editor");
            editor.LeftMouseButtonReleased += delegate { wndw.Navigate(ePanel); };
            var options = subCategories.AddMenuItem("Options");
            var oPanel = BuildSettingsPanel(contentPanel);
            options.LeftMouseButtonPressed += delegate { oPanel.Visible = true; };
            return hPanel;
        }

        #endregion

        #region Library Panel

        private TemplateButton AddTemplate(RawTemplate template, Panel parent)
        {
            var button = new TemplateButton(template)
            {
                Parent = parent,
                Icon = GetEliteRender(template),
                IconSize = DetailsIconSize.Small,
                Text = template.Title,
                BottomText = template.GetClassFriendlyName()
            };
            _displayedTemplates.Add(button);
            button.LeftMouseButtonPressed += delegate
            {
                if (button.MouseOverPlay) _templatePlayer.Play(template);
                if (button.MouseOverUtility1 || button.MouseOverUtility2 || button.MouseOverUtility3)
                {
                    var index = button.MouseOverUtility1 ? 0 : button.MouseOverUtility2 ? 1 : 2;
                    var swap = button.Template.Utilitykeys[index] == 3 ? 1 : button.Template.Utilitykeys[index] + 1;

                    if (Array.Exists(button.Template.Utilitykeys, e => e == swap))
                        button.Template.Utilitykeys[Array.FindIndex(button.Template.Utilitykeys, e => e == swap)] =
                            button.Template.Utilitykeys[index];
                    button.Template.Utilitykeys[index] = swap;
                    button.Template.Save();
                }
            };
            return button;
        }

        private Panel BuildLibraryPanel(WindowBase wndw)
        {
            var libraryPanel = new Panel
            {
                CanScroll = false,
                Size = wndw.ContentRegion.Size
            };
            var backButton = new BackButton(wndw)
            {
                Text = "Special Forces",
                NavTitle = "Settings",
                Parent = libraryPanel,
                Location = new Point(20, 20)
            };
            var contentPanel = new Panel
            {
                Location = new Point(0, BOTTOM_MARGIN + backButton.Bottom),
                Size = new Point(libraryPanel.Width, libraryPanel.Size.Y - 150 - BOTTOM_MARGIN),
                Parent = libraryPanel,
                ShowTint = true,
                ShowBorder = true,
                CanScroll = true
            };
            foreach (var template in _templates) AddTemplate(template, contentPanel);
            var ddSortMethod = new Dropdown
            {
                Parent = libraryPanel,
                Visible = contentPanel.Visible,
                Location = new Point(libraryPanel.Right - 150 - 10, 5),
                Width = 150
            };
            ddSortMethod.Items.Add(DD_TITLE);
            ddSortMethod.Items.Add(DD_PROFESSION);
            ddSortMethod.ValueChanged += UpdateSort;
            ddSortMethod.SelectedItem = DD_TITLE;
            UpdateSort(ddSortMethod, EventArgs.Empty);

            var sortShowAll = new Checkbox
            {
                Parent = libraryPanel,
                Visible = contentPanel.Visible,
                Location = new Point(ddSortMethod.Left - 140, 10),
                Text = "Show All",
                Checked = LibraryShowAll.Value
            };
            sortShowAll.CheckedChanged += delegate(object sender, CheckChangedEvent e)
            {
                LibraryShowAll.Value = e.Checked;
                UpdateSort(ddSortMethod, EventArgs.Empty);
            };
            var import_button = new StandardButton
            {
                Parent = libraryPanel,
                Location = new Point(contentPanel.Right - 150, contentPanel.Bottom + BOTTOM_MARGIN),
                Text = "Import Json Url",
                Size = new Point(150, 30)
            };
            import_button.LeftMouseButtonReleased += delegate
            {
                ScreenNotification.ShowNotification("This function is not implemented", ScreenNotification.NotificationType.Error);
                /*
                var json = Task.Run(async () => await ClipboardUtil.WindowsClipboardService.GetTextAsync());
                var template = _templateReader.LoadSingle(json.Result);
                if (template != null)
                {
                    template.Save();
                    _templates.Add(template);
                    AddTemplate(template, contentPanel);
                    UpdateSort(ddSortMethod, EventArgs.Empty);
                }*/
            };
            return libraryPanel;
        }

        private void UpdateSort(object sender, EventArgs e)
        {
            switch (((Dropdown) sender).SelectedItem)
            {
                case DD_TITLE:
                    _displayedTemplates.Sort((e1, e2) => e1.Template.Title.CompareTo(e2.Template.Title));
                    foreach (var e1 in _displayedTemplates)
                        e1.Visible = LibraryShowAll.Value || e1.Template.BuildChatLink.Profession.ToString()
                            .Equals(GameService.Gw2Mumble.PlayerCharacter.Profession.ToString(),
                                StringComparison.InvariantCultureIgnoreCase);

                    break;
                case DD_PROFESSION:
                    _displayedTemplates.Sort((e1, e2) =>
                        e1.BottomText.CompareTo(e2.BottomText));
                    foreach (var e1 in _displayedTemplates)
                        e1.Visible = LibraryShowAll.Value || e1.Template.BuildChatLink.Profession.ToString()
                            .Equals(GameService.Gw2Mumble.PlayerCharacter.Profession.ToString(),
                                StringComparison.InvariantCultureIgnoreCase);
                    break;
            }

            RepositionTemplates();
        }

        private void RepositionTemplates()
        {
            var pos = 0;
            foreach (var e in _displayedTemplates)
            {
                var x = pos % 3;
                var y = pos / 3;
                e.Location = new Point(x * 335, y * 108);

                ((Panel) e.Parent).VerticalScrollOffset = 0;
                e.Parent.Invalidate();
                if (e.Visible) pos++;
            }
        }

        #endregion

        #region Settings Panel

        private Panel BuildSettingsPanel(Panel wndw)
        {
            var settingsPanel = new Panel
            {
                Parent = wndw,
                CanScroll = false,
                Size = wndw.ContentRegion.Size,
                Visible = false
            };
            var surrenderItem = new Checkbox
            {
                Parent = settingsPanel,
                Location = new Point(LEFT_MARGIN, TOP_MARGIN),
                Text = SurrenderButtonEnabled.DisplayName,
                BasicTooltipText = SurrenderButtonEnabled.Description,
                Checked = SurrenderButtonEnabled.Value
            };
            surrenderItem.CheckedChanged += delegate(object sender, CheckChangedEvent e)
            {
                SurrenderButtonEnabled.Value = e.Checked;
                if (e.Checked)
                    _surrenderButton = BuildSurrenderButton();
                else
                    GameService.Animation.Tweener.Tween(_surrenderButton, new {Opacity = 0.0f}, 0.2f).OnComplete(() => _surrenderButton?.Dispose());
            };
            var bindingsPanel = new FlowPanel
            {
                Parent = settingsPanel,
                Size = new Point(settingsPanel.Size.X - 100, settingsPanel.Size.Y - 50),
                Location = new Point(settingsPanel.Size.X / 2 - (settingsPanel.Size.X - 100) / 2,
                    50),
                ControlPadding = new Vector2(2, 2),
                Title = "",
                CanScroll = true
            };
            var miscBindings = new FlowPanel
            {
                Parent = bindingsPanel,
                Size = new Point(bindingsPanel.ContentRegion.Size.X - 24, 100),
                ControlPadding = new Vector2(2, 2),
                ShowTint = true,
                Title = "Miscellaneous",
                CanCollapse = true,
                Collapsed = false
            };
            var skillsBindings = new FlowPanel
            {
                Parent = bindingsPanel,
                Size = new Point(bindingsPanel.ContentRegion.Size.X - 24, 500),
                ControlPadding = new Vector2(2, 2),
                ShowTint = true,
                Title = "Skills",
                CanCollapse = true,
                Collapsed = true
            };
            // KeybindingAssigners
            var surrenderKeyAssigner = new KeybindingAssigner(SurrenderBinding.Value)
            {
                Parent = miscBindings,
                KeyBindingName = SurrenderBinding.DisplayName,
                BasicTooltipText = SurrenderBinding.Description,
                Enabled = true
            };
            surrenderKeyAssigner.BindingChanged += delegate
            {
                SurrenderBinding.Value = new KeyBinding(surrenderKeyAssigner.KeyBinding.PrimaryKey) { Enabled = true };
            };
            foreach (var binding in SkillBindings.Values)
            {
                var skillKeyAssigner = new KeybindingAssigner(binding.Value)
                {
                    Parent = skillsBindings,
                    KeyBindingName = binding.DisplayName,
                    BasicTooltipText = binding.Description,
                    Enabled = true
                };
                skillKeyAssigner.BindingChanged += delegate
                {
                    binding.Value = new KeyBinding(skillKeyAssigner.KeyBinding.PrimaryKey);
                };
            }
            var interactionKeyAssigner = new KeybindingAssigner(InteractionBinding.Value)
            {
                Parent = skillsBindings,
                KeyBindingName = InteractionBinding.DisplayName,
                BasicTooltipText = InteractionBinding.Description,
                Enabled = true
            };
            interactionKeyAssigner.BindingChanged += delegate
            {
                InteractionBinding.Value = new KeyBinding(interactionKeyAssigner.KeyBinding.PrimaryKey) { Enabled = true };
            };
            var dodgeKeyAssigner = new KeybindingAssigner(DodgeBinding.Value)
            {
                Parent = skillsBindings,
                KeyBindingName = DodgeBinding.DisplayName,
                BasicTooltipText = DodgeBinding.Description,
                Enabled = true
            };
            interactionKeyAssigner.BindingChanged += delegate
            {
                DodgeBinding.Value = new KeyBinding(dodgeKeyAssigner.KeyBinding.PrimaryKey) { Enabled = true };
            };
            return settingsPanel;
        }

        #endregion

        #region Editor Panel
        private Panel BuildEditorPanel(WindowBase wndw)
        {
            var editorPanel = new Panel
            {
                CanScroll = false,
                Size = wndw.ContentRegion.Size
            };
            var backButton = new BackButton(wndw)
            {
                Text = "Special Forces",
                NavTitle = "Editor",
                Parent = editorPanel,
                Location = new Point(20, 20)
            };
            var weaponSkills_button = new StandardButton() {
                Parent = editorPanel,
                Size = new Point(68, 38),
                Location = new Point(0, backButton.Bottom),
                //Icon = GameService.Content.GetTexture("")
            };
            var skills_button = new StandardButton() {
                Parent = weaponSkills_button.Parent,
                Size = new Point(68, 38),
                Location = new Point(weaponSkills_button.Right, weaponSkills_button.Location.Y),
                //Icon = GameService.Content.GetTexture("")
            };
            var fp_items = new Panel {
                Parent = editorPanel,
                Size = new Point(320 + SCROLLBAR_WIDTH + 9,
                    editorPanel.ContentRegion.Size.Y - backButton.Height - BOTTOM_MARGIN - TOP_MARGIN),
                Location = new Point(0, weaponSkills_button.Bottom),
                ShowTint = true,
                Title = "",
                CanScroll = true
            };
            var fp_weapon_skills = new FlowPanel {
                Parent = fp_items,
                Size = new Point(fp_items.ContentRegion.Size.X - SCROLLBAR_WIDTH, fp_items.ContentRegion.Size.Y),
                Location = new Point(0,0),
                ControlPadding = new Vector2(2,2),
                Visible = true
            };
            var fp_slot_skills = new FlowPanel {
                Parent = fp_items,
                Size = new Point(fp_items.ContentRegion.Size.X - SCROLLBAR_WIDTH, fp_items.ContentRegion.Size.Y),
                Location = new Point(0, 0),
                ControlPadding = new Vector2(2, 2),
                Visible = false
            };
            weaponSkills_button.Click += delegate(object sender, MouseEventArgs e)
            {
                fp_slot_skills.Hide();
                fp_weapon_skills.Show();
            };
            skills_button.Click += delegate (object sender, MouseEventArgs e)
            {
                fp_weapon_skills.Hide();
                fp_slot_skills.Show();
            };
            var contentPanel = new Panel
            {
                Location = new Point(fp_items.Right, BOTTOM_MARGIN + backButton.Bottom + 200),
                Size = new Point(editorPanel.ContentRegion.Width / 2 + ((editorPanel.ContentRegion.Width / 2) - fp_items.Width), editorPanel.Size.Y - BOTTOM_MARGIN - 250),
                Parent = editorPanel,
                ShowTint = true,
                ShowBorder = true,
                CanScroll = true
            };
            var descriptorPanel = new Panel
            {
                Location = new Point(fp_items.Right, BOTTOM_MARGIN),
                Size = new Point(editorPanel.Width / 2 + (editorPanel.Width - fp_items.Width), editorPanel.Height - contentPanel.Height),
                Parent = editorPanel,
                ShowBorder = true
            };
            var title_label = new Label
            {
                Parent = descriptorPanel,
                Size = new Point(100, 30),
                Location = new Point(LEFT_MARGIN, TOP_MARGIN + backButton.Bottom),
                Text = "Title:"
            };
            var patch_label = new Label
            {
                Parent = title_label.Parent,
                Size = new Point(100, 30),
                Location = new Point(LEFT_MARGIN, TOP_MARGIN + title_label.Bottom),
                Text = "Patch:"
            };
            var template_label = new Label
            {
                Parent = title_label.Parent,
                Size = new Point(100, 30),
                Location = new Point(LEFT_MARGIN, TOP_MARGIN + patch_label.Bottom),
                Text = "Template:"
            };
            var profession_label = new Label
            {
                Parent = title_label.Parent,
                Size = new Point(100, 30),
                Location = new Point(LEFT_MARGIN, TOP_MARGIN + template_label.Bottom),
                Text = _editorTemplate.GetClassFriendlyName()
            };
            var title_text = new TextBox
            {
                Parent = title_label.Parent,
                Size = new Point(200, 30),
                Location = new Point(title_label.Right, title_label.Location.Y),
                PlaceholderText = "Title",
                Text = _editorTemplate.Title ?? ""
            };
            var patch_text = new Label
            {
                Parent = title_label.Parent,
                Size = new Point(200, 30),
                Location = new Point(patch_label.Right, patch_label.Location.Y),
                Text = DateTime.Today.ToString("dd/MM/yyyy")
            };
            var template_text = new TextBox
            {
                Parent = title_label.Parent,
                Size = new Point(200, 30),
                Location = new Point(template_label.Right, template_label.Location.Y),
                PlaceholderText = "[TEMPLATE]",
                Text = _editorTemplate.Template == null ? "" : _editorTemplate.Template
            };
            var template_text_del = new StandardButton
            {
                Parent = title_label.Parent,
                Size = new Point(template_text.Height, template_text.Height),
                Location = new Point(template_text.Right + RIGHT_MARGIN, template_text.Location.Y),
                Text = "x"
            };
            template_text_del.Click += delegate { template_text.Text = ""; };
            return editorPanel;
        }

        #endregion

        #endregion
    }
}