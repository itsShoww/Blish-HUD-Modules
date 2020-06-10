using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Intern;
using Blish_HUD.Input;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using Special_Forces_Module.Parser;
using Special_Forces_Module.Persistance;
using Special_Forces_Module.Professions;
using static Blish_HUD.Controls.ScreenNotification;
using Color = Microsoft.Xna.Framework.Color;
using VerticalAlignment = Blish_HUD.Controls.VerticalAlignment;

namespace Special_Forces_Module.Player
{
    internal class TemplatePlayer
    {
        private readonly Dictionary<string, GuildWarsControls> map = new Dictionary<string, GuildWarsControls>
        {
            {"swap", GuildWarsControls.SwapWeapons},
            {"drop", GuildWarsControls.SwapWeapons},
            {"1", GuildWarsControls.WeaponSkill1},
            {"auto", GuildWarsControls.WeaponSkill1},
            {"2", GuildWarsControls.WeaponSkill2},
            {"3", GuildWarsControls.WeaponSkill3},
            {"4", GuildWarsControls.WeaponSkill4},
            {"5", GuildWarsControls.WeaponSkill5},
            {"heal", GuildWarsControls.HealingSkill},
            {"6", GuildWarsControls.HealingSkill},
            {"7", GuildWarsControls.UtilitySkill1},
            {"8", GuildWarsControls.UtilitySkill2},
            {"9", GuildWarsControls.UtilitySkill3},
            {"0", GuildWarsControls.EliteSkill},
            {"elite", GuildWarsControls.EliteSkill},
            {"f1", GuildWarsControls.ProfessionSkill1},
            {"f2", GuildWarsControls.ProfessionSkill2},
            {"f3", GuildWarsControls.ProfessionSkill3},
            {"f4", GuildWarsControls.ProfessionSkill4},
            {"f5", GuildWarsControls.ProfessionSkill5},
            {"special", GuildWarsControls.SpecialAction}
        };


        private readonly GuildWarsControls[] utilityswaps = new GuildWarsControls[3]
        {
            GuildWarsControls.UtilitySkill1,
            GuildWarsControls.UtilitySkill2,
            GuildWarsControls.UtilitySkill3
        };
        
        private readonly Stopwatch _time;
        private EventHandler<EventArgs> _pressed;
        private IProfession _currentProfession;
        private RawTemplate _currentTemplate;
        private KeyBinding _currentKey;
        private List<Control> _controls;
        private HealthPoolButton _stopButton;
        private Regex _syntaxPattern;
        private BitmapFont _labelFont;
        private Effect _glowFx;
        internal TemplatePlayer()
        {
            _time = new Stopwatch();
            _syntaxPattern = new Regex(@"(?![\w\d]+(?=\+\d|/\d))(?<repetitions>(?<=\+)[1-9][0-9]*)|(?![\w\d]+(?=\+\d|/\d))(?<duration>(?<=/)[1-9][0-9]*)|(?<action>[\w\d]+)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
            _labelFont = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size36, ContentService.FontStyle.Regular);
            _glowFx = GameService.Content.ContentManager.Load<Effect>(@"effects\glow");
        }

        internal void Dispose()
        {
            ResetBindings();
            DisposeControls();

            _stopButton?.Dispose();
        }
        private void ResetBindings()
        {
            foreach (var binding in SpecialForcesModule.ModuleInstance.SkillBindings)
            {
                binding.Value.Value.Enabled = false;
                binding.Value.Value.Activated -= _pressed;
            }
        }
        private void DisposeControls()
        {
            if (_controls != null) {
                foreach (var c in _controls)
                {
                    c?.Dispose();
                }
                _controls.Clear();
            }
        }
        internal void Play(RawTemplate template)
        {
            Dispose();

            _currentProfession = TemplateParser.Parse(template);

            _controls = new List<Control>();

            _stopButton = new HealthPoolButton()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Visible = GameService.GameIntegration.IsInGame,
                Text = "Stop Rotation"
            };
            _stopButton.Click += delegate {
                Dispose();
            };
            _currentTemplate = template;

            var profession = template.GetProfession();

            if (!profession.Equals(GameService.Gw2Mumble.PlayerCharacter.Profession.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                ShowNotification($"Your profession is {GameService.Gw2Mumble.PlayerCharacter.Profession}.\nRequired: {profession}", NotificationType.Error);
                return;
            }

            var opener = template.Rotation.Opener.Split(null);
            if (opener.Length > 1)
                DoRotation(opener);

            var loop = template.Rotation.Loop.Split(null);
            if (loop.Length > 1)
                DoRotation(loop);
        }

