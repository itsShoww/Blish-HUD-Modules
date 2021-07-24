using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Gw2Sharp.ChatLinks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nekres.Kill_Proof_Module.Models;
using static Blish_HUD.GameService;
using static Nekres.Kill_Proof_Module.KillProofModule;

namespace Nekres.Kill_Proof_Module.Controls
{
    public class SmartPingMenu : IDisposable
    {
        private DateTimeOffset _smartPingCooldownSend = DateTimeOffset.Now;
        private DateTimeOffset _smartPingHotButtonTimeSend = DateTimeOffset.Now;
        private int _smartPingRepetitions;
        private int _smartPingCurrentReduction;
        private int _smartPingCurrentValue;
        private int _smartPingCurrentRepetitions;

        private Panel _smartPingMenu;

        private bool IsUiAvailable() => Gw2Mumble.IsAvailable && GameIntegration.IsInGame && !Gw2Mumble.UI.IsMapOpen;
        private void OnIsMapOpenChanged(object o, ValueEventArgs<bool> e) => ToggleSmartPingMenu(!e.Value, 0.45f);
        private void OnIsInGameChanged(object o, ValueEventArgs<bool> e) => ToggleSmartPingMenu(e.Value, 0.1f);
        private void OnSmartPingMenuEnabledSettingChanged(object o, ValueChangedEventArgs<bool> e) => ToggleSmartPingMenu(e.NewValue, 0.1f);
        private void OnSPM_RepetitionsChanged(object o, ValueChangedEventArgs<int> e) => _smartPingRepetitions = MathHelper.Clamp(e.NewValue, 10, 100) / 10;
        private void OnSelfUpdated(object o, ValueEventArgs<PlayerProfile> e) => BuildSmartPingMenu();

        public SmartPingMenu()
        {
            ModuleInstance.SmartPingMenuEnabled.SettingChanged += OnSmartPingMenuEnabledSettingChanged;

            OnSPM_RepetitionsChanged(ModuleInstance.SPM_Repetitions, new ValueChangedEventArgs<int>(0, ModuleInstance.SPM_Repetitions.Value));
            ModuleInstance.SPM_Repetitions.SettingChanged += OnSPM_RepetitionsChanged;

            GameIntegration.IsInGameChanged += OnIsInGameChanged;
            Gw2Mumble.UI.IsMapOpenChanged += OnIsMapOpenChanged;

            ModuleInstance.PartyManager.SelfUpdated += OnSelfUpdated;
        }

        private void ToggleSmartPingMenu(bool enabled, float tDuration)
        {
            if (enabled)
                BuildSmartPingMenu();
            else if (_smartPingMenu != null)
                Animation.Tweener.Tween(_smartPingMenu, new { Opacity = 0.0f }, tDuration).OnComplete(() => _smartPingMenu?.Dispose());
        }


        private void DoSmartPing(Token token)
        {
            var chatLink = new ItemChatLink { ItemId = token.Id };

            var totalAmount = ModuleInstance.PartyManager.Self.KillProof.GetToken(token.Id)?.Amount ?? 0;
            if (totalAmount <= 250)
            {
                chatLink.Quantity = Convert.ToByte(totalAmount);
                GameIntegration.Chat.Send(chatLink.ToString());
                return;
            }

            var hotButtonCooldownTime = DateTimeOffset.Now.Subtract(_smartPingHotButtonTimeSend);
            if (hotButtonCooldownTime.TotalMilliseconds > 500)
            {
                _smartPingCurrentReduction = 0;
                _smartPingCurrentValue = 0;
                _smartPingCurrentRepetitions = 0;
            }

            var rest = totalAmount - _smartPingCurrentValue % totalAmount;
            if (rest > 250)
            {

                var tempAmount = 250 - _smartPingCurrentReduction;

                if (_smartPingCurrentRepetitions < _smartPingRepetitions)
                {
                    _smartPingCurrentRepetitions++;
                }
                else
                {
                    _smartPingCurrentValue += tempAmount;
                    _smartPingCurrentReduction++;
                    _smartPingCurrentRepetitions = 0;
                }
                chatLink.Quantity = Convert.ToByte(tempAmount);
            }
            else
            {
                chatLink.Quantity = Convert.ToByte(rest);

                if (_smartPingCurrentRepetitions < _smartPingRepetitions)
                {
                    _smartPingCurrentRepetitions++;
                }
                else
                {
                    _smartPingCurrentReduction = 0;
                    _smartPingCurrentValue = 0;
                    _smartPingCurrentRepetitions = 0;
                    _smartPingCooldownSend = DateTimeOffset.Now;
                }
            }

            GameIntegration.Chat.Send(chatLink.ToString());
            _smartPingHotButtonTimeSend = DateTimeOffset.Now;
        }

