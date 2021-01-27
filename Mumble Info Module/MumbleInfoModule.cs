using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;
using static Blish_HUD.GameService;
using Color = Microsoft.Xna.Framework.Color;

namespace Nekres.Mumble_Info_Module
{
    [Export(typeof(Module))]
    public class MumbleInfoModule : Module
    {

        //private static readonly Logger Logger = Logger.GetLogger(typeof(MumbleInfoModule));

        internal static MumbleInfoModule ModuleInstance;

        #region Service Managers
        /*internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;*/
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        [ImportingConstructor]
        public MumbleInfoModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        #region Settings

        private SettingEntry<KeyBinding> _toggleInfoBinding;
        internal SettingEntry<bool>      CaptureMouseOnLCtrl { get; private set; }

        #endregion

        public Map            CurrentMap  { get; private set; }
        public Specialization CurrentSpec { get; private set; }
        public float          MemoryUsage { get; private set; }
        public float          CpuUsage    { get; private set; }
        public string         CpuName     { get; private set; }

        private DataPanel          _dataPanel;
        private Label              _cursorPos;
        private PerformanceCounter _ramCounter;
        private PerformanceCounter _cpuCounter;
        private DateTime           _timeOutPc;

        protected override void DefineSettings(SettingCollection settings) {
            _toggleInfoBinding = settings.DefineSetting("ToggleInfoBinding", new KeyBinding(Keys.OemPlus),
                "Toggle Mumble Data", "Toggles the display of mumble data.");
            CaptureMouseOnLCtrl = settings.DefineSetting("ForceInterceptMouseOnCtrl", true, 
                "Capture mouse on [Left Control]", "Whether the mouse should be intercepted forcibly while [Left Control] is pressed.");
        }

        protected override void Initialize() {
            _timeOutPc = DateTime.Now;
            CpuName    = "";
        }

        protected override async Task LoadAsync() {
            await LoadPerformanceCounters();
            await QueryManagementObjects();
        }

        protected override void Update(GameTime gameTime) {
            UpdateCounter();
            if (_cursorPos != null)
            {
                _cursorPos.Text = PInvoke.IsLControlPressed() ? 
                                  $"X: {Input.Mouse.Position.X - Graphics.SpriteScreen.Width / 2}, Y: {Math.Abs(Input.Mouse.Position.Y - Graphics.SpriteScreen.Height)}" :
                                  $"X: {Input.Mouse.Position.X}, Y: {Input.Mouse.Position.Y}";
                _cursorPos.Location = new Point(Input.Mouse.Position.X + 50, Input.Mouse.Position.Y);
            }
        }