        private async void DoRotation(string[] rotation, int skillIndex = 0, int repetitions = -1)
        {
            if (skillIndex >= rotation.Length) skillIndex = 0;

            var current = rotation[skillIndex].ToLowerInvariant();

            var duration = 0;

            var matchCollection = _syntaxPattern.Matches(current);
            foreach (Match match in matchCollection)
            {
                if (match.Groups["action"].Success)
                    current = match.Groups["action"].Value;
                if (match.Groups["duration"].Success)
                    duration = int.Parse(match.Groups["duration"].Value);
                if (match.Groups["repetitions"].Success && repetitions < 0)
                    repetitions = int.Parse(match.Groups["repetitions"].Value);
            }

            Control hint;

            if (current.Equals("take") || current.Equals("interact")) {

                _currentKey = SpecialForcesModule.ModuleInstance.InteractionBinding.Value;

                var text = "Interact! [" + _currentKey.GetBindingDisplayText() + ']';
                var textWidth = (int)_labelFont.MeasureString(text).Width;
                var textHeight = (int)_labelFont.MeasureString(text).Height;
                hint = new Label
                {
                    Parent = GameService.Graphics.SpriteScreen,
                    Size = new Point(textWidth, textHeight),
                    Visible = GameService.GameIntegration.IsInGame,
                    VerticalAlignment = VerticalAlignment.Middle,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    StrokeText = true,
                    ShowShadow = true,
                    Text = text,
                    Font = _labelFont,
                    TextColor = Color.Red
                };
                hint.Location = new Point((GameService.Graphics.SpriteScreen.Width / 2 - hint.Width / 2), (GameService.Graphics.SpriteScreen.Height - hint.Height) - 160);

            } else if (current.Equals("dodge")) {

                _currentKey = SpecialForcesModule.ModuleInstance.DodgeBinding.Value;

                var text = "Dodge! [" + _currentKey.GetBindingDisplayText() + ']';
                var textWidth = (int)_labelFont.MeasureString(text).Width;
                var textHeight = (int)_labelFont.MeasureString(text).Height;
                hint =  new Label
                {
                    Parent = GameService.Graphics.SpriteScreen,
                    Size = new Point(textWidth, textHeight),
                    Visible = GameService.GameIntegration.IsInGame,
                    VerticalAlignment = VerticalAlignment.Middle,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    StrokeText = true,
                    ShowShadow = true,
                    Text = text,
                    Font = _labelFont,
                    TextColor = Color.Red
                };
                hint.Location = new Point((GameService.Graphics.SpriteScreen.Width / 2 - hint.Width / 2), (GameService.Graphics.SpriteScreen.Height - hint.Height) - 160);

            } else {

                var skill = map[current];

                _currentKey = SpecialForcesModule.ModuleInstance.SkillBindings[skill].Value;

                switch (skill)
                {
                    case GuildWarsControls.UtilitySkill1:
                        skill = utilityswaps[_currentTemplate.Utilitykeys[0] - 1];
                        break;
                    case GuildWarsControls.UtilitySkill2:
                        skill = utilityswaps[_currentTemplate.Utilitykeys[1] - 1];
                        break;
                    case GuildWarsControls.UtilitySkill3:
                        skill = utilityswaps[_currentTemplate.Utilitykeys[2] - 1];
                        break;
                }

                var transforms = _currentProfession.GetTransformation(skill);

                var X = transforms.Item1;
                var Y = transforms.Item2;
                var scale = transforms.Item3 != 0 ? transforms.Item3 : 58;

                if (_currentProfession.IsDynamic(skill))
                {
                    var text = _currentProfession.GetDisplayText(skill);
                    var textWidth = (int)_labelFont.MeasureString(text).Width;
                    var textHeight = (int)_labelFont.MeasureString(text).Height;
                    hint = new Label
                    {
                        Parent = GameService.Graphics.SpriteScreen,
                        Size = new Point(textWidth, textHeight),
                        VerticalAlignment = VerticalAlignment.Top,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Visible = GameService.GameIntegration.IsInGame,
                        StrokeText = true,
                        ShowShadow = true,
                        Text = text,
                        Font = _labelFont,
                        TextColor = _currentProfession.GetDisplayTextColor(skill)
                    };
                    hint.Location = new Point((GameService.Graphics.SpriteScreen.Width / 2) + X,
                        GameService.Graphics.SpriteScreen.Height - hint.Height - Y);

                } else {

                    hint = new Image
                    {
                        Parent = GameService.Graphics.SpriteScreen,
                        Size = new Point(scale, scale),
                        Texture = SpecialForcesModule.ModuleInstance.ContentsManager.GetTexture("skill_frame.png"),
                        Visible = GameService.GameIntegration.IsInGame,
                        Tint = Color.Red
                    };
                    hint.Location = new Point((GameService.Graphics.SpriteScreen.Width / 2 - hint.Width / 2) + X,
                        (GameService.Graphics.SpriteScreen.Height - hint.Height) - Y);
                }
            }

            Label remainingDuration = null;
            if (repetitions >= 0) {

                var text = (repetitions + 1).ToString();
                var textWidth = (int)_labelFont.MeasureString(text).Width;
                var currentRepetition = new Label
                {
                    Parent = GameService.Graphics.SpriteScreen,
                    Size = new Point(textWidth, hint.Height),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Visible = GameService.GameIntegration.IsInGame,
                    StrokeText = true,
                    ShowShadow = true,
                    Text = text,
                    Font = _labelFont,
                    TextColor = Color.Red
                };
                currentRepetition.Location = new Point(hint.Location.X + (hint.Width / 2) - (currentRepetition.Width / 2),hint.Location.Y);
                _controls.Add(currentRepetition);

            } else if (_time.Elapsed.TotalMilliseconds < duration || duration >  0) {

                _time.Restart();

                var text = $"{(duration - _time.Elapsed.TotalMilliseconds) / 1000:0.00}".Replace(',','.');
                var textWidth = (int)_labelFont.MeasureString(text).Width;
                remainingDuration = new Label
                {
                    Parent = GameService.Graphics.SpriteScreen,
                    Size = new Point(textWidth, hint.Height),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Visible = GameService.GameIntegration.IsInGame,
                    StrokeText = true,
                    ShowShadow = true,
                    Text = text,
                    Font = _labelFont,
                    TextColor = Color.IndianRed
                };
                remainingDuration.Location = new Point(hint.Location.X + (hint.Width / 2) - (remainingDuration.Width / 2),hint.Location.Y);
                _controls.Add(remainingDuration);

                await Task.Run(() =>
                {
                    while (remainingDuration != null && _time.IsRunning && _time.Elapsed.TotalMilliseconds < duration)
                    {
                        text = $"{(duration - _time.Elapsed.TotalMilliseconds) / 1000:0.00}".Replace(',','.');
                        textWidth = (int) _labelFont.MeasureString(text).Width;
                        remainingDuration.Text = text;
                        remainingDuration.Size = new Point(textWidth, hint.Height);
                        remainingDuration.Location = new Point(hint.Location.X + (hint.Width / 2) - (remainingDuration.Width / 2), hint.Location.Y);
                    }
                });
            }

            _glowFx.Parameters["TextureWidth"].SetValue(58.0f);
            _glowFx.Parameters["GlowColor"].SetValue(Color.Black.ToVector4());
            var arrow = new Image
            {
                Parent = GameService.Graphics.SpriteScreen,
                Size = new Point(58,58),
                Texture = GameService.Content.GetTexture("991944"),
                SpriteBatchParameters = new SpriteBatchParameters()
                {
                    Effect = _glowFx
                },
                Visible = hint.Visible
            };
            arrow.Location = new Point(hint.Location.X + (hint.Width / 2) - (arrow.Width / 2),hint.Location.Y - arrow.Height);
            GameService.Animation.Tweener.Tween(arrow, new {Location = new Point(arrow.Location.X, arrow.Location.Y + 10)}, 0.7f).Repeat();
            _controls.Add(arrow);

            _controls.Add(hint);

            _pressed = delegate {
                    if (duration == 0 || _time.Elapsed.TotalMilliseconds > 0.9 * duration)
                    {
                        _currentKey.Activated -= _pressed;
                        _time.Reset();

                        ResetBindings();
                        DisposeControls();

                        if (repetitions > 0) 
                            DoRotation(rotation, skillIndex, repetitions - 1);
                        else if (skillIndex < rotation.Length)
                            DoRotation(rotation, skillIndex + 1);

                    }
            };
            _currentKey.Activated += _pressed;
            _currentKey.Enabled = true;
        }
    }
}