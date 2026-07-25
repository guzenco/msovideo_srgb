using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace msovideo_srgb
{
    public static class Config
    {
        private static XElement config;
        private static string _configPath = AppDomain.CurrentDomain.BaseDirectory + "config.xml";

        public static void Load()
        {
            if (File.Exists(_configPath))
            {
                config = XElement.Load(_configPath);
            }
            else
            {
                config = new XElement("monitors");
            }
        }

        public static void Save()
        {
            config.Save(_configPath);
        }

        public static void SaveMonitorData(MonitorData monitorData)
        {
            var monitor = config.Descendants("monitor").FirstOrDefault(x => (string)x.Attribute("path") == monitorData.Path);
            if (monitor == null)
            {
                monitor = new XElement("monitor");
                monitor.SetAttributeValue("path", monitorData.Path);
                config.Add(monitor);
            }
            SaveO(monitorData, monitor);
        }

        public static void LoadMonitorData(MonitorData monitor)
        {
            var element = config.Descendants("monitor").FirstOrDefault(x => (string)x.Attribute("path") == monitor.Path);
            LoadO(monitor, element);
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
                    var attribute = element.Attribute(persistent.Key);
                    if (attribute == null)
                    {
                        prop.SetValue(obj, persistent.DefaultValue);
                    }
                    else
                    {
                        var val = attribute.Value;
                        var converted = Convert.ChangeType(val, prop.PropertyType);
                        prop.SetValue(obj, converted);
                    }
                }
            }
        }
    }
}
