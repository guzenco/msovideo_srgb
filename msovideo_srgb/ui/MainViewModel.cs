using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.Win32;
namespace msovideo_srgb
{
    public class MainViewModel
    {
        public ObservableCollection<MonitorData> Monitors { get; }

        private string _configPath;

        private string _startupName;
        private RegistryKey _startupKey;
        private string _startupValue;

        public MainViewModel()
        {
            Monitors = new ObservableCollection<MonitorData>();
            _configPath = AppDomain.CurrentDomain.BaseDirectory + "config.xml";

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
            List<XElement> config = null;
            if (File.Exists(_configPath))
            {
                config = XElement.Load(_configPath).Descendants("monitor").ToList();
            }

            var hdrPaths = DisplayConfigManager.GetHdrDisplayPaths();

            var number = 1;
            foreach (var display in WindowsDisplayAPI.Display.GetDisplays())
            {
                var path = display.DevicePath;

                var hdrActive = hdrPaths.Contains(path);

                var settings = config?.FirstOrDefault(x => (string)x.Attribute("path") == path);
                MonitorData monitor;
                if (settings != null)
                {
                    monitor = new MonitorData(this, number++, display, path, hdrActive,
                        (bool)settings.Attribute("clamp_sdr"),
                        (bool)settings.Attribute("use_icc"),
                        (string)settings.Attribute("icc_path"),
                        (bool)settings.Attribute("calibrate_gamma"),
                        (int)settings.Attribute("selected_gamma"),
                        (double)settings.Attribute("custom_gamma"),
                        (double)settings.Attribute("custom_percentage"),
                        (int?)settings.Attribute("target_white") ?? 0,
                        (double?)settings.Attribute("custom_white_x") ?? Colorimetry.D65.X,
                        (double?)settings.Attribute("custom_white_y") ?? Colorimetry.D65.Y,
                        (int)settings.Attribute("target"),
                        (int?)settings.Attribute("resolution") ?? 2,
                        (bool?)settings.Attribute("use_icc_hdr") ?? false,
                        (string)settings.Attribute("icc_path_hdr") ?? "",
                        (bool?)settings.Attribute("calibrate_gamma_hdr") ?? false,
                        (int?)settings.Attribute("target_peak") ?? 10000,
                        (double?)settings.Attribute("bpc_threshold") ?? 80,
                        (int?)settings.Attribute("target_white_hdr") ?? 0,
                        (double?)settings.Attribute("custom_white_hdr_x") ?? Colorimetry.D65.X,
                        (double?)settings.Attribute("custom_white_hdr_y") ?? Colorimetry.D65.Y);
                }
                else
                {
                    monitor = new MonitorData(this, number++, display, path, hdrActive, false);
                }

                Monitors.Add(monitor);
            }

            foreach (var monitor in Monitors)
            {
                monitor.ReapplyClamp();
            }
        }

        public void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            UpdateMonitors();
        }

        public void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode != PowerModes.Resume) return;
            OnDisplaySettingsChanged(null, null);
        }

        public void SaveConfig()
        {
            try
            {
                var xElem = new XElement("monitors",
                    Monitors.Select(x =>
                        new XElement("monitor", new XAttribute("path", x.Path),
                            new XAttribute("clamp_sdr", x.ClampSdr),
                            new XAttribute("use_icc", x.UseIcc),
                            new XAttribute("icc_path", x.ProfilePath),
                            new XAttribute("calibrate_gamma", x.CalibrateGamma),
                            new XAttribute("selected_gamma", x.SelectedGamma),
                            new XAttribute("custom_gamma", x.CustomGamma),
                            new XAttribute("custom_percentage", x.CustomPercentage),
                            new XAttribute("target_white", x.TargetWhite),
                            new XAttribute("custom_white_x", x.CustomWhiteX),
                            new XAttribute("custom_white_y", x.CustomWhiteY),
                            new XAttribute("target", x.Target),
                            new XAttribute("resolution", x.Resolution),
                            new XAttribute("use_icc_hdr", x.UseIccHDR),
                            new XAttribute("icc_path_hdr", x.ProfilePathHDR),
                            new XAttribute("calibrate_gamma_hdr", x.CalibrateGammaHDR),
                            new XAttribute("target_peak", x.TargetPeak),
                            new XAttribute("bpc_threshold", x.BPCThreshold),
                            new XAttribute("target_white_hdr", x.TargetWhiteHDR),
                            new XAttribute("custom_white_hdr_x", x.CustomWhiteHdrX),
                            new XAttribute("custom_white_hdr_y", x.CustomWhiteHdrY))));
                xElem.Save(_configPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n\nTry extracting the program elsewhere.");
                Environment.Exit(1);
            }
        }
    }
}