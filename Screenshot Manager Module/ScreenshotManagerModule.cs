﻿using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;
using Image = Blish_HUD.Controls.Image;
using Module = Blish_HUD.Modules.Module;
using Point = Microsoft.Xna.Framework.Point;
namespace Screenshot_Manager_Module
{

    [Export(typeof(Module))]
    public class ScreenshotManagerModule : Module
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(ScreenshotManagerModule));

        internal static ScreenshotManagerModule ModuleInstance;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion


        private Texture2D _icon64;
        private Texture2D _icon128;
        private Texture2D _portaitModeIcon128;
        private Texture2D _portaitModeIcon512;
        private Texture2D _trashcanClosedIcon64;
        private Texture2D _trashcanOpenIcon64;
        private Texture2D _trashcanClosedIcon128;
        private Texture2D _trashcanOpenIcon128;
        private Texture2D _inspectIcon;
        private Texture2D _incompleteHeartIcon;
        private Texture2D _completeHeartIcon;

        private readonly string[] _imageFilters = { "*.bmp", "*.jpg", "*.png" };
        private const int WindowWidth = 1024;
        private const int WindowHeight = 780;
        private const int PanelMargin = 5;
        private const int FileTimeOutMilliseconds = 10000;
        private const int MaxFileNameLength = 50;
        private readonly IEnumerable<char> _invalidFileNameCharacters;

        #region Localization Strings
        private string FailedToDeleteFileNotification = "Failed to delete image.";
        private string FailedToRenameFileNotification = "Unable to rename image:";
        private string ReasonFileInUse = "The image is in use by another process.";
        private string ReasonFileNotExisting = "The image doesn't exist anymore!";
        private string ReasonDublicateFileName = "A duplicate image name was specified!";
        private string ReasonInvalidFileName = "The image name contains invalid characters.";
        private string PromptChangeFileName = "Please enter a different image name.";
        private string InvalidFileNameCharactersHint = "The following characters are not allowed:";
        private string FileDeletionPrompt = "Delete Image?";
        private string RenameFileTooltipText = "Rename Image";
        private string DeleteFileTooltipText = "Delete Image";
        private string ZoomInThumbnailTooltipText = "Click To Zoom";
        #endregion

        private CornerIcon moduleCornerIcon;
        private WindowTab moduleTab;
        private Panel modulePanel;
        private List<FileSystemWatcher> screensPathWatchers;

        private FlowPanel thumbnailFlowPanel;
        private Dictionary<string, Panel> displayedThumbnails;

        [ImportingConstructor]
        public ScreenshotManagerModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(
            moduleParameters)
        {
            ModuleInstance = this;
            _invalidFileNameCharacters = Path.GetInvalidFileNameChars().Union(Path.GetInvalidPathChars());
        }

        protected override void DefineSettings(SettingCollection settings) {

        }

        protected void LoadTextures()
        {
            _icon64 = ContentsManager.GetTexture("screenshots_icon_64x64.png");
            //_icon128 = ContentsManager.GetTexture("screenshots_icon_128x128.png");
            _inspectIcon = ContentsManager.GetTexture("inspect.png");
            _portaitModeIcon128 = ContentsManager.GetTexture("portaitMode_icon_128x128.png");
            //_portaitModeIcon512 = ContentsManager.GetTexture("portaitMode_icon_128x128.png");
            _trashcanClosedIcon64 = ContentsManager.GetTexture("trashcanClosed_icon_64x64.png");
            _trashcanOpenIcon64 = ContentsManager.GetTexture("trashcanOpen_icon_64x64.png");
            //_trashcanClosedIcon128 = ContentsManager.GetTexture("trashcanClosed_icon_128x128.png");
            //_trashcanOpenIcon128 = ContentsManager.GetTexture("trashcanOpen_icon_128x128.png");
            _incompleteHeartIcon = ContentsManager.GetTexture("incomplete_heart.png");
            _completeHeartIcon = ContentsManager.GetTexture("complete_heart.png");
        }

        protected override void Initialize()
        {
            LoadTextures();
            screensPathWatchers = new List<FileSystemWatcher>();
            displayedThumbnails = new Dictionary<string, Panel>();
            foreach (string f in _imageFilters) {
                var w = new FileSystemWatcher
                {
                    Path = DirectoryUtil.ScreensPath,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                    Filter = f,
                    EnableRaisingEvents = true
                };
                w.Created += OnScreenshotCreated;
                w.Deleted += OnScreenshotDeleted;
                screensPathWatchers.Add(w);
            }
            modulePanel = BuildModulePanel(GameService.Overlay.BlishHudWindow);
            moduleTab = GameService.Overlay.BlishHudWindow.AddTab(Name, _icon64, modulePanel, 0);
            moduleCornerIcon = new CornerIcon()
            {
                IconName = Name,
                Icon = ContentsManager.GetTexture("screenshots_icon_64x64.png"),
                Priority = Name.GetHashCode()
            };
            moduleCornerIcon.Click += delegate
            {
                GameService.Overlay.BlishHudWindow.Show();
                GameService.Overlay.BlishHudWindow.Navigate(modulePanel);
                //TODO: Select the correct tab.
            };
        }
        private void ToggleFileSystemWatchers(object sender, EventArgs e)
        {
            foreach (var fsw in screensPathWatchers)
                fsw.EnableRaisingEvents = GameService.GameIntegration.Gw2HasFocus;
        }
        private void AddThumbnail(string filePath)
        {
            if (modulePanel == null || displayedThumbnails.ContainsKey(filePath)) return;
            Texture2D texture = null;
            Point textureSize = Point.Zero;
            var completed = false;
            var timeout = DateTime.Now.AddMilliseconds(FileTimeOutMilliseconds);
            while (!completed)
            {
                if (!File.Exists(filePath)) return;
                try
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        using (var originalImage = System.Drawing.Image.FromStream(fs))
                        {
                            using (var textureStream = new MemoryStream())
                            {
                                originalImage.Save(textureStream, originalImage.RawFormat);
                                var buffer = new byte[textureStream.Length];
                                textureStream.Position = 0;
                                textureStream.Read(buffer, 0, buffer.Length);
                                texture = Texture2D.FromStream(GameService.Graphics.GraphicsDevice, textureStream);
                                textureSize = new Point(texture.Width, texture.Height);
                                textureStream.Close();
                            }
                            originalImage.Dispose();
                        }
                        fs.Close();
                        completed = true;
                    }
                } catch (IOException e) {
                    if (DateTime.Now < timeout) continue;
                    Logger.Error(e.Message + e.StackTrace);
                    return;
                }
            }

            var thumbnailScale = PointExtensions.ResizeKeepAspect(textureSize, 300, 300);
            var thumbnail = new Panel
            {
                Parent = thumbnailFlowPanel,
                Size = new Point(thumbnailScale.X + 6, thumbnailScale.Y + 6),
                BackgroundColor = Color.Black
            };

            var tImage = new Blish_HUD.Controls.Image {
                Parent = thumbnail,
                Location = new Point(3, 3),
                Size = thumbnailScale,
                Texture = texture,
                Opacity = 0.8f
            };
            var inspectButton = new Blish_HUD.Controls.Image {
                Parent = thumbnail,
                Texture = _inspectIcon,
                Size = new Point(64, 64),
                Location = new Point((thumbnail.Width / 2) - 32, (thumbnail.Height / 2) - 32),
                Opacity = 0.0f
            };
            var deleteBackgroundTint = new Panel()
            {
                Parent = thumbnail,
                Size = thumbnail.Size,
                BackgroundColor = Color.Black,
                Opacity = 0.0f
            };
            var deleteLabel = new Label {
                Parent = thumbnail,
                Size = thumbnail.Size,
                TextColor = Color.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle,
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size24, ContentService.FontStyle.Regular), 
                Text = DeleteFileTooltipText,
                BasicTooltipText = ZoomInThumbnailTooltipText,
                StrokeText = true,
                ShowShadow = true,
                Opacity = 0.0f,
            };
            var fileNameTextBox = new TextBox {
                Parent = thumbnail,
                Size = new Point(thumbnail.Width / 2 + 20, 30),
                Location = new Point(PanelMargin, thumbnail.Height - 30 - PanelMargin),
                PlaceholderText = Path.GetFileNameWithoutExtension(filePath),
                MaxLength = MaxFileNameLength,
                BackgroundColor = Color.DarkBlue,
                BasicTooltipText = Path.GetFileNameWithoutExtension(filePath),
                Text = "",
                Opacity = 0.8f
            };
            var fileNameLengthLabel = new Label
            {
                Parent = thumbnail,
                Size = fileNameTextBox.Size,
                Location = new Point(fileNameTextBox.Location.X, fileNameTextBox.Location.Y - fileNameTextBox.Height),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Text = "0/" + MaxFileNameLength,
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size11, ContentService.FontStyle.Regular),
                TextColor = Color.Yellow,
                StrokeText = true,
                Visible = false
            };
            fileNameTextBox.TextChanged += delegate { fileNameLengthLabel.Text = fileNameTextBox.Text.Length + "/" + MaxFileNameLength; };
            fileNameTextBox.MouseEntered += delegate { GameService.Animation.Tweener.Tween(fileNameTextBox, new { Opacity = 1.0f }, 0.2f); };
            fileNameTextBox.MouseLeft += delegate { if (!fileNameTextBox.Focused) GameService.Animation.Tweener.Tween(fileNameTextBox, new { Opacity = 0.8f }, 0.2f); };
            var enterPressed = false;
            fileNameTextBox.EnterPressed += delegate
            {
                enterPressed = true;
                var newFileName = fileNameTextBox.Text.Trim();
                if (newFileName.Equals(String.Empty))
                {

                } else if (_invalidFileNameCharacters.Any(x => newFileName.Contains(x)))
                {
                    ScreenNotification.ShowNotification(ReasonInvalidFileName
                                                        + "\n" + PromptChangeFileName
                                                        + "\n" + InvalidFileNameCharactersHint + "\n"
                                                        + string.Join(" ", _invalidFileNameCharacters),
                        ScreenNotification.NotificationType.Error, null, 10);
                }
                else
                {
                    var newPath = Path.Combine(Directory.GetParent(Path.GetFullPath(filePath)).FullName,
                        newFileName + Path.GetExtension(filePath));
                    if (newPath.Equals(filePath, StringComparison.InvariantCultureIgnoreCase)) { }
                    else if (File.Exists(newPath))
                    {
                        ScreenNotification.ShowNotification(
                            FailedToRenameFileNotification + " " + ReasonDublicateFileName,
                            ScreenNotification.NotificationType.Error);
                    }
                    else if (!File.Exists(filePath))
                    {
                        ScreenNotification.ShowNotification(
                            FailedToRenameFileNotification + " " + ReasonFileNotExisting,
                            ScreenNotification.NotificationType.Error);
                        thumbnail?.Dispose();
                    }
                    else
                    {
                        var renameCompleted = false;
                        var renameTimeout = DateTime.Now.AddMilliseconds(FileTimeOutMilliseconds);
                        while (!renameCompleted)
                        {
                            try
                            {
                                File.Move(filePath, newPath);
                                displayedThumbnails.Remove(filePath);
                                displayedThumbnails.Add(newPath, thumbnail);
                                fileNameTextBox.PlaceholderText = Path.GetFileNameWithoutExtension(newPath);
                                fileNameTextBox.BasicTooltipText = Path.GetFileNameWithoutExtension(newPath);
                                filePath = newPath;
                                renameCompleted = true;
                            }
                            catch (IOException e)
                            {
                                if (DateTime.Now < renameTimeout) continue;
                                Logger.Error(e.Message + e.StackTrace);
                            }
                        }
                    }
                }

                if (!fileNameTextBox.MouseOver) GameService.Animation.Tweener.Tween(fileNameTextBox, new { Opacity = 0.6f }, 0.2f);
                fileNameTextBox.Text = "";
                enterPressed = false;
            };
            fileNameTextBox.InputFocusChanged += delegate
            {
                fileNameLengthLabel.Visible = fileNameTextBox.Focused;
                fileNameLengthLabel.Text = "0/" + MaxFileNameLength;
                fileNameTextBox.InputFocusChanged += delegate
                {
                    Task.Run(async delegate {
                        //InputFocusChanged needs to wait to not interfere with EnterPressed.
                        await Task.Delay(1).ContinueWith(delegate {
                            if (!enterPressed)
                                fileNameTextBox.Text = "";
                        });
                    });
                };
            };

            deleteLabel.MouseEntered += delegate
            {
                GameService.Animation.Tweener.Tween(inspectButton, new { Opacity = 1.0f }, 0.2f);
                GameService.Animation.Tweener.Tween(tImage, new { Opacity = 1.0f }, 0.45f);
            };
            deleteLabel.MouseLeft += delegate
            {
                GameService.Animation.Tweener.Tween(inspectButton, new { Opacity = 0.0f }, 0.2f);
                GameService.Animation.Tweener.Tween(tImage, new { Opacity = 0.8f }, 0.45f);
            };
            Panel inspectPanel = null;
            deleteLabel.Click += delegate {
                inspectPanel?.Dispose();
                var maxWidth = GameService.Graphics.Resolution.X - 100;
                var maxHeight = GameService.Graphics.Resolution.Y - 100;
                var inspectScale = PointExtensions.ResizeKeepAspect(GameService.Graphics.Resolution, maxWidth,
                    maxHeight);
                inspectPanel = new Panel() {
                    Parent = GameService.Graphics.SpriteScreen,
                    Size = new Point(inspectScale.X + 10, inspectScale.Y + 10),
                    Location = new Point((GameService.Graphics.SpriteScreen.Width / 2) - (inspectScale.X / 2), (GameService.Graphics.SpriteScreen.Height / 2) - (inspectScale.Y / 2)),
                    BackgroundColor = Color.Black,
                    ZIndex = 9999,
                    ShowBorder = true,
                    ShowTint = true,
                    Opacity = 0.0f
                };
                var inspImage = new Blish_HUD.Controls.Image() {
                    Parent = inspectPanel,
                    Location = new Point(5, 5),
                    Size = inspectScale,
                    Texture = texture
                };
                GameService.Animation.Tweener.Tween(inspectPanel, new { Opacity = 1.0f }, 0.35f);
                inspImage.Click += delegate { GameService.Animation.Tweener.Tween(inspectPanel, new { Opacity = 0.0f }, 0.15f).OnComplete(() => inspectPanel?.Dispose()); };
            };
            var deleteButton = new Image()
            {
                Parent = thumbnail,
                Texture = _trashcanClosedIcon64,
                Size = new Point(45,45),
                Location = new Point(thumbnail.Width - 45 - PanelMargin, thumbnail.Height - 45 - PanelMargin),
                Opacity = 0.5f
            };
            deleteButton.MouseEntered += delegate
            {
                deleteLabel.Text = FileDeletionPrompt;
                deleteButton.Texture = _trashcanOpenIcon64;
                GameService.Animation.Tweener.Tween(deleteButton, new { Opacity = 1.0f }, 0.2f);
                GameService.Animation.Tweener.Tween(deleteLabel, new { Opacity = 1.0f }, 0.2f);
                GameService.Animation.Tweener.Tween(deleteBackgroundTint, new { Opacity = 0.6f }, 0.35f);
                GameService.Animation.Tweener.Tween(fileNameTextBox, new { Opacity = 0.0f }, 0.2f);
            };
            deleteButton.MouseLeft += delegate {
                deleteButton.Texture = _trashcanClosedIcon64; 
                GameService.Animation.Tweener.Tween(deleteButton, new { Opacity = 0.8f }, 0.2f);
                GameService.Animation.Tweener.Tween(deleteLabel, new { Opacity = 0.0f }, 0.2f);
                GameService.Animation.Tweener.Tween(deleteBackgroundTint, new { Opacity = 0.0f }, 0.2f);
                GameService.Animation.Tweener.Tween(fileNameTextBox, new { Opacity = 0.8f }, 0.2f);
            };
            deleteButton.LeftMouseButtonReleased += delegate
            {
                if (!File.Exists(filePath)) {
                    thumbnail?.Dispose();
                } else {
                    var deletionCompleted = false;
                    var deletionTimeout = DateTime.Now.AddMilliseconds(FileTimeOutMilliseconds);
                    while (!deletionCompleted) {
                        try
                        {
                            fileNameTextBox.Text = "";
                            File.Delete(filePath);
                            deletionCompleted = true;
                        }
                        catch (IOException e)
                        {
                            if (DateTime.Now < deletionTimeout) continue;
                            Logger.Error(e.Message + e.StackTrace);
                            ScreenNotification.ShowNotification(FailedToDeleteFileNotification + " " + ReasonFileInUse, ScreenNotification.NotificationType.Error);
                        }
                    }
                }
            };
            thumbnail.Disposed += delegate
            {
                if (displayedThumbnails.ContainsKey(filePath)) 
                    displayedThumbnails.Remove(filePath);
            };
            displayedThumbnails.Add(filePath, thumbnail);
        }
        private void OnScreenshotCreated(object sender, FileSystemEventArgs e)
        {
            if (!displayedThumbnails.ContainsKey(e.FullPath))
                AddThumbnail(e.FullPath);
        }
        private void OnScreenshotDeleted(object sender, FileSystemEventArgs e) {
            if (displayedThumbnails.ContainsKey(e.FullPath))
                displayedThumbnails[e.FullPath].Dispose();
        }
        private Panel BuildModulePanel(WindowBase wnd)
        {
            var homePanel = new Panel()
            {
                Parent = wnd,
                Size = new Point(WindowWidth, WindowHeight),
                Location = new Point(GameService.Graphics.SpriteScreen.Width / 2 - WindowWidth / 2, GameService.Graphics.SpriteScreen.Height / 2 - WindowHeight / 2),
            };
            homePanel.Hidden += delegate {
                homePanel.Dispose();
            };
            thumbnailFlowPanel = new FlowPanel()
            {
                Parent = homePanel,
                Size = homePanel.ContentRegion.Size,
                Location = new Point(0,0),
                FlowDirection = ControlFlowDirection.LeftToRight,
                ControlPadding = new Vector2(5, 5),
                CanCollapse = false,
                CanScroll = true,
                Collapsed = false,
                ShowTint = true,
                ShowBorder = true
            };
            homePanel.Hidden += ToggleFileSystemWatchers;
            homePanel.Hidden += ToggleFileSystemWatchers;
            homePanel.Shown += LoadImages;
            return homePanel;
        }

        private async void LoadImages(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                if (Directory.Exists(DirectoryUtil.ScreensPath))
                {
                    foreach (var fileName in Directory.EnumerateFiles(DirectoryUtil.ScreensPath)
                        .Where(s => s.EndsWith(".bmp") || s.EndsWith(".jpg") || s.EndsWith(".png")))
                    {
                        AddThumbnail(Path.Combine(DirectoryUtil.ScreensPath, fileName));
                    }
                }
            });
        }
        protected override async Task LoadAsync() {

        }

        protected override void OnModuleLoaded(EventArgs e) {

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        protected override void Update(GameTime gameTime){ /* NOOP */ }

        /// <inheritdoc />
        protected override void Unload() {
            // Unload
            modulePanel.Hidden -= ToggleFileSystemWatchers;
            modulePanel.Hidden -= ToggleFileSystemWatchers;
            modulePanel.Shown -= LoadImages;
            GameService.Overlay.BlishHudWindow.RemoveTab(moduleTab);
            moduleTab = null;
            modulePanel?.Dispose();
            moduleCornerIcon?.Dispose();
            thumbnailFlowPanel?.Dispose();
            // avoiding resource leak
            for (var i=0; i<screensPathWatchers.Count; i++)
            {
                if (screensPathWatchers[i] == null) continue;
                screensPathWatchers[i].Created -= OnScreenshotCreated;
                screensPathWatchers[i].Deleted -= OnScreenshotDeleted;
                screensPathWatchers[i].Dispose();
                screensPathWatchers[i] = null;
            }
            displayedThumbnails.Clear();
            displayedThumbnails = null;
            // All static members must be manually unset
            ModuleInstance = null;
        }

    }

}
