using Blish_HUD;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;
using static Blish_HUD.GameService;
namespace Nekres.Mumble_Info_Module
{
    [Export(typeof(Module))]
    public class MumbleInfoModule : Module
    {

        //private static readonly Logger Logger = Logger.GetLogger(typeof(MumbleInfoModule));

        internal static MumbleInfoModule ModuleInstance;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        [ImportingConstructor]
        public MumbleInfoModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        #region Settings

        private SettingEntry<KeyBinding> ToggleInfoBinding;

        #endregion

        public Dictionary<int, Map> MapRepository { get; private set; }
        public Dictionary<int, Specialization> EliteSpecRepository { get; private set; }
        public float MemoryUsage { get; private set; }
        public float CpuUsage { get; private set; }
        public string CpuName { get; private set; }

        private DataPanel _dataPanel;
        private PerformanceCounter _ramCounter;
        private PerformanceCounter _cpuCounter;
        private DateTime _timeOutPc = DateTime.Now;

        protected override void DefineSettings(SettingCollection settings) {
            ToggleInfoBinding = settings.DefineSetting("ToggleInfoBinding", new KeyBinding(Keys.F12),
                "Toggle Mumble Data", "Toggles the display of mumble data.");
        }

        protected override void Initialize() {
            CpuName = "";
            MapRepository = new Dictionary<int, Map>();
            EliteSpecRepository = new Dictionary<int, Specialization>();
        }

        protected override async Task LoadAsync() {
            await Task.Run(() => {
                _ramCounter = new PerformanceCounter() {
                    CategoryName = "Process",
                    CounterName = "Working Set - Private",
                    InstanceName = GameIntegration.Gw2Process.ProcessName,
                    ReadOnly = true
                };
                _cpuCounter = new PerformanceCounter() {
                    CategoryName = "Process",
                    CounterName = "% Processor Time",
                    InstanceName =  GameIntegration.Gw2Process.ProcessName,
                    ReadOnly = true
                };
                ManagementObjectSearcher mos = 
                  new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
                    foreach (ManagementObject mo in mos.Get()) {
                        var name = (string)mo["Name"];
                        if (name.Length < CpuName.Length) continue;
                        CpuName = name.Trim();
                    }
                mos.Dispose();
            });
        }

        protected override void Update(GameTime gameTime) {
            UpdateCounter();
        }
        protected override void OnModuleLoaded(EventArgs e) {
            ToggleInfoBinding.Value.Enabled = true;
            ToggleInfoBinding.Value.Activated += OnToggleInfoBindingActivated;
            Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;
            Gw2Mumble.PlayerCharacter.SpecializationChanged += OnSpecializationChanged;
            // Base handler must be called
            base.OnModuleLoaded(e);
        }


        private void UpdateCounter() {
            if (DateTime.Now < _timeOutPc) return;

            _timeOutPc = DateTime.Now.AddMilliseconds(500);
            MemoryUsage = _ramCounter.NextValue() / 1024 / 1024;
            CpuUsage = _cpuCounter.NextValue() / Environment.ProcessorCount;
        }

        private void OnToggleInfoBindingActivated(object o, EventArgs e) {
            if (_dataPanel != null) {
                _dataPanel.Dispose();
                _dataPanel = null;
            } else
                BuildDisplay();
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
            if (MapRepository.ContainsKey(id)) {
                if (_dataPanel == null) return;
                _dataPanel.CurrentMap = MapRepository[id];
            } else {
                await Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(id)
                    .ContinueWith(response => {
                        if (response.Exception != null || response.IsFaulted || response.IsCanceled) return;
                        var result = response.Result;
                        MapRepository.Add(result.Id, result);
                        if (_dataPanel == null) return;
                        _dataPanel.CurrentMap = result;
                });
            }
        }

        private async void GetCurrentElite(int id) {
            if (EliteSpecRepository.ContainsKey(id)) {
                if (_dataPanel == null) return;
                _dataPanel.CurrentEliteSpec = EliteSpecRepository[id];
            } else {
                await Gw2ApiManager.Gw2ApiClient.V2.Specializations.GetAsync(id)
                    .ContinueWith(response => {
                        if (response.Exception != null || response.IsFaulted || response.IsCanceled) return;
                        var result = response.Result;
                        EliteSpecRepository.Add(result.Id, result);
                        if (_dataPanel == null) return;
                        _dataPanel.CurrentEliteSpec = result;
                });
            }
        }
        /// <inheritdoc />
        protected override void Unload() {
            _dataPanel?.Dispose();
            MapRepository?.Clear();
            EliteSpecRepository?.Clear();
            _ramCounter?.Close();
            _ramCounter?.Dispose();
            _cpuCounter?.Close();
            _cpuCounter?.Dispose();
            ToggleInfoBinding.Value.Activated -= OnToggleInfoBindingActivated;
            Gw2Mumble.CurrentMap.MapChanged -= OnMapChanged;
            Gw2Mumble.PlayerCharacter.SpecializationChanged -= OnSpecializationChanged;
            // All static members must be manually unset
            ModuleInstance = null;
        }

    }

}
