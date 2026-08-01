using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Xml.Linq;

namespace msovideo_srgb
{
    public static class Config
    {
        private static XElement config;
        private static string _configPath = AppDomain.CurrentDomain.BaseDirectory + "config.xml";

        public static void SafeLoad()
        {
            try
            {
                Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n\nTry extracting the program elsewhere.");
                Application.Current.Shutdown();
            }
        }

        public static void Load()
        {
            if (File.Exists(_configPath))
            {
                config = XElement.Load(_configPath);

                if (config.Name == "monitors")
                {
                    var monitors = config;
                    config = new XElement("config");

                    foreach (var monitor in monitors.Descendants("monitor"))
                    {
                        var clamp_sdr = monitor.Attribute("clamp_sdr");
                        monitor.SetAttributeValue("clamp", clamp_sdr.Value);
                        clamp_sdr.Remove();
                    }
                    
                    monitors.Name = "preset";
                    monitors.SetAttributeValue("name", "Preset 1");

                    config.Add(monitors);
                }
            }
            else
            {
                config = new XElement("config");
                AddPreset();
            }
        }

        public static void SafeSave()
        {
            try
            {
                Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n\nTry extracting the program elsewhere.");
                Application.Current.Shutdown();
            }
        }

        public static void Save()
        {
            config.Save(_configPath);
        }

        public static void SaveMonitorData(MonitorData monitorData)
        {
            var preset = GetActivePreset();
            var monitor = preset.Descendants("monitor").FirstOrDefault(x => (string)x.Attribute("path") == monitorData.Path);
            if (monitor == null)
            {
                monitor = new XElement("monitor");
                monitor.SetAttributeValue("path", monitorData.Path);
                preset.Add(monitor);
            }
            SaveO(monitorData, monitor);
        }

        public static void LoadMonitorData(MonitorData monitor)
        {
            var preset = GetActivePreset();
            var element = preset.Descendants("monitor").FirstOrDefault(x => (string)x.Attribute("path") == monitor.Path);
            LoadO(monitor, element);
        }

        private static XElement[] GetPresets()
        {
            return config.Descendants("preset").ToArray();
        }

        private static XElement GetPreset(int presetId)
        {
            var presets = GetPresets();
            if (presetId >= presets.Length || presetId < 0) throw new Exception("Unknown preset");
            return presets[presetId];
        }

        private static XElement GetActivePreset()
        {
            return GetPreset(GetActivePresetId());
        }

        public static int GetActivePresetId()
        {
            int activePresetId = SafeGetAtributeValue(config, "active_preset", -1);
            var presets = GetPresets();

            if (activePresetId >= presets.Length || activePresetId < 0)
            {
                if(presets.Length == 0)
                {
                    AddPreset();
                }

                SetActivePreset(0);
                return 0;
            }

            return activePresetId;
        }

        public static void SetActivePreset(int presetId)
        {
            config.SetAttributeValue("active_preset", presetId);
        }

        private static Hotkey GetHotkey(XElement element)
        {
            uint keyModifier = SafeGetAtributeValue<uint>(element, "hotkey_key_modifier", 0);
            uint virtualKey = SafeGetAtributeValue<uint>(element, "hotkey_virtual_key", 0);
            return new Hotkey(keyModifier, virtualKey);     
        }

        private static void SetHotkey(XElement element, Hotkey hotkey)
        {
            element.SetAttributeValue("hotkey_key_modifier", (uint)hotkey.KeyModifier);
            element.SetAttributeValue("hotkey_virtual_key", (uint)hotkey.VirtualKey);
        }

        public static Preset[] GetAllPresets()
        {
            var presets = GetPresets();
            Preset[] presetNames = new Preset[presets.Length];

            for (int i = 0; i < presets.Length; i++)
            {
                var preset = presets[i];
                var name = SafeGetAtributeValue(preset, "name", "<unknown preset>");
                var hotkey = GetHotkey(preset);
                presetNames[i] = new Preset(i, name, hotkey);
            }

            return presetNames;
        }

        public static void RenamePreset(int presetId, string name)
        {
            var preset = GetPreset(presetId);
            preset.SetAttributeValue("name", name);
        }

        public static void SetPresetHotkey(int presetId, Hotkey hotkey)
        {
            var preset = GetPreset(presetId);
            SetHotkey(preset, hotkey);
        }

        public static void DeletePreset(int presetId)
        {
            var preset = GetPreset(presetId);
            preset.Remove();
            
            var activePreset = GetActivePresetId();
            var presets = GetPresets();

            if (activePreset <= presetId)
            {
                activePreset = Math.Min(activePreset, presets.Length - 1);
            }
            else
            {
                activePreset = Math.Min(activePreset -1, presets.Length - 1);
            }

            SetActivePreset(activePreset);
        }

        public static void AddPreset()
        {
            var presets = GetPresets().ToArray();
            
            XElement newConfigurection;
            if (presets.Length > 0)
            {
                var activePreset = GetActivePreset();
                newConfigurection = new XElement(activePreset);
            }
            else
            {
                newConfigurection = new XElement("preset");
            }

            newConfigurection.SetAttributeValue("name", $"Preset {presets.Length + 1}");
            config.Add(newConfigurection);

            SetActivePreset(presets.Length);
        }

        private static T SafeGetAtributeValue<T>(XElement element, string attributeName, T defaultValue)
        {
            return (T)SafeGetAtributeValue(element, attributeName, typeof(T), defaultValue);
        }

        private static object SafeGetAtributeValue(XElement element, string attributeName, Type type, object defaultValue)
        {
            var attribute = element.Attribute(attributeName);

            if (attribute == null) {
                return defaultValue;
            }

            try
            {
                var val = attribute.Value;
                object converted = Convert.ChangeType(val, type);
                return converted;
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        private static XElement SaveO<T>(T obj, XElement element)
        {
            foreach (var prop in typeof(T).GetProperties())
            {
                var persistent = prop.GetCustomAttribute<PersistentAttribute>();
                if (persistent != null)
                {
                    var value = prop.GetValue(obj);
                    element.SetAttributeValue(persistent.Key, value?.ToString());
                }
            }
            return element;
        }

        private static void LoadO<T>(T obj, XElement element)
        {
            if (element == null || obj == null) return;
            foreach (var prop in typeof(T).GetProperties())
            {
                var persistent = prop.GetCustomAttribute<PersistentAttribute>();
                
                if (persistent != null)
                {
                    var val = SafeGetAtributeValue(element, persistent.Key, prop.PropertyType, persistent.DefaultValue);
                    prop.SetValue(obj, val);
                }
            }
        }
    }
}