        protected override void OnModuleLoaded(EventArgs e) {
            _toggleInfoBinding.Value.Enabled = true;
            _toggleInfoBinding.Value.Activated += OnToggleInfoBindingActivated;
            Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;
            Gw2Mumble.PlayerCharacter.SpecializationChanged += OnSpecializationChanged;
            GameIntegration.Gw2Closed += OnGw2Closed;
            GameIntegration.Gw2Started += OnGw2Started;

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        private Task LoadPerformanceCounters() {
            return Task.Run(() => { 
                if (!GameIntegration.Gw2IsRunning) return;
                _ramCounter = new PerformanceCounter() {
                    CategoryName = "Process",
                    CounterName = "Working Set - Private",
                    InstanceName = GameIntegration.Gw2Process.ProcessName,
                    ReadOnly = true
                };
                _cpuCounter = new PerformanceCounter() {
                    CategoryName = "Process",
                    CounterName = "% Processor Time",
                    InstanceName = GameIntegration.Gw2Process.ProcessName,
                    ReadOnly = true
                };
            });
        }

        private Task QueryManagementObjects() {
            return Task.Run(() => {
                using (var mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor")) {
                    foreach (var o in mos.Get()) {
                        var mo = (ManagementObject) o;
                        var name = (string)mo["Name"];
                        if (name.Length < CpuName.Length) continue;
                        CpuName = name.Trim();
                    }
                }
            });
        }

        private void OnGw2Closed(object o, EventArgs e) {
            _dataPanel?.Dispose();
            _cursorPos?.Dispose();
            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();
        }

        private void OnGw2Started(object o, EventArgs e) => LoadPerformanceCounters();

        private void UpdateCounter() {
            if (!GameIntegration.Gw2IsRunning || _ramCounter == null || _cpuCounter == null) return;
            if (DateTime.Now < _timeOutPc) return;

            _timeOutPc = DateTime.Now.AddMilliseconds(250);
            MemoryUsage = _ramCounter.NextValue() / 1024 / 1024;
            CpuUsage = _cpuCounter.NextValue() / Environment.ProcessorCount;
        }

        private void OnToggleInfoBindingActivated(object o, EventArgs e) {
            if (!GameIntegration.Gw2IsRunning || Gw2Mumble.UI.IsTextInputFocused) return;
            if (_dataPanel != null) {
                _dataPanel.Dispose();
                _dataPanel = null;
            } else {
                BuildDisplay();
            }
            if (_cursorPos != null) {
                _cursorPos.Dispose();
                _cursorPos = null;
            } else {
                BuildCursorPosTooltip();
            }
        }

        private void BuildCursorPosTooltip()
        {
            _cursorPos?.Dispose();
            _cursorPos = new Label()
            {
                Parent = Graphics.SpriteScreen,
                Size = new Point(130, 20),
                StrokeText = true,
                ShowShadow = true,
                Location = new Point(Input.Mouse.Position.X, Input.Mouse.Position.Y),
                VerticalAlignment = VerticalAlignment.Top,
                ZIndex = -9999
            };
            Input.Mouse.RightMouseButtonPressed += OnRightMouseButtonPressed;
            Input.Mouse.RightMouseButtonReleased += OnRightMouseButtonReleased;
            _cursorPos.Disposed += (o, e) =>
            {
                Input.Mouse.RightMouseButtonPressed -= OnRightMouseButtonPressed;
                Input.Mouse.RightMouseButtonReleased -= OnRightMouseButtonReleased;
            };
        }

        private void OnRightMouseButtonPressed(object o, MouseEventArgs e)
        {
            if (_cursorPos == null) return;
            _cursorPos.TextColor = new Color(250, 250, 148);
        }

        private void OnRightMouseButtonReleased(object o, MouseEventArgs e)
        {
            if (_cursorPos == null) return;
            _cursorPos.TextColor = Color.White;
            ClipboardUtil.WindowsClipboardService.SetTextAsync(_cursorPos.Text);
            ScreenNotification.ShowNotification("Copied!");
        }

        private void BuildDisplay() {
            _dataPanel?.Dispose();
            _dataPanel = new DataPanel() {
                Parent = Graphics.SpriteScreen,
                Size = new Point(Graphics.SpriteScreen.Width, Graphics.SpriteScreen.Height),
                Location = new Point(0,0),
                ZIndex = -9999
            };

            GetCurrentMap(Gw2Mumble.CurrentMap.Id);
            GetCurrentElite(Gw2Mumble.PlayerCharacter.Specialization);
        }

        private void OnMapChanged(object o, ValueEventArgs<int> e) => GetCurrentMap(e.Value);
        private void OnSpecializationChanged(object o, ValueEventArgs<int> e) => GetCurrentElite(e.Value);

        private async void GetCurrentMap(int id) {
            await Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(id)
                    .ContinueWith(response => {
                        if (response.Exception != null || response.IsFaulted || response.IsCanceled) return;
                        var result = response.Result;
                        if (_dataPanel == null) return;
                        CurrentMap = result;
                    });
        }

        private async void GetCurrentElite(int id) {
            await Gw2ApiManager.Gw2ApiClient.V2.Specializations.GetAsync(id)
                    .ContinueWith(response => {
                        if (response.Exception != null || response.IsFaulted || response.IsCanceled) return;
                        var result = response.Result;
                        if (_dataPanel == null) return;
                        CurrentSpec = result;
                    });
        }

        /// <inheritdoc />
        protected override void Unload() {
            _dataPanel?.Dispose();
            _cursorPos?.Dispose();
            _ramCounter?.Close();
            _ramCounter?.Dispose();
            _cpuCounter?.Close();
            _cpuCounter?.Dispose();
            _toggleInfoBinding.Value.Activated -= OnToggleInfoBindingActivated;
            Gw2Mumble.CurrentMap.MapChanged -= OnMapChanged;
            Gw2Mumble.PlayerCharacter.SpecializationChanged -= OnSpecializationChanged;
            GameIntegration.Gw2Closed -= OnGw2Closed;
            GameIntegration.Gw2Started -= OnGw2Started;
            // All static members must be manually unset
            ModuleInstance = null;
        }
    }
}
