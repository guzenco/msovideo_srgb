using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace msovideo_srgb
{
    public class SettingsSourceMap
    {
        [Description("Clamp")]
        [Persistent("source_clamp", (uint)Source.SEPARATE)]     
        public uint Clamp { get; set; }

        [Description("Target")]
        [Persistent("source_target", (uint)Source.SEPARATE)]
        public uint Target { get; set; }

        [Description("Curve resolution")]
        [Persistent("source_resolution", (uint)Source.SEPARATE)]
        public uint Resolution { get; set; }

        [Description("Target White")]
        [Persistent("source_target_white", (uint)Source.SEPARATE)]
        public uint TargetWhite { get; set; }

        [Description("Use EDID/ICC")]
        [Persistent("source_use_icc", (uint)Source.SEPARATE)]
        public uint UseIcc { get; set;}

        [Description("ICC path")]
        [Persistent("source_icc_path", (uint)Source.SEPARATE)]
        public uint ProfilePath { get; set; }

        [Description("Limit luminance")]
        [Persistent("source_limit_luminance", (uint)Source.SEPARATE)]
        public uint LimitLuminance { get; set; }

        [Description("Gamma")]
        [Persistent("source_gamma", (uint)Source.SEPARATE)]
        public uint Gamma { get; set; }

        [Description("Optimize matrix")]
        [Persistent("source_optimize_matrix", (uint)Source.SEPARATE)]
        public uint OptimizeMatrix { get; set; }

        [Description("MHC2 Profile settings")]
        [Persistent("source_report", (uint)Source.SEPARATE)]
        public uint Report { get; set; }

        [Description("Target White HDR")]
        [Persistent("source_target_white_hdr", (uint)Source.SEPARATE)]
        public uint TargetWhiteHDR { get; set; }

        [Description("Use ICC HDR")]
        [Persistent("source_use_icc_hdr", (uint)Source.SEPARATE)]
        public uint UseIccHDR { get; set; }

        [Description("ICC path HDR")]
        [Persistent("source_icc_path_hdr", (uint)Source.SEPARATE)]
        public uint ProfilePathHDR { get; set; }

        [Description("Gamma HDR")]
        [Persistent("source_gamma_hdr", (uint)Source.SEPARATE)]
        public uint GammaHDR { get; set; }

        [Description("Override HDR staic metadata")]
        [Persistent("source_override_metadata_hdr", (uint)Source.SEPARATE)]
        public uint OverrideMetadataHDR { get; set; }

        public List<Setting> Settings { get; set; }

        public bool Changed => Settings.Any(s=>s.Changed);

        public SettingsSourceMap()
        {
            Settings = new List<Setting>();
            foreach (var prop in typeof(SettingsSourceMap).GetProperties())
            {
                if (prop.PropertyType != typeof(uint)) continue;

                var desc = prop.GetCustomAttribute<DescriptionAttribute>();

                if (desc != null)
                {
                    Settings.Add(new Setting(desc.Description, this, prop));
                }
            }
        }

        public Dictionary<Source, HashSet<string>> GetPropertiesBySources()
        {
            Dictionary<Source, HashSet<string>> settingsBindings = new Dictionary<Source, HashSet<string>>();
            
            foreach (var source in EnumExtensions.ToArray<Source>())
            {
                settingsBindings.Add(source, new HashSet<string>());
            }

            foreach (var prop in typeof(MonitorData).GetProperties())
            {
                var bind = prop.GetCustomAttribute<BindToPropertyAttribute>();

                if (bind != null && bind.Property.DeclaringType == typeof(SettingsSourceMap))
                {
                    Source source = (Source)bind.Property.GetValue(this);
                    settingsBindings[source].Add(prop.Name);
                }
            }
            return settingsBindings;
        }

        public HashSet<string> GetSameSourceProperties()
        {
            var propertiesBySources = GetPropertiesBySources();
            return propertiesBySources[Source.SAME].Union(propertiesBySources[Source.SAME_GLOBAL]).ToHashSet();
        }

        public class Setting : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private object _sourceObject;
            private PropertyInfo _sourceProperty;
            private Source? _unchanged;

            public Source Source {
                get => (Source)_sourceProperty.GetValue(_sourceObject);
                set => _sourceProperty.SetValue(_sourceObject, value); 
            }

            public string Name { get; set; }

            public bool Same {
                get => Source.GetBit(SAME_BIT);
                set
                { 
                    if(_unchanged == null)
                    {
                        _unchanged = Source;
                    }

                    Source = Source.SetBit(SAME_BIT, value);
                    OnPropertyChanged(nameof(Same));
                }
            }

            public bool Global
            {
                get => Source.GetBit(GLOBAL_BIT);
                set
                {
                    if (_unchanged == null)
                    {
                        _unchanged = Source;
                    }

                    Source = Source.SetBit(GLOBAL_BIT, value);
                    OnPropertyChanged(nameof(Global));
                }
            }

            public bool Changed => _unchanged != null && _unchanged != Source;

            public Setting(string name, object sourceObject, PropertyInfo sourceProperty)
            {
                Name = name;
                _sourceObject = sourceObject;
                _sourceProperty = sourceProperty;
            }

            private void OnPropertyChanged(string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        public const int SAME_BIT = 0;
        public const int GLOBAL_BIT = 1;

        public enum Source : uint
        {
            SEPARATE = 0,
            SAME = 1 << SAME_BIT,
            SEPARATE_GLOBAL = 1 << GLOBAL_BIT,
            SAME_GLOBAL = 1 << SAME_BIT | 1 << GLOBAL_BIT
        }
    }
}
