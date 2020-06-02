using System;
using System.Collections.Generic;
using System.Diagnostics;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Intern;
using Microsoft.Xna.Framework;
using Special_Forces_Module.Parser;
using Special_Forces_Module.Persistance;
using Special_Forces_Module.Professions;
using static Blish_HUD.Controls.ScreenNotification;

namespace Special_Forces_Module.Player
{
    public class TemplatePlayer
    {
        private readonly List<Control> _controls;

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
        private IProfession Profession;

        public TemplatePlayer()
        {
            Time = new Stopwatch();
            _controls = new List<Control>();
        }

        public void Dispose()
        {
            foreach (var binding in SpecialForcesModule.ModuleInstance.SkillBindings)
                binding.Value.Value.Activated -= _pressed;
            foreach (var c in _controls) c.Dispose();
        }

        public void Play(RawTemplate template)
        {
            Dispose();

            Profession = TemplateParser.Parse(template);

            if (!Profession.Equals(GameService.Gw2Mumble.PlayerCharacter.Profession))
            {
                ShowNotification("Rotation not for your current profession.", NotificationType.Error);
                return;
            }

            var opener = template.Rotation.Opener.Split(null);
            var loop = template.Rotation.Loop.Split(null);
            var rotation = new string[opener.Length + loop.Length];
            Array.Copy(opener, rotation, opener.Length);
            Array.Copy(loop, 0, rotation, opener.Length, loop.Length);

            DoRotation(rotation, 0, opener.Length, template.Utilitykeys);
        }

        private void DoRotation(string[] rotation, int i, int openerLength, int[] utility)
        {
            var current = rotation[i].ToLowerInvariant().Split('/');
            var skill = map[current[0]];

            switch (skill)
            {
                case GuildWarsControls.UtilitySkill1:
                    skill = utilityswaps[utility[0] - 1];
                    break;
                case GuildWarsControls.UtilitySkill2:
                    skill = utilityswaps[utility[1] - 1];
                    break;
                case GuildWarsControls.UtilitySkill3:
                    skill = utilityswaps[utility[2] - 1];
                    break;
            }

            var duration = 0;
            if (current.Length == 2) duration = int.Parse(current[1]);

            Time.Restart();

            var transforms = Profession.GetTransformation(skill);

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
            frame.Location = new Point(GameService.Graphics.SpriteScreen.Width / 2 - frame.Width / 2 + X,
                GameService.Graphics.SpriteScreen.Height - frame.Height - Y);
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

            var key = SpecialForcesModule.ModuleInstance.SkillBindings[skill].Value;

            _pressed = delegate
            {
                if (Time.Elapsed.TotalMilliseconds > duration)
                {
                    key.Activated -= _pressed;
                    Time.Reset();
                    frame.Dispose();
                    bounce.Cancel();
                    arrow.Dispose();
                    if (i >= rotation.Length - 1)
                        DoRotation(rotation, 0, openerLength, utility);
                    else
                        DoRotation(rotation, i + 1, openerLength, utility);
                }
            };
            key.Activated += _pressed;
            key.Enabled = true;
        }
    }
}