using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Runtime.InteropServices;
using static Blish_HUD.GameService;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Nekres.Mumble_Info_Module
{
    internal class DataPanel : Container
    {
        private float _memoryUsage => MumbleInfoModule.ModuleInstance.MemoryUsage;
        private float _cpuUsage => MumbleInfoModule.ModuleInstance.CpuUsage;
        private string _cpuName => MumbleInfoModule.ModuleInstance.CpuName;
        private bool _captureMouseOnLCtrl => MumbleInfoModule.ModuleInstance.CaptureMouseOnLCtrl.Value;

        public Map CurrentMap;
        public Specialization CurrentEliteSpec;

        #region Colors

        private Color _grey = new Color(168, 168, 168);
        private Color _orange = new Color(252, 168, 0);
        private Color _red = new Color(252, 84, 84);
        private Color _softRed = new Color(250, 148, 148);
        private Color _lemonGreen = new Color(84, 252, 84);
        private Color _cyan = new Color(84, 252, 252);
        private Color _blue = new Color(0, 168, 252);
        private Color _green = new Color(0, 168, 0);
        private Color _brown = new Color(158, 81, 44);
        private Color _yellow = new Color(252, 252, 84);
        private Color _softYellow = new Color(250, 250, 148);
        private Color _borderColor = Color.AntiqueWhite;
        private Color _clickColor = Color.AliceBlue;

        #endregion

        #region PInvoke

            [DllImport("USER32.dll")]
            private static extern short GetKeyState(uint vk);
            private bool IsPressed(uint key){
                return Convert.ToBoolean(GetKeyState(key) & KEY_PRESSED);
            }
            private const uint KEY_PRESSED = 0x8000;
            private const uint VK_LCONTROL = 0xA2;
            private const uint VK_LSHIFT = 0xA0;

        #endregion

        private BitmapFont _font;
        private const int _leftMargin = 10;
        private const int _rightMargin = 10;
        private const int _topMargin = 5;
        private const int _strokeDist = 1;
        private const int _borderSize = 1;
        private const int _clipboardIndent = 4;
        private const char _clipboardIndentChar = ' ';
        private const string _clipboardMessage = "Copied!";
        private const string _decimalFormat = "0.###";

        private bool _isMousePressed;

        #region Info Elements

        private (string, bool) _gameInfo;
        private (string, bool) _avatarInfo;
        private (string, bool) _mapInfo;
        private (string, bool) _cameraInfo;
        private (string, bool) _userInterfaceInfo;
        private (string, bool) _computerInfo;

        private Rectangle _currentFocusBounds;
        private string _currentSingleInfo;

        #endregion

        public DataPanel() {
            _font = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size36, ContentService.FontStyle.Regular);

            UpdateLocation(null, null);
            Graphics.SpriteScreen.Resized += UpdateLocation;
            Disposed += OnDisposed;

            Input.Mouse.LeftMouseButtonReleased += OnLeftMouseButtonReleased;
            Input.Mouse.RightMouseButtonReleased += OnRightMouseButtonReleased;
            Input.Mouse.LeftMouseButtonPressed += OnMousePressed;
            Input.Mouse.RightMouseButtonPressed += OnMousePressed;
        }

        private void OnLeftMouseButtonReleased(object o, MouseEventArgs e) {
            _isMousePressed = false;
            if (Input.Mouse.Position.IsInBounds(_currentFocusBounds)) {
                ClipboardUtil.WindowsClipboardService.SetTextAsync(_currentSingleInfo);
                ScreenNotification.ShowNotification(_clipboardMessage);
            } else if (_gameInfo.Item2) {
                ClipboardUtil.WindowsClipboardService.SetTextAsync(_gameInfo.Item1);
                ScreenNotification.ShowNotification(_clipboardMessage);
            } else if (_avatarInfo.Item2) {
                ClipboardUtil.WindowsClipboardService.SetTextAsync(_avatarInfo.Item1);
                ScreenNotification.ShowNotification(_clipboardMessage);
            } else if (_mapInfo.Item2) {
                ClipboardUtil.WindowsClipboardService.SetTextAsync(_mapInfo.Item1);
                ScreenNotification.ShowNotification(_clipboardMessage);
            } else if (_cameraInfo.Item2) {
                ClipboardUtil.WindowsClipboardService.SetTextAsync(_cameraInfo.Item1);
                ScreenNotification.ShowNotification(_clipboardMessage);
            } else if (_userInterfaceInfo.Item2) {
                ClipboardUtil.WindowsClipboardService.SetTextAsync(_userInterfaceInfo.Item1);
                ScreenNotification.ShowNotification(_clipboardMessage);
            }
        }
        private void OnRightMouseButtonReleased(object o, MouseEventArgs e) {
            _isMousePressed = false;
            if (_computerInfo.Item2) {
                ClipboardUtil.WindowsClipboardService.SetTextAsync(_computerInfo.Item1);
                ScreenNotification.ShowNotification(_clipboardMessage);
            }
        }
        private void OnMousePressed(object o, MouseEventArgs e) => _isMousePressed = true;

        protected override CaptureType CapturesInput() => _captureMouseOnLCtrl && IsPressed(VK_LCONTROL) ? CaptureType.Mouse : CaptureType.ForceNone;
        private void OnDisposed(object sender, EventArgs e) {
            Input.Mouse.LeftMouseButtonReleased -= OnLeftMouseButtonReleased; 
            Input.Mouse.RightMouseButtonReleased -= OnRightMouseButtonReleased;
            Input.Mouse.LeftMouseButtonPressed -= OnMousePressed;
            Input.Mouse.RightMouseButtonPressed -= OnMousePressed;
        }

        private void UpdateLocation(object sender, EventArgs e) => Location = new Point(0, 0);

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds) {
            if (!GameIntegration.Gw2IsRunning || !Gw2Mumble.IsAvailable || !GameIntegration.IsInGame) return;

            var left = HorizontalAlignment.Left;
            var top = VerticalAlignment.Top;

            var togglePressed = IsPressed(VK_LCONTROL);

            string text;
            int height;
            int width;
            Rectangle rect;

            var calcTopMargin = _topMargin;
            var calcLeftMargin = _leftMargin;

            string focusedSingleInfo;
            Rectangle infoBounds;

            #region Game
            
            text = $"{Gw2Mumble.RawClient.Name}  ";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _brown, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _gameInfo.Item1 = text;
            focusedSingleInfo = text;

            calcLeftMargin += width;

            text = $"({Gw2Mumble.Info.BuildId})/";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _green, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _gameInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"(Mumble Link v{Gw2Mumble.Info.Version})";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _green, false, true, _strokeDist, left, top);

            _gameInfo.Item1 += text + '\n';
            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            focusedSingleInfo += text + '\n';

            if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            #endregion

            calcTopMargin += height;
            calcLeftMargin = _leftMargin;

            #region Server

            text = $"{Gw2Mumble.Info.ServerAddress}";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _grey, false, true, _strokeDist, left, top);

            infoBounds = rect;
            focusedSingleInfo = text;
            calcLeftMargin += width;

            text = ":";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"{Gw2Mumble.Info.ServerPort}  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _grey, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"- {Gw2Mumble.Info.ShardId}  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _grey, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"({Gw2Mumble.RawClient.Instance})";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _grey, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            focusedSingleInfo += text + '\n';

            if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            #endregion

            calcTopMargin += height * 2;
            calcLeftMargin = _leftMargin;

            #region Avatar

            text = "Avatar";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _avatarInfo.Item1 = text + '\n';

            if (_avatarInfo.Item2 = Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
            }

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = $"{Gw2Mumble.PlayerCharacter.Name} - {Gw2Mumble.PlayerCharacter.Race}";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _softRed, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _avatarInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
            focusedSingleInfo = text;
            calcLeftMargin += width;

            text = $"  ({Gw2Mumble.PlayerCharacter.TeamColorId})";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _softYellow, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _avatarInfo.Item1 += text + '\n';
            focusedSingleInfo += text + '\n';

            if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = "Profession";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);
            
            infoBounds = rect;
            _avatarInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
            focusedSingleInfo = text;
            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
            
            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _avatarInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"{Gw2Mumble.PlayerCharacter.Profession}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _yellow, false, true, _strokeDist, left, top);
            
            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _avatarInfo.Item1 += text + '\n';
            focusedSingleInfo += text;

            if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            if (CurrentEliteSpec != null && CurrentEliteSpec.Elite && CurrentEliteSpec.Id == Gw2Mumble.PlayerCharacter.Specialization) {
                
                calcTopMargin += height;
                calcLeftMargin = _leftMargin * 3;

                text = "Elite";
                width = (int)_font.MeasureString(text).Width;
                height = (int)_font.MeasureString(text).Height;
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);
            
                infoBounds = rect;
                _avatarInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
                focusedSingleInfo = text;
                calcLeftMargin += width;

                text = ":  ";
                width = (int)_font.MeasureString(text).Width;
                height = Math.Max(height, (int)_font.MeasureString(text).Height);
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
            
                RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
                _avatarInfo.Item1 += text;
                focusedSingleInfo += text;
                calcLeftMargin += width;

                text = $"{CurrentEliteSpec.Name}";
                width = (int)_font.MeasureString(text).Width;
                height = Math.Max(height, (int)_font.MeasureString(text).Height);
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _yellow, false, true, _strokeDist, left, top);
            
                RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
                _avatarInfo.Item1 += text;
                focusedSingleInfo += text;
                calcLeftMargin += width;

                text = $"  ({CurrentEliteSpec.Id})";
                width = (int)_font.MeasureString(text).Width;
                height = Math.Max(height, (int)_font.MeasureString(text).Height);
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _softYellow, false, true, _strokeDist, left, top);
                
                RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
                _avatarInfo.Item1 += text + '\n';
                focusedSingleInfo += text;

                if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                    DrawBorder(spriteBatch, infoBounds);
                    _currentSingleInfo = focusedSingleInfo;
                    _currentFocusBounds = infoBounds;
                }
            }

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            var playerPos = Gw2Mumble.PlayerCharacter.Position;

            text = "X";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _avatarInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
            focusedSingleInfo = text;
            calcLeftMargin += width;

            text = "Y";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _lemonGreen, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _avatarInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = "Z";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _avatarInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _avatarInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = playerPos.X.ToString(togglePressed ? null : _decimalFormat);
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _avatarInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"  {playerPos.Y.ToString(togglePressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _lemonGreen, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _avatarInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"  {playerPos.Z.ToString(togglePressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _avatarInfo.Item1 += text + '\n';
            focusedSingleInfo += text;

            if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            var playerFacing = Gw2Mumble.RawClient.AvatarFront;

            text = "Facing";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _avatarInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
            focusedSingleInfo = text;
            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
            
            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _avatarInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = playerFacing.X.ToString(togglePressed ? null : _decimalFormat);
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _avatarInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"  {playerFacing.Y.ToString(togglePressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _lemonGreen, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _avatarInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"  {playerFacing.Z.ToString(togglePressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _avatarInfo.Item1 += text + '\n';
            focusedSingleInfo += text;

            if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            #endregion

            calcTopMargin += height * 2;
            calcLeftMargin = _leftMargin;

            #region Map

            text = "Map";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _blue, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _mapInfo.Item1 = text + '\n';

            if (_mapInfo.Item2 = Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
            }

            if (CurrentMap != null && CurrentMap.Id == Gw2Mumble.CurrentMap.Id) {
                calcTopMargin += height;
                calcLeftMargin = _leftMargin * 3;

                text = $"{CurrentMap.Name}";
                width = (int)_font.MeasureString(text).Width;
                height = Math.Max(height, (int)_font.MeasureString(text).Height);
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);
                
                infoBounds = rect;
                _mapInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text + '\n';
                focusedSingleInfo = text + '\n';
                calcTopMargin += height;
                calcLeftMargin = _leftMargin * 3;

                if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                    DrawBorder(spriteBatch, infoBounds);
                    _currentSingleInfo = focusedSingleInfo;
                    _currentFocusBounds = infoBounds;
                }

                text = "Region";
                width = (int)_font.MeasureString(text).Width;
                height = (int)_font.MeasureString(text).Height;
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _blue, false, true, _strokeDist, left, top);
             
                infoBounds = rect;
                _mapInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
                focusedSingleInfo = text;
                calcLeftMargin += width;

                text = ":  ";
                width = (int)_font.MeasureString(text).Width;
                height = Math.Max(height, (int)_font.MeasureString(text).Height);
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
                
                RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
                _mapInfo.Item1 += text;
                focusedSingleInfo += text;
                calcLeftMargin += width;

                text = $"{CurrentMap.RegionName}";
                width = (int)_font.MeasureString(text).Width;
                height = (int)_font.MeasureString(text).Height;
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

                RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
                _mapInfo.Item1 += text + '\n';
                focusedSingleInfo += text + '\n';
                calcTopMargin += height;
                calcLeftMargin = _leftMargin * 3;

                if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                    DrawBorder(spriteBatch, infoBounds);
                    _currentSingleInfo = focusedSingleInfo;
                    _currentFocusBounds = infoBounds;
                }

                text = "Continent";
                width = (int)_font.MeasureString(text).Width;
                height = (int)_font.MeasureString(text).Height;
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _blue, false, true, _strokeDist, left, top);
             
                infoBounds = rect;
                _mapInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
                focusedSingleInfo = text;
                calcLeftMargin += width;

                text = ":  ";
                width = (int)_font.MeasureString(text).Width;
                height = Math.Max(height, (int)_font.MeasureString(text).Height);
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
               
                RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
                _mapInfo.Item1 += text;
                focusedSingleInfo += text;
                calcLeftMargin += width;

                text = $"{CurrentMap.ContinentName}";
                width = (int)_font.MeasureString(text).Width;
                height = Math.Max(height, (int)_font.MeasureString(text).Height);
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

                RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
                _mapInfo.Item1 += text + '\n';
                focusedSingleInfo += text + '\n';

                if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                    DrawBorder(spriteBatch, infoBounds);
                    _currentSingleInfo = focusedSingleInfo;
                    _currentFocusBounds = infoBounds;
                }

            }

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = "Id";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _blue, false, true, _strokeDist, left, top);
            
            infoBounds = rect;
            _mapInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
            focusedSingleInfo = text;
            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _mapInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"{Gw2Mumble.CurrentMap.Id}";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _yellow, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _mapInfo.Item1 += text + '\n';
            focusedSingleInfo += text + '\n';
            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            text = "Type";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _blue, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _mapInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
            focusedSingleInfo = text;
            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _mapInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"{Gw2Mumble.CurrentMap.Type} ({(Gw2Mumble.CurrentMap.IsCompetitiveMode ? "PvP" : "PvE")})";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _mapInfo.Item1 += text + '\n';
            focusedSingleInfo += text + '\n';
            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            var playerLocationMap = Gw2Mumble.RawClient.PlayerLocationMap;

            text = "X";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _mapInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
            focusedSingleInfo = text;
            calcLeftMargin += width;

            text = "Y";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _lemonGreen, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _mapInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _mapInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = playerLocationMap.X.ToString(togglePressed ? null : _decimalFormat);
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _mapInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"  {playerLocationMap.X.ToString(togglePressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _lemonGreen, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _mapInfo.Item1 += text + '\n';
            focusedSingleInfo += text + '\n';

            if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            #endregion

            calcTopMargin += height * 2;
            calcLeftMargin = _leftMargin;

            #region Camera

            text = "Camera";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _green, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _cameraInfo.Item1 = text + '\n';

            if (_cameraInfo.Item2 = Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
            }

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            var cameraForward = Gw2Mumble.PlayerCamera.Forward;

            text = "Direction";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _green, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _cameraInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
            focusedSingleInfo = text;
            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
            
            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _cameraInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = cameraForward.X.ToString(togglePressed ? null : _decimalFormat);
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _cameraInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"  {cameraForward.Y.ToString(togglePressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _lemonGreen, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _cameraInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"  {cameraForward.Z.ToString(togglePressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _cameraInfo.Item1 += text + '\n';
            focusedSingleInfo += text + '\n';

            if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            var cameraPosition = Gw2Mumble.PlayerCamera.Position;

            text = "Position";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _green, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _cameraInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
            focusedSingleInfo = text;
            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _cameraInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = cameraPosition.X.ToString(togglePressed ? null : _decimalFormat);
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _cameraInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"  {cameraPosition.Y.ToString(togglePressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _lemonGreen, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _cameraInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"  {cameraPosition.Z.ToString(togglePressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _cameraInfo.Item1 += text + '\n';
            focusedSingleInfo += text + '\n';

            if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = $"Field of View";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _green, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _cameraInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
            focusedSingleInfo = text;
            calcLeftMargin += width;

            text = $":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
            
            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _cameraInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"{Gw2Mumble.PlayerCamera.FieldOfView}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _yellow, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _cameraInfo.Item1 += text + '\n';
            focusedSingleInfo += text + '\n';

            if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = $"Near Plane Render Distance";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _green, false, true, _strokeDist, left, top);

            infoBounds = rect;
            focusedSingleInfo = text;
            _cameraInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
            calcLeftMargin += width;

            text = $":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _cameraInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"{Gw2Mumble.PlayerCamera.NearPlaneRenderDistance}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _yellow, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _cameraInfo.Item1 += text + '\n';
            focusedSingleInfo += text + '\n';

            if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = $"Far Plane Render Distance";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _green, false, true, _strokeDist, left, top);

            infoBounds = rect;
            focusedSingleInfo = text;
            _cameraInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
            calcLeftMargin += width;

            text = $":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _cameraInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"{Gw2Mumble.PlayerCamera.FarPlaneRenderDistance}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _yellow, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _cameraInfo.Item1 += text + '\n';
            focusedSingleInfo += text + '\n';

            if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            #endregion

            calcTopMargin += height * 2;
            calcLeftMargin = _leftMargin;

            #region User Interface

            text = "User Interface";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _orange, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _userInterfaceInfo.Item1 = text + '\n';

            if (_userInterfaceInfo.Item2 = Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
            }

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = "Size";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _orange, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _userInterfaceInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
            focusedSingleInfo = text;

            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _userInterfaceInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"{Gw2Mumble.UI.UISize}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _yellow, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _userInterfaceInfo.Item1 += text + '\n';
            focusedSingleInfo += text + '\n';

            if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = "Text Input Focused";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _orange, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _userInterfaceInfo.Item1 += new string(_clipboardIndentChar, _clipboardIndent) + text;
            focusedSingleInfo = text;
            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _userInterfaceInfo.Item1 += text;
            focusedSingleInfo += text;
            calcLeftMargin += width;

            text = $"{Gw2Mumble.UI.IsTextInputFocused}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _yellow, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _userInterfaceInfo.Item1 += text + '\n';
            focusedSingleInfo += text + '\n';

            if (Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            #endregion

            calcTopMargin = _topMargin;
            var calcRightMargin = _rightMargin;

            #region Computer

            text = $"{_memoryUsage.ToString(togglePressed ? null : _decimalFormat)} MB";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _computerInfo.Item1 = text;
            focusedSingleInfo = text;

            calcRightMargin += width;

            text = $":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
            
            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _computerInfo.Item1 = text + _computerInfo.Item1;
            focusedSingleInfo = text + focusedSingleInfo;

            calcRightMargin += width;

            text = $"Memory Usage";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _orange, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _computerInfo.Item1 = '\n' + text + _computerInfo.Item1;
            focusedSingleInfo = text + focusedSingleInfo + '\n';

            if (_computerInfo.Item2 = Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            calcTopMargin += height;
            calcRightMargin = _rightMargin;

            text = $"{Environment.ProcessorCount}x {_cpuName}";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _computerInfo.Item1 = text + _computerInfo.Item1;
            focusedSingleInfo = text;

            calcRightMargin += width;

            text = $":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
            
            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _computerInfo.Item1 = text + _computerInfo.Item1;
            focusedSingleInfo = text + focusedSingleInfo;
            calcRightMargin += width;

            text = $"CPU";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _orange, false, true, _strokeDist, left, top);

            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _computerInfo.Item1 = '\n' + text + _computerInfo.Item1;
            focusedSingleInfo = text + focusedSingleInfo + '\n';

            if (_computerInfo.Item2 = Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            calcTopMargin += height;
            calcRightMargin = _rightMargin;

            text = $"{_cpuUsage.ToString(togglePressed ? null : _decimalFormat)}%";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _computerInfo.Item1 = text + _computerInfo.Item1;
            focusedSingleInfo = text;

            calcRightMargin += width;

            text = $":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
            
            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _computerInfo.Item1 = text + _computerInfo.Item1;
            focusedSingleInfo = text + focusedSingleInfo;

            calcRightMargin += width;

            text = $"CPU Usage";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _orange, false, true, _strokeDist, left, top);
            
            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _computerInfo.Item1 = '\n' + text + _computerInfo.Item1;
            focusedSingleInfo = text + focusedSingleInfo + '\n';

            if (_computerInfo.Item2 = Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            calcTopMargin += height;
            calcRightMargin = _rightMargin;

            text = $"{Graphics.GraphicsDevice.Adapter.Description}";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            infoBounds = rect;
            _computerInfo.Item1 = text + _computerInfo.Item1;
            focusedSingleInfo = text;
            calcRightMargin += width;

            text = $":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
            
            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _computerInfo.Item1 = text + _computerInfo.Item1;
            focusedSingleInfo = text + focusedSingleInfo;

            calcRightMargin += width;

            text = $"GPU";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _orange, false, true, _strokeDist, left, top);
            
            RectangleExtension.Union(ref rect, ref infoBounds, out infoBounds);
            _computerInfo.Item1 = '\n' + text + _computerInfo.Item1;
            focusedSingleInfo = text + focusedSingleInfo + '\n';

            if (_computerInfo.Item2 = Input.Mouse.Position.IsInBounds(infoBounds)) {
                DrawBorder(spriteBatch, infoBounds);
                _currentSingleInfo = focusedSingleInfo;
                _currentFocusBounds = infoBounds;
            }

            #endregion
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds) {
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, _borderSize), _isMousePressed ? _clickColor : _borderColor);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.X, bounds.Y, _borderSize, bounds.Height), _isMousePressed ? _clickColor : _borderColor);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.X, bounds.Y + bounds.Height, bounds.Width, _borderSize), _isMousePressed ? _clickColor : _borderColor);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.X + bounds.Width, bounds.Y, _borderSize, bounds.Height), _isMousePressed ? _clickColor : _borderColor);
        }
    }
}
