using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Win32;

namespace msovideo_srgb
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<MonitorData> Monitors { get; }
        public ObservableCollection<Preset> Presets { get; }

        private string _startupName;
        private RegistryKey _startupKey;
        private string _startupValue;

        public MainViewModel()
        {
            Monitors = new ObservableCollection<MonitorData>();
            Presets = new ObservableCollection<Preset>();

            _startupName = "msovideo_srgb";
            _startupKey = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            _startupValue = Application.ExecutablePath + " -minimize";

            Config.SafeLoad();

            UpdatePresets();
            UpdateMonitors();
        }

        public bool? RunAtStartup
        {
            get
            {
                var keyValue = _startupKey.GetValue(_startupName);

                if (keyValue == null)
                {
                    return false;
                }

                if ((string)keyValue == _startupValue)
                {
                    return true;
                }

                return null;
            }
            set
            {
                if (value == true)
                {
                    _startupKey.SetValue(_startupName, _startupValue);
                }
                else
                {
                    _startupKey.DeleteValue(_startupName);
                }
            }
        }
      
        public Preset ActivePreset
        {
            get {
                int activePresetId = Config.GetActivePresetId();
                
                if (activePresetId < Presets.Count)
                {
                    return Presets[activePresetId];
                }

                return null;
            }
            set
            {
                var preset = value;

                if (preset != null && Presets.Count > 0 && preset != ActivePreset) {

                    if (preset.Id == -1)
                    {
                        Config.AddPreset();
                        Config.SafeSave();
                        UpdatePresets();
                        return;
                    }

                    Config.SetActivePreset(preset.Id);
                    Config.SafeSave();
                    UpdateMonitors();
                    OnPropertyChanged(nameof(ActivePreset));
                }
            }
        }

        private void UpdateMonitors()
        {
            ActionScheduler.ClearAll();
            Monitors.Clear();

            var hdrPaths = DisplayConfigManager.GetHdrDisplayPaths();

            var number = 1;
            foreach (var display in WindowsDisplayAPI.Display.GetDisplays())
            {
                var path = display.DevicePath;

                var hdrActive = hdrPaths.Contains(path);
         
                MonitorData monitor = new MonitorData(this, number++, display, path, hdrActive);
                Config.LoadMonitorData(monitor);

                Monitors.Add(monitor);
            }

            ReapplyAll();
        }
        
        private void UpdatePresets()
        {
            GlobalEventsObserver.ClearHotKeys();
            Presets.Clear();

            var presets = Config.GetAllPresets();
            foreach (var preset in presets)
            {
                Presets.Add(preset);
                if (preset.Hotkey.IsBindable)
                {
                    GlobalEventsObserver.AddHotKey(preset.Id, preset.Hotkey);
                }
            }

            Presets.Add(new Preset (-1, "+"));

            OnPropertyChanged(nameof(ActivePreset));
        }

        public void RenamePreset(Preset preset, string name)
        {     
            Config.RenamePreset(preset.Id, name);
            Config.SafeSave();
            UpdatePresets();
        }

        public void DeletePreset(Preset preset)
        {
            bool isActive = preset == ActivePreset;

            Config.DeletePreset(preset.Id);
            Config.SafeSave();
            UpdatePresets();

            if (isActive)
            {
               UpdateMonitors();
            }
        }

        public void SetPresetHotkey(Preset preset, Hotkey hotkey)
        {
            Config.SetPresetHotkey(preset.Id, hotkey);
            Config.SafeSave();
            UpdatePresets();
        }

        public void ReapplyAll()
        {
            try
            {
                foreach (var monitor in Monitors)
                {
                    monitor.ReapplyClamp();
                }
            }
            catch (InvalidOperationException) { }
        }

        public void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            Thread.Sleep(100);
            UpdateMonitors();
        }

        public void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode != PowerModes.Resume) return;
            OnDisplaySettingsChanged(null, null);
        }

        public void OnHotkey(int id)
        {
            ActivePreset = Presets[id];
        }

        public void SaveConfig()
        {    
            foreach (var m in Monitors)
            {
                Config.SaveMonitorData(m); 
            }

            Config.SafeSave();           
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}