        private void BuildSmartPingMenu()
        {
            _smartPingMenu?.Dispose();

            if (!ModuleInstance.SmartPingMenuEnabled.Value || !IsUiAvailable() || ModuleInstance.PartyManager.Self.KillProof == null) return;

            _smartPingMenu = new Panel
            {
                Parent = Graphics.SpriteScreen,
                Location = new Point(10, 38),
                Size = new Point(400, 40),
                Opacity = 0.0f,
                ShowBorder = true
            };

            _smartPingMenu.Resized += delegate { _smartPingMenu.Location = new Point(10, 38); };

            _smartPingMenu.MouseEntered += delegate { Animation.Tweener.Tween(_smartPingMenu, new { Opacity = 1.0f }, 0.45f); };

            var leftBracket = new Label
            {
                Parent = _smartPingMenu,
                Size = _smartPingMenu.Size,
                Font = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size20, ContentService.FontStyle.Regular),
                Text = "[",
                Location = new Point(0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            var quantity = new Label
            {
                Parent = _smartPingMenu,
                Size = new Point(30, 30),
                Location = new Point(10, -2)
            };
            var dropdown = new Dropdown
            {
                Parent = _smartPingMenu,
                Size = new Point(260, 20),
                Location = new Point(quantity.Right + 2, 3),
                SelectedItem = Properties.Resources.Loading___
            };
            _smartPingMenu.MouseLeft += delegate
            {
                //TODO: Check for when dropdown IsExpanded
                Animation.Tweener.Tween(_smartPingMenu, new { Opacity = 0.4f }, 0.45f);
            };
            var rightBracket = new Label
            {
                Parent = _smartPingMenu,
                Size = new Point(10, _smartPingMenu.Height),
                Font = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size20, ContentService.FontStyle.Regular),
                Text = "]",
                Location = new Point(dropdown.Right, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            var tokenStringSorted = new List<string>();
            foreach (var token in ModuleInstance.PartyManager.Self.KillProof.GetAllTokens())
            {
                var wing = ModuleInstance.Resources.GetWing(token);
                if (wing != null)
                    tokenStringSorted.Add($"W{ModuleInstance.Resources.GetAllWings().ToList().IndexOf(wing) + 1} | {token.Name}");
                else
                    tokenStringSorted.Add(token.Name);
            }
            tokenStringSorted.Sort((e1, e2) => string.Compare(e1, e2, StringComparison.InvariantCultureIgnoreCase));
            foreach (var tokenString in tokenStringSorted)
            {
                dropdown.Items.Add(tokenString);
            }

            dropdown.ValueChanged += delegate (object o, ValueChangedEventArgs e)
            {
                quantity.Text = ModuleInstance.PartyManager.Self.KillProof?.GetToken(e.CurrentValue)?.Amount.ToString() ?? "";
                ModuleInstance.SPM_DropdownSelection.Value = e.CurrentValue;
            };

            var oldSelection = dropdown.Items.FirstOrDefault(x => x.Equals(ModuleInstance.SPM_DropdownSelection.Value, StringComparison.InvariantCultureIgnoreCase));
            dropdown.SelectedItem = oldSelection ?? (dropdown.Items.Count > 0 ? dropdown.Items[0] : "");

            var sendButton = new Image
            {
                Parent = _smartPingMenu,
                Size = new Point(24, 24),
                Location = new Point(rightBracket.Right + 1, 0),
                Texture = Content.GetTexture("784268"),
                SpriteEffects = SpriteEffects.FlipHorizontally,
                BasicTooltipText = Properties.Resources.Send_To_Chat_nLeft_Click__Only_send_code_up_to_a_stack_s_worth__250x____nRight_Click__Send_killproof_me_total_amount_
            };
            var randomizeButton = new StandardButton
            {
                Parent = _smartPingMenu,
                Size = new Point(29, 24),
                Location = new Point(sendButton.Right + 7, 0),
                Text = ModuleInstance.SPM_WingSelection.Value,
                BackgroundColor = Color.Gray,
                BasicTooltipText = Properties.Resources.Random_token_from_selected_wing_when_pressing_Send_To_Chat__nLeft_Click__Toggle_nRight_Click__Iterate_wings
            };

            randomizeButton.PropertyChanged += delegate (object o, PropertyChangedEventArgs e) {
                if (!e.PropertyName.Equals(nameof(StandardButton.Text))) return;
                ModuleInstance.SPM_WingSelection.Value = randomizeButton.Text;
            };

            randomizeButton.LeftMouseButtonPressed += delegate
            {
                randomizeButton.Size = new Point(27, 22);
                randomizeButton.Location = new Point(sendButton.Right + 5, 2);
            };

            randomizeButton.LeftMouseButtonReleased += delegate
            {
                randomizeButton.Size = new Point(29, 24);
                randomizeButton.Location = new Point(sendButton.Right + 7, 0);
                randomizeButton.BackgroundColor =
                    randomizeButton.BackgroundColor == Color.Gray ? Color.LightGreen : Color.Gray;
            };
            randomizeButton.RightMouseButtonPressed += delegate
            {
                randomizeButton.Size = new Point(27, 22);
                randomizeButton.Location = new Point(sendButton.Right + 5, 2);
            };
            randomizeButton.RightMouseButtonReleased += delegate
            {
                randomizeButton.Size = new Point(29, 24);
                randomizeButton.Location = new Point(sendButton.Right + 7, 0);
                var allWings = ModuleInstance.Resources.GetAllWings().ToList();
                var current = ModuleInstance.Resources.GetWing(randomizeButton.Text);
                var wingIndex = allWings.IndexOf(current) + 1;
                var next = wingIndex + 1 <= allWings.Count() ? wingIndex + 1 : 1;
                randomizeButton.Text = $"W{next}";
            };
            sendButton.LeftMouseButtonPressed += delegate
            {
                sendButton.Size = new Point(22, 22);
                sendButton.Location = new Point(rightBracket.Right + 3, 2);
            };

            sendButton.LeftMouseButtonReleased += delegate
            {
                sendButton.Size = new Point(24, 24);
                sendButton.Location = new Point(rightBracket.Right + 1, 0);

                if (Gw2Mumble.UI.IsTextInputFocused) return;

                var cooldown = DateTimeOffset.Now.Subtract(_smartPingCooldownSend);
                if (cooldown.TotalSeconds < 1)
                {
                    ScreenNotification.ShowNotification("Your total has been reached. Cooling down.", ScreenNotification.NotificationType.Error);
                    return;
                }

                if (randomizeButton.BackgroundColor == Color.LightGreen)
                {
                    var wing = ModuleInstance.Resources.GetWing(randomizeButton.Text);
                    var wingTokens = wing.GetTokens();
                    var tokenSelection = ModuleInstance.PartyManager.Self.KillProof.GetAllTokens().Where(x => wingTokens.Any(y => y.Id.Equals(x.Id))).ToList();
                    if (tokenSelection.Count == 0) return;

                    DoSmartPing(tokenSelection.ElementAt(RandomUtil.GetRandom(0, tokenSelection.Count - 1)));

                }
                else
                {

                    var token = ModuleInstance.PartyManager.Self.KillProof.GetToken(dropdown.SelectedItem);
                    if (token == null) return;
                    DoSmartPing(token);
                }
            };

            sendButton.RightMouseButtonPressed += delegate
            {
                sendButton.Size = new Point(22, 22);
                sendButton.Location = new Point(rightBracket.Right + 3, 2);
            };

            var timeOutRightSend = new Dictionary<int, DateTimeOffset>();

            sendButton.RightMouseButtonReleased += delegate
            {
                sendButton.Size = new Point(24, 24);
                sendButton.Location = new Point(rightBracket.Right + 1, 0);

                if (ModuleInstance.PartyManager.Self.KillProof == null) return;

                var chatLink = new ItemChatLink();

                if (randomizeButton.BackgroundColor == Color.LightGreen)
                {
                    var wing = ModuleInstance.Resources.GetWing(randomizeButton.Text);
                    var wingTokens = wing.GetTokens();
                    var tokenSelection = ModuleInstance.PartyManager.Self.KillProof.GetAllTokens().Where(x => wingTokens.Any(y => y.Id.Equals(x.Id))).ToList();
                    if (tokenSelection.Count == 0) return;
                    var singleRandomToken = tokenSelection.ElementAt(RandomUtil.GetRandom(0, tokenSelection.Count - 1));
                    chatLink.ItemId = singleRandomToken.Id;
                    if (timeOutRightSend.Any(x => x.Key == chatLink.ItemId))
                    {
                        var cooldown = DateTimeOffset.Now.Subtract(timeOutRightSend[chatLink.ItemId]);
                        if (cooldown.TotalMinutes < 2)
                        {
                            var timeLeft = TimeSpan.FromMinutes(2 - cooldown.TotalMinutes);
                            var minuteWord = timeLeft.TotalSeconds > 119 ? $" {timeLeft:%m} minutes and" :
                                timeLeft.TotalSeconds > 59 ? $" {timeLeft:%m} minute and" : "";
                            var secondWord = timeLeft.Seconds > 9 ? $"{timeLeft:ss} seconds" :
                                timeLeft.Seconds > 1 ? $"{timeLeft:%s} seconds" : $"{timeLeft:%s} second";
                            ScreenNotification.ShowNotification(
                                $"You can't send your {singleRandomToken.Name} total\nwithin the next{minuteWord} {secondWord} again.",
                                ScreenNotification.NotificationType.Error);
                            return;
                        }

                        timeOutRightSend[chatLink.ItemId] = DateTimeOffset.Now;
                    }
                    else
                    {
                        timeOutRightSend.Add(chatLink.ItemId, DateTimeOffset.Now);
                    }

                    chatLink.Quantity = Convert.ToByte(1);
                    GameIntegration.Chat.Send($"Total: {ModuleInstance.PartyManager.Self.KillProof.GetToken(singleRandomToken.Id)?.Amount ?? 0} of {chatLink} (killproof.me/{ModuleInstance.PartyManager.Self.KillProof.KpId})");

                }
                else
                {

                    var token = ModuleInstance.PartyManager.Self.KillProof.GetToken(dropdown.SelectedItem);
                    if (token == null) return;
                    chatLink.ItemId = token.Id;
                    if (timeOutRightSend.Any(x => x.Key == chatLink.ItemId))
                    {
                        var cooldown = DateTimeOffset.Now.Subtract(timeOutRightSend[chatLink.ItemId]);
                        if (cooldown.TotalMinutes < 2)
                        {
                            var timeLeft = TimeSpan.FromMinutes(2 - cooldown.TotalMinutes);
                            var minuteWord = timeLeft.TotalSeconds > 119 ? $" {timeLeft:%m} minutes and" :
                                timeLeft.TotalSeconds > 59 ? $" {timeLeft:%m} minute and" : "";
                            var secondWord = timeLeft.Seconds > 9 ? $"{timeLeft:ss} seconds" :
                                timeLeft.Seconds > 1 ? $"{timeLeft:%s} seconds" : $"{timeLeft:%s} second";
                            ScreenNotification.ShowNotification(
                                $"You can't send your {token.Name} total\nwithin the next{minuteWord} {secondWord} again.",
                                ScreenNotification.NotificationType.Error);
                            return;
                        }

                        timeOutRightSend[chatLink.ItemId] = DateTimeOffset.Now;

                    }
                    else
                    {

                        timeOutRightSend.Add(chatLink.ItemId, DateTimeOffset.Now);

                    }

                    chatLink.Quantity = Convert.ToByte(1);
                    GameIntegration.Chat.Send(Properties.Resources.Total___0__of__1___killproof_me__2__.Replace("{0}", token.Amount.ToString()).Replace("{1}", chatLink.ToString()).Replace("{2}", ModuleInstance.PartyManager.Self.KillProof.KpId));
                }
            };
            _smartPingMenu.Disposed += delegate { Animation.Tweener.Tween(_smartPingMenu, new { Opacity = 0.0f }, 0.2f); };
            quantity.Text = ModuleInstance.PartyManager.Self.KillProof.GetToken(dropdown.SelectedItem)?.Amount.ToString() ?? "0";

            Animation.Tweener.Tween(_smartPingMenu, new { Opacity = 0.4f }, 0.35f);
            return;
        }

        public void Dispose()
        {
            _smartPingMenu?.Dispose();
            ModuleInstance.SmartPingMenuEnabled.SettingChanged -= OnSmartPingMenuEnabledSettingChanged;
            ModuleInstance.SPM_Repetitions.SettingChanged -= OnSPM_RepetitionsChanged;
            Gw2Mumble.UI.IsMapOpenChanged -= OnIsMapOpenChanged;
            GameIntegration.IsInGameChanged -= OnIsInGameChanged;
        }
    }
}
