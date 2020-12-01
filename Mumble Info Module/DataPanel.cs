using Blish_HUD;
using Blish_HUD.Controls;
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
        private int _leftMargin = 10;
        private int _rightMargin = 10;
        private int _topMargin = 5;
        private int _strokeDist = 1;
        private string _decimalFormat = "0.###";

        public DataPanel() {
            _font = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size36, ContentService.FontStyle.Regular);

            UpdateLocation(null, null);
            Graphics.SpriteScreen.Resized += UpdateLocation;
            Disposed += OnDisposed;
        }

        protected override CaptureType CapturesInput() => CaptureType.ForceNone;
        private void OnDisposed(object sender, EventArgs e) { /* NOOP */ }

        private void UpdateLocation(object sender, EventArgs e) => Location = new Point(0, 0);

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds) {
            if (!Gw2Mumble.IsAvailable || !GameIntegration.IsInGame) return;

            var left = HorizontalAlignment.Left;
            var top = VerticalAlignment.Top;

            var ctrlPressed = IsPressed(VK_LCONTROL);

            string text;
            int height;
            int width;
            Rectangle rect;

            var calcTopMargin = _topMargin;
            var calcLeftMargin = _leftMargin;

            #region Game
            
            text = $"{Gw2Mumble.RawClient.Name}  ";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _brown, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"({Gw2Mumble.Info.BuildId})/";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _green, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"(Mumble Link v{Gw2Mumble.Info.Version})";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _green, false, true, _strokeDist, left, top);

            #endregion

            calcTopMargin += height;
            calcLeftMargin = _leftMargin;

            #region Server

            text = $"{Gw2Mumble.Info.ServerAddress}";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _grey, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = ":";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"{Gw2Mumble.Info.ServerPort}  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _grey, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"- {Gw2Mumble.Info.ShardId}  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _grey, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"({Gw2Mumble.RawClient.Instance})";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _grey, false, true, _strokeDist, left, top);

            #endregion

            calcTopMargin += height * 2;
            calcLeftMargin = _leftMargin;

            #region Avatar

            text = "Avatar";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = $"{Gw2Mumble.PlayerCharacter.Name} - {Gw2Mumble.PlayerCharacter.Race}";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _softRed, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"  ({Gw2Mumble.PlayerCharacter.TeamColorId})";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _softYellow, false, true, _strokeDist, left, top);

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = "Profession";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
            
            calcLeftMargin += width;

            text = $"{Gw2Mumble.PlayerCharacter.Profession}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _yellow, false, true, _strokeDist, left, top);
            
            if (CurrentEliteSpec != null && CurrentEliteSpec.Elite && CurrentEliteSpec.Id == Gw2Mumble.PlayerCharacter.Specialization) {
                
                calcTopMargin += height;
                calcLeftMargin = _leftMargin * 3;

                text = "Elite";
                width = (int)_font.MeasureString(text).Width;
                height = (int)_font.MeasureString(text).Height;
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

                calcLeftMargin += width;

                text = ":  ";
                width = (int)_font.MeasureString(text).Width;
                height = Math.Max(height, (int)_font.MeasureString(text).Height);
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
            
                calcLeftMargin += width;

                text = $"{CurrentEliteSpec.Name}";
                width = (int)_font.MeasureString(text).Width;
                height = Math.Max(height, (int)_font.MeasureString(text).Height);
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _yellow, false, true, _strokeDist, left, top);
            
                calcLeftMargin += width;

                text = $"  ({CurrentEliteSpec.Id})";
                width = (int)_font.MeasureString(text).Width;
                height = Math.Max(height, (int)_font.MeasureString(text).Height);
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _softYellow, false, true, _strokeDist, left, top);

            }

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            var playerPos = Gw2Mumble.PlayerCharacter.Position;

            text = "X";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = "Y";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _lemonGreen, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = "Z";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = playerPos.X.ToString(ctrlPressed ? null : _decimalFormat);
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"  {playerPos.Y.ToString(ctrlPressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _lemonGreen, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"  {playerPos.Z.ToString(ctrlPressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            var playerFacing = Gw2Mumble.RawClient.AvatarFront;

            text = "Facing";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = playerFacing.X.ToString(ctrlPressed ? null : _decimalFormat);
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"  {playerFacing.Y.ToString(ctrlPressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _lemonGreen, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"  {playerFacing.Z.ToString(ctrlPressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            #endregion

            calcTopMargin += height * 2;
            calcLeftMargin = _leftMargin;

            #region Map

            text = "Map";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _blue, false, true, _strokeDist, left, top);

            if (CurrentMap != null && CurrentMap.Id == Gw2Mumble.CurrentMap.Id) {
                calcTopMargin += height;
                calcLeftMargin = _leftMargin * 3;

                text = $"{CurrentMap.Name}";
                width = (int)_font.MeasureString(text).Width;
                height = Math.Max(height, (int)_font.MeasureString(text).Height);
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);
                
                calcTopMargin += height;
                calcLeftMargin = _leftMargin * 3;

                text = "Region";
                width = (int)_font.MeasureString(text).Width;
                height = (int)_font.MeasureString(text).Height;
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _blue, false, true, _strokeDist, left, top);
             
                calcLeftMargin += width;

                text = ":  ";
                width = (int)_font.MeasureString(text).Width;
                height = Math.Max(height, (int)_font.MeasureString(text).Height);
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
                
                calcLeftMargin += width;

                text = $"{CurrentMap.RegionName}";
                width = (int)_font.MeasureString(text).Width;
                height = (int)_font.MeasureString(text).Height;
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

                calcTopMargin += height;
                calcLeftMargin = _leftMargin * 3;

                text = "Continent";
                width = (int)_font.MeasureString(text).Width;
                height = (int)_font.MeasureString(text).Height;
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _blue, false, true, _strokeDist, left, top);
             
                calcLeftMargin += width;

                text = ":  ";
                width = (int)_font.MeasureString(text).Width;
                height = Math.Max(height, (int)_font.MeasureString(text).Height);
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
               
                calcLeftMargin += width;

                text = $"{CurrentMap.ContinentName}";
                width = (int)_font.MeasureString(text).Width;
                height = Math.Max(height, (int)_font.MeasureString(text).Height);
                rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
                spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);
            }

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = "Id";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _blue, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"{Gw2Mumble.CurrentMap.Id}";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _yellow, false, true, _strokeDist, left, top);

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = "Type";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _blue, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"{Gw2Mumble.CurrentMap.Type} ({(Gw2Mumble.CurrentMap.IsCompetitiveMode ? "PvP" : "PvE")})";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            var playerLocationMap = Gw2Mumble.RawClient.PlayerLocationMap;

            text = "X";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = "Y";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _lemonGreen, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = playerLocationMap.X.ToString(ctrlPressed ? null : _decimalFormat);
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"  {playerLocationMap.X.ToString(ctrlPressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _lemonGreen, false, true, _strokeDist, left, top);

            #endregion

            calcTopMargin += height * 2;
            calcLeftMargin = _leftMargin;

            #region Camera

            text = "Camera";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _green, false, true, _strokeDist, left, top);

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            var cameraForward = Gw2Mumble.PlayerCamera.Forward;

            text = "Direction";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _green, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = cameraForward.X.ToString(ctrlPressed ? null : _decimalFormat);
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"  {cameraForward.Y.ToString(ctrlPressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _lemonGreen, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"  {cameraForward.Z.ToString(ctrlPressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            var cameraPosition = Gw2Mumble.PlayerCamera.Position;

            text = "Position";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _green, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = cameraPosition.X.ToString(ctrlPressed ? null : _decimalFormat);
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _red, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"  {cameraPosition.Y.ToString(ctrlPressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _lemonGreen, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"  {cameraPosition.Z.ToString(ctrlPressed ? null : _decimalFormat)}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = $"Field of View";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _green, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"{Gw2Mumble.PlayerCamera.FieldOfView}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _yellow, false, true, _strokeDist, left, top);

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = $"Near Plane Render Distance";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _green, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"{Gw2Mumble.PlayerCamera.NearPlaneRenderDistance}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _yellow, false, true, _strokeDist, left, top);

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = $"Far Plane Render Distance";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _green, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"{Gw2Mumble.PlayerCamera.FarPlaneRenderDistance}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _yellow, false, true, _strokeDist, left, top);

            #endregion

            calcTopMargin += height * 2;
            calcLeftMargin = _leftMargin;

            #region User Interface

            text = "User Interface";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _orange, false, true, _strokeDist, left, top);

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = "Size";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _orange, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"{Gw2Mumble.UI.UISize}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _yellow, false, true, _strokeDist, left, top);

            calcTopMargin += height;
            calcLeftMargin = _leftMargin * 3;

            text = "Text Input Focused";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _orange, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = ":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);

            calcLeftMargin += width;

            text = $"{Gw2Mumble.UI.IsTextInputFocused}";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(calcLeftMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _yellow, false, true, _strokeDist, left, top);

            #endregion

            calcTopMargin = _topMargin;
            var calcRightMargin = _rightMargin;

            #region Computer

            text = $"{_memoryUsage.ToString(ctrlPressed ? null : _decimalFormat)} MB";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            calcRightMargin += width;

            text = $":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
            
            calcRightMargin += width;

            text = $"Memory Usage";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _orange, false, true, _strokeDist, left, top);

            calcTopMargin += height;
            calcRightMargin = _rightMargin;

            text = $"{Environment.ProcessorCount}x {_cpuName}";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            calcRightMargin += width;

            text = $":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
            
            calcRightMargin += width;

            text = $"CPU";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _orange, false, true, _strokeDist, left, top);

            calcTopMargin += height;
            calcRightMargin = _rightMargin;

            text = $"{_cpuUsage.ToString(ctrlPressed ? null : _decimalFormat)}%";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            calcRightMargin += width;

            text = $":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
            
            calcRightMargin += width;

            text = $"CPU Usage";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _orange, false, true, _strokeDist, left, top);
            
            calcTopMargin += height;
            calcRightMargin = _rightMargin;

            text = $"{Graphics.GraphicsDevice.Adapter.Description}";
            width = (int)_font.MeasureString(text).Width;
            height = (int)_font.MeasureString(text).Height;
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _cyan, false, true, _strokeDist, left, top);

            calcRightMargin += width;

            text = $":  ";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, Color.LightGray, false, true, _strokeDist, left, top);
            
            calcRightMargin += width;

            text = $"GPU";
            width = (int)_font.MeasureString(text).Width;
            height = Math.Max(height, (int)_font.MeasureString(text).Height);
            rect = new Rectangle(Size.X - width - calcRightMargin, calcTopMargin, width, height);
            spriteBatch.DrawStringOnCtrl(this, text, _font, rect, _orange, false, true, _strokeDist, left, top);
            
            #endregion
        }
    }
}
