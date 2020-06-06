using System;
using System.Collections.Generic;
using System.Diagnostics;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Intern;
using Blish_HUD.Input;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Special_Forces_Module.Parser;
using Special_Forces_Module.Persistance;
using Special_Forces_Module.Professions;
using static Blish_HUD.Controls.ScreenNotification;
using Color = Microsoft.Xna.Framework.Color;

namespace Special_Forces_Module.Player
{
    public class TemplatePlayer
    {
        private readonly Dictionary<string, GuildWarsControls> map = new Dictionary<string, GuildWarsControls>
        {
            {"swap", GuildWarsControls.SwapWeapons},
            {"1", GuildWarsControls.WeaponSkill1},
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

        private readonly Stopwatch Time;

        private readonly GuildWarsControls[] utilityswaps = new GuildWarsControls[3]
        {
            GuildWarsControls.UtilitySkill1,
            GuildWarsControls.UtilitySkill2,
            GuildWarsControls.UtilitySkill3
        };

        private EventHandler<EventArgs> _pressed;
        private IProfession _currentProfession;
        private RawTemplate _currentTemplate;
        private KeyBinding _currentKey;
        private List<Control> _controls;
        private HealthPoolButton _stopButton;
        public TemplatePlayer()
        {
            Time = new Stopwatch();
        }

        public void Dispose()
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
        public void Play(RawTemplate template)
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

            var _profession = template.GetProfession();

            if (!_profession.Equals(GameService.Gw2Mumble.PlayerCharacter.Profession.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                ShowNotification($"Your profession is {GameService.Gw2Mumble.PlayerCharacter.Profession}.\nRequired: {_profession}", NotificationType.Error);
                return;
            }

            var opener = template.Rotation.Opener.Split(null);
            if (opener.Length > 1)
                DoRotation(opener, 0);

            var loop = template.Rotation.Loop.Split(null);
            if (loop.Length > 1)
                DoRotation(loop, 0);
        }

        private void DoRotation(string[] rotation, int skillIndex)
        {
            var current = rotation[skillIndex].ToLowerInvariant();

            var split = current.Split('/');

            var duration = split.Length > 1 ? int.Parse(split[1]) : 0;

            Time.Restart();

            //TODO: Labels for dynamically resizing skills. Ex. attunements.
            if (current.Equals("drop") || current.Equals("take")) {

                _currentKey = SpecialForcesModule.ModuleInstance.InteractionBinding.Value;

                var interactLabel = new Label
                {
                    Parent = GameService.Graphics.SpriteScreen,
                    Size = new Point(300, 300),
                    Visible = GameService.GameIntegration.IsInGame,
                    Text = "Interact!",
                    Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size36, ContentService.FontStyle.Regular),
                    TextColor = Color.Red
                };
                _controls.Add(interactLabel);

            } else if (current.Equals("dodge")) {

                _currentKey = SpecialForcesModule.ModuleInstance.DodgeBinding.Value;

                var dodgeLabel = new Label
                {
                    Parent = GameService.Graphics.SpriteScreen,
                    Size = new Point(300, 300),
                    Visible = GameService.GameIntegration.IsInGame,
                    Text = "Dodge!",
                    Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size36, ContentService.FontStyle.Regular),
                    TextColor = Color.Red
                };
                _controls.Add(dodgeLabel);

            } else {

                var skill = split.Length > 1 ? map[split[0]] : map[current];

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

                var frame = new Image
                {
                    Parent = GameService.Graphics.SpriteScreen,
                    Size = new Point(scale, scale),
                    Texture = SpecialForcesModule.ModuleInstance.ContentsManager.GetTexture("skill_frame.png"),
                    Visible = GameService.GameIntegration.IsInGame,
                    Tint = Color.Red
                };
                frame.Location = new Point((GameService.Graphics.SpriteScreen.Width / 2 - frame.Width / 2) + X,
                    (GameService.Graphics.SpriteScreen.Height - frame.Height) - Y);
                _controls.Add(frame);
                var arrow = new Image
                {
                    Parent = GameService.Graphics.SpriteScreen,
                    Size = new Point(scale, scale),
                    Texture = GameService.Content.GetTexture("991944"),
                    Visible = frame.Visible,
                    Location = new Point(frame.Location.X, frame.Location.Y - scale)
                };
                _controls.Add(arrow);
                var bounce = GameService.Animation.Tweener
                    .Tween(arrow, new {Location = new Point(arrow.Location.X, arrow.Location.Y + 10)}, 0.7f).Repeat();
            }
            _pressed = delegate {
                    if (Time.Elapsed.TotalMilliseconds > duration)
                    {
                        _currentKey.Activated -= _pressed;
                        Time.Reset();

                        ResetBindings();
                        DisposeControls();

                        if (skillIndex < rotation.Length)
                            DoRotation(rotation, skillIndex + 1);
                    }
            };
            _currentKey.Activated += _pressed;
            _currentKey.Enabled = true;
        }
    }
}