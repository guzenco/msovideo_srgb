using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.Win32;
namespace msovideo_srgb
{
    public class MainViewModel
    {
        public ObservableCollection<MonitorData> Monitors { get; }

        private string _startupName;
        private RegistryKey _startupKey;
        private string _startupValue;

        public MainViewModel()
        {
            Monitors = new ObservableCollection<MonitorData>();

            _startupName = "msovideo_srgb";
            _startupKey = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            _startupValue = Application.ExecutablePath + " -minimize";

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

        private void UpdateMonitors()
        {
            Monitors.Clear();
            Config.Load();

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

        public void SaveConfig()
        {    
            Config.Load();

            foreach (var m in Monitors)
            {
                Config.SaveMonitorData(m); 
            }

            Config.SafeSave();           
        }
    }
}