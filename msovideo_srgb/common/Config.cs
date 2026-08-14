using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Xml.Linq;
using static msovideo_srgb.SettingsSourceMap;

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
                App.CurrentApp.OnExit();
                Environment.Exit(1);
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
                App.CurrentApp.OnExit();
                Environment.Exit(1);
            }
        }

        public static void Save()
        {
            config.Save(_configPath);
        }

        public static void SaveMonitorData(MonitorData monitor)
        {
            var preset = GetActivePresetElement();
            var settingsSourceMap = GetSettingsSourceMap(preset);
            var propertiesBySources = settingsSourceMap.GetPropertiesBySources();
            foreach (var source in EnumExtensions.ToArray<Source>())
            {
                var properties = propertiesBySources[source];
                if (properties.Count == 0) continue;

                var element = GetMonitorElement(source, monitor.Path);
                SaveO(monitor, element, properties);
            }
        }

        public static void LoadMonitorData(MonitorData monitor)
        {
            var preset = GetActivePresetElement();
            var settingsSourceMap = GetSettingsSourceMap(preset);
            var propertiesBySources = settingsSourceMap.GetPropertiesBySources();
            foreach (var source in EnumExtensions.ToArray<Source>())
            {
                var properties = propertiesBySources[source];
                if (properties.Count == 0) continue;

                var element = GetMonitorElement(source, monitor.Path);
                LoadO(monitor, element, properties);
            }
        }

        private static XElement[] GetPresetElements()
        {
            return config.Descendants("preset").ToArray();
        }

        private static XElement GetPresetElement(int presetId)
        {
            var presets = GetPresetElements();
            if (presetId >= presets.Length || presetId < 0) throw new Exception("Unknown preset");
            return presets[presetId];
        }

        private static XElement GetActivePresetElement()
        {
            return GetPresetElement(GetActivePresetId());
        }

        private static XElement GetGlobalElement()
        {
            var global = config.Descendants("global").FirstOrDefault();
            if(global == null)
            {
                global = new XElement("global");
                config.Add(global);
                MoveElement(config, global, int.MinValue, false);
            }
            return global;
        }

        private static XElement GetSameElement(XElement element)
        {
            var same = element.Descendants("same").FirstOrDefault();
            if (same == null)
            {
                same = new XElement("same");
                element.Add(same);
                MoveElement(element, same, int.MinValue, false);
            }
            return same;
        }

        private static XElement GetMonitorElement(XElement element, string path)
        {
            var monitor = element.Descendants("monitor").FirstOrDefault(x => (string)x.Attribute("path") == path);
            if (monitor == null)
            {
                monitor = new XElement("monitor");
                monitor.SetAttributeValue("path", path);
                element.Add(monitor);
            }
            return monitor;
        }

        private static XElement GetMonitorElement(Source source, string path)
        {
            switch (source)
            {
                case Source.SEPARATE:
                    return GetMonitorElement(GetActivePresetElement(), path);
                case Source.SAME:
                    return GetSameElement(GetActivePresetElement());
                case Source.SEPARATE_GLOBAL:
                    return GetMonitorElement(GetGlobalElement(), path);
                case Source.SAME_GLOBAL:
                    return GetSameElement(GetGlobalElement());
            }
            return GetActivePresetElement();
        }

        public static int GetActivePresetId()
        {
            int activePresetId = SafeGetAtributeValue(config, "active_preset", -1);
            var presets = GetPresetElements();

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

        public static bool MovePreset(int presetId, int offset)
        {
            var preset = GetPresetElement(presetId);
            int newPresetId = MoveElement(config, preset, offset, true);
            if (newPresetId != -1)
            {
                int activePresetId = GetActivePresetId();
                if(activePresetId == presetId)
                {
                    SetActivePreset(newPresetId);
                }
                else if (activePresetId == newPresetId) {
                    SetActivePreset(activePresetId + (offset < 0 ? 1: -1));
                }
                return true;
            }
            return false;
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

        private static SettingsSourceMap GetSettingsSourceMap(XElement element)
        {
            SettingsSourceMap settingsSourceMap = new SettingsSourceMap();
            LoadO(settingsSourceMap, element);
            return settingsSourceMap;
        }

        private static void SetSettingsSourceMap(XElement element, SettingsSourceMap settingsSourceMap)
        {
            SaveO(settingsSourceMap, element);
        }

        public static Preset[] GetAllPresets()
        {
            var presets = GetPresetElements();

            if (presets.Length == 0)
            {
                AddPreset();
                presets = GetPresetElements();
            }

            Preset[] presetObjects = new Preset[presets.Length];

            for (int i = 0; i < presets.Length; i++)
            {
                var preset = presets[i];
                var name = SafeGetAtributeValue(preset, "name", "<unknown preset>");
                var hotkey = GetHotkey(preset);
                var settingsSourceMap = GetSettingsSourceMap(preset);
                presetObjects[i] = new Preset(i, name, hotkey, settingsSourceMap);
            }

            return presetObjects;
        }

        public static void RenamePreset(int presetId, string name)
        {
            var preset = GetPresetElement(presetId);
            preset.SetAttributeValue("name", name);
        }

        public static void SetPresetHotkey(int presetId, Hotkey hotkey)
        {
            var preset = GetPresetElement(presetId);
            SetHotkey(preset, hotkey);
        }

        public static void SetPresetSettingsSourceMap(int presetId, SettingsSourceMap settingsSourceMap)
        {
            var preset = GetPresetElement(presetId);
            SetSettingsSourceMap(preset, settingsSourceMap);
        }

        public static void DeletePreset(int presetId)
        {
            int activePresetId = GetActivePresetId();

            var preset = GetPresetElement(presetId);
            preset.Remove();
            
            var presets = GetPresetElements();

            if (activePresetId <= presetId)
            {
                activePresetId = Math.Min(activePresetId, presets.Length - 1);
            }
            else
            {
                activePresetId = Math.Min(activePresetId -1, presets.Length - 1);
            }

            SetActivePreset(activePresetId);
        }

        public static void AddPreset()
        {
            var presets = GetPresetElements().ToArray();
            
            XElement newPreset;
            if (presets.Length > 0)
            {
                var activePreset = GetActivePresetElement();
                newPreset = new XElement(activePreset);
            }
            else
            {
                newPreset = new XElement("preset");
            }

            newPreset.SetAttributeValue("name", $"Preset {presets.Length + 1}");

            var hotkey = GetHotkey(newPreset);
            hotkey.VirtualKey = VirtualKey.None;
            SetHotkey(newPreset, hotkey);

            config.Add(newPreset);

            SetActivePreset(presets.Length);
        }

        static int MoveElement(XElement parent, XElement element, int offset, bool sameNameOnly)
        {
            if (element == null) return -1;

            var elements = sameNameOnly ? parent.Elements(element.Name).ToList() : parent.Elements().ToList();
            int index = elements.IndexOf(element);
            if (index < 0) return -1;


            int newIndex = index + offset;
            newIndex = Math.Min(newIndex, elements.Count - 1);
            newIndex = Math.Max(newIndex, 0);

            if (index == newIndex) return -1;

            element.Remove();

            if (offset < 0)
            {
                elements[newIndex].AddBeforeSelf(element);
            }
            else
            {
                elements[newIndex].AddAfterSelf(element);
            }
            return newIndex;
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

        private static XElement SaveO<T>(T obj, XElement element, HashSet<string> properties = null)
        {
            foreach (var prop in typeof(T).GetProperties())
            {
                if (properties != null && !properties.Contains(prop.Name)) continue;
                
                var persistent = prop.GetCustomAttribute<PersistentAttribute>();

                if (persistent != null)
                {
                    var value = prop.GetValue(obj);
                    element.SetAttributeValue(persistent.Key, value?.ToString());
                }
            }
            return element;
        }

        private static void LoadO<T>(T obj, XElement element, HashSet<string> properties = null)
        {
            if (element == null || obj == null) return;
            foreach (var prop in typeof(T).GetProperties())
            {
                if (properties != null && !properties.Contains(prop.Name)) continue;
                
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
