using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using EDIDParser;

namespace msovideo_srgb
{
    public class AdvancedViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private MonitorData _monitor;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.Target))]
        private int _target;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.Resolution))]
        private int _resolution;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.UseIcc))]
        private bool _useIcc;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.ProfilePath))]
        private string _profilePath;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.LimitLuminance))]
        private bool _limitLuminance;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.MaxLuminance))]
        private int _maxLuminance;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.CalibrateGamma))]
        private bool _calibrateGamma;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.SelectedGamma))]
        private int _selectedGamma;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.CustomGamma))]
        private double _customGamma;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.CustomPercentage))]
        private double _customPercentage;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.UseVcgt))]
        private bool _useVcgt;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.OptimizeMatrix))]
        private bool _optimizeMatrix;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.TargetWhite))]
        private int _targetWhite;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.CustomWhiteX))]
        private double _customWhiteX;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.CustomWhiteY))]
        private double _customWhiteY;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.ReportWhiteD65))]
        private bool _reportWhiteD65;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.ReportColorSpaceSRGB))]
        private bool _reportColorSpaceSRGB;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.ReportGammaSRGB))]
        private bool _reportGammaSRGB;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.ExcludeHdrMetadata))]
        private bool _excludeHdrMetadata;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.UseIccHDR))]
        private bool _useIccHDR;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.ProfilePathHDR))]
        private string _profilePathHDR;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.CalibrateGammaHDR))]
        private bool _calibrateGammaHDR;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.TargetPeak))]
        private int _targetPeak;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.BPCThreshold))]
        private double _bpcThreshold;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.TargetWhiteHDR))]
        private int _targetWhiteHDR;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.CustomWhiteHdrX))]
        private double _customWhiteHdrX;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.CustomWhiteHdrY))]
        private double _customWhiteHdrY;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.OverrideMetadataHDR))]
        private bool _overrideMetadataHDR;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.PeakLuminanceHDR))]
        private int _peakLuminanceHDR;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.MaxFullFrameLuminanceHDR))]
        private int _maxFullFrameLuminanceHDR;

        [BindToProperty(typeof(MonitorData), nameof(MonitorData.MinLuminanceHDR))]
        private double _minLuminanceHDR;

        public AdvancedViewModel()
        {
            throw new NotSupportedException();
        }

        public AdvancedViewModel(MonitorData monitor)
        {
            _monitor = monitor;

            foreach (var prop in typeof(AdvancedViewModel).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var bindTo = prop.GetCustomAttribute<BindToPropertyAttribute>();

                if (bindTo != null)
                {                    
                    var val = bindTo.Property.GetValue(monitor);
                    prop.SetValue(this, val);
                }
            }
        }

        public void ApplyChanges()
        {
            foreach (var prop in typeof(AdvancedViewModel).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var bindTo = prop.GetCustomAttribute<BindToPropertyAttribute>();

                if (bindTo != null)
                {
                    var valMonitor = bindTo.Property.GetValue(_monitor);
                    var valThis = prop.GetValue(this);

                    if (!valMonitor.Equals(valThis)) {
                        ChangedProperties.Add(bindTo.Property.Name);
                    }

                    bindTo.Property.SetValue(_monitor, valThis);
                }
            }
        }

        public ChromaticityCoordinates Coords => _monitor.Edid.DisplayParameters.ChromaticityCoordinates;

        public bool UseEdid
        {
            set
            {
                if (!value == _useIcc) return;
                _useIcc = !value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UseIcc));
                OnPropertyChanged(nameof(ProfilePathSDRWarning));
            }
            get => !_useIcc;
        }

        public bool UseIcc
        {
            set
            {
                if (value == _useIcc) return;
                _useIcc = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UseEdid));
                OnPropertyChanged(nameof(ProfilePathSDRWarning));
            }
            get => _useIcc;
        }

        public string ProfilePath
        {
            set
            {
                if (value == _profilePath) return;
                _profilePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProfileName));
                OnPropertyChanged(nameof(ProfilePathSDRWarning));
            }
            get => _profilePath;
        }

        public string ProfileName => Path.GetFileName(ProfilePath);

        public bool LimitLuminance
        {
            set
            {
                if (value == _limitLuminance) return;
                _limitLuminance = value;
                OnPropertyChanged();
            }
            get => _limitLuminance;
        }

        public int MaxLuminance
        {
            set
            {
                if (value == _maxLuminance) return;
                _maxLuminance = value;
                OnPropertyChanged();
            }
            get => _maxLuminance;
        }

        public bool CalibrateGamma
        {
            set
            {
                if (value == _calibrateGamma) return;
                _calibrateGamma = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UseVcgtVisibility));
            }
            get => _calibrateGamma;
        }

        public int SelectedGamma
        {
            set
            {
                if (value == _selectedGamma) return;
                _selectedGamma = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UseCustomGamma));
            }
            get => _selectedGamma;
        }

        public Visibility UseCustomGamma =>
            SelectedGamma == 2 || SelectedGamma == 3 ? Visibility.Visible : Visibility.Collapsed;

        public double CustomGamma
        {
            set
            {
                if (value == _customGamma) return;
                _customGamma = value;
                OnPropertyChanged();
            }
            get => _customGamma;
        }

        public bool UseVcgt
        {
            set
            {
                if (value == _useVcgt) return;
                _useVcgt = value;
                OnPropertyChanged();
            }
            get => _useVcgt;
        }

        public Visibility UseVcgtVisibility => CalibrateGamma ? Visibility.Collapsed : Visibility.Visible;

        public bool OptimizeMatrix
        {
            set
            {
                if (value == _optimizeMatrix) return;
                _optimizeMatrix = value;
                OnPropertyChanged();
            }
            get => _optimizeMatrix;
        }

        public int TargetWhite
        {
            set
            {
                if (value == _targetWhite) return;
                _targetWhite = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UseCustomWhite));
            }
            get => _targetWhite;
        }

        public Visibility UseCustomWhite => TargetWhite == 4 ? Visibility.Visible : Visibility.Collapsed;

        public double CustomWhiteX
        {
            set
            {
                if (value == _customWhiteX) return;
                _customWhiteX = value;
                OnPropertyChanged();
            }
            get => _customWhiteX;
        }

        public double CustomWhiteY
        {
            set
            {
                if (value == _customWhiteY) return;
                _customWhiteY = value;
                OnPropertyChanged();
            }
            get => _customWhiteY;
        }

        public bool ReportWhiteD65
        {
            set
            {
                if (value == _reportWhiteD65) return;
                _reportWhiteD65 = value;
                OnPropertyChanged();
            }
            get => _reportWhiteD65;
        }

        public bool ReportColorSpaceSRGB
        {
            set
            {
                if (value == _reportColorSpaceSRGB) return;
                _reportColorSpaceSRGB = value;
                OnPropertyChanged();
            }
            get => _reportColorSpaceSRGB;
        }

        public bool ReportGammaSRGB
        {
            set
            {
                if (value == _reportGammaSRGB) return;
                _reportGammaSRGB = value;
                OnPropertyChanged();
            }
            get => _reportGammaSRGB;
        }

        public bool ExcludeHdrMetadata
        {
            set
            {
                if (value == _excludeHdrMetadata) return;
                _excludeHdrMetadata = value;
                OnPropertyChanged();
            }
            get => _excludeHdrMetadata;
        }

        public int Target
        {
            set
            {
                if (value == _target) return;
                _target = value;
                OnPropertyChanged();
            }
            get => _target;
        }

        public int Resolution
        {
            set
            {
                if (value == _resolution) return;
                _resolution = value;
                OnPropertyChanged();
            }
            get => _resolution;
        }

        public bool UseIccHDR
        {
            set
            {
                if (value == _useIccHDR) return;
                _useIccHDR = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProfilePathHDRWarning));
            }
            get => _useIccHDR;
        }

        public string ProfilePathHDR
        {
            set
            {
                if (value == _profilePathHDR) return;
                _profilePathHDR = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProfileNameHDR));
                OnPropertyChanged(nameof(ProfilePathHDRWarning));
            }
            get => _profilePathHDR;
        }

        public string ProfileNameHDR => Path.GetFileName(ProfilePathHDR);

        public bool CalibrateGammaHDR
        {
            set
            {
                if (value == _calibrateGammaHDR) return;
                _calibrateGammaHDR = value;
                OnPropertyChanged();
            }
            get => _calibrateGammaHDR;
        }

        public int TargetPeak
        {
            set
            {
                if (value == _targetPeak) return;
                _targetPeak = value;
                OnPropertyChanged();
            }
            get => _targetPeak;
        }

        public double BPCThreshold
        {
            set
            {
                if (value == _bpcThreshold) return;
                _bpcThreshold = value;
                OnPropertyChanged();
            }
            get => _bpcThreshold;
        }

        public int TargetWhiteHDR
        {
            set
            {
                if (value == _targetWhiteHDR) return;
                _targetWhiteHDR = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UseCustomWhiteHDR));
            }
            get => _targetWhiteHDR;
        }

        public Visibility UseCustomWhiteHDR => TargetWhiteHDR == 4 ? Visibility.Visible : Visibility.Collapsed;

        public double CustomWhiteHdrX
        {
            set
            {
                if (value == _customWhiteHdrX) return;
                _customWhiteHdrX = value;
                OnPropertyChanged();
            }
            get => _customWhiteHdrX;
        }

        public double CustomWhiteHdrY
        {
            set
            {
                if (value == _customWhiteHdrY) return;
                _customWhiteHdrY = value;
                OnPropertyChanged();
            }
            get => _customWhiteHdrY;
        }

        public bool OverrideMetadataHDR
        {
            set
            {
                if (value == _overrideMetadataHDR) return;
                _overrideMetadataHDR = value;
                OnPropertyChanged();
            }
            get => _overrideMetadataHDR;
        }

        public int PeakLuminanceHDR
        {
            set
            {
                if (value == _peakLuminanceHDR) return;
                _peakLuminanceHDR = value;
                OnPropertyChanged();
            }
            get => _peakLuminanceHDR;
        }

        public int MaxFullFrameLuminanceHDR
        {
            set
            {
                if (value == _maxFullFrameLuminanceHDR) return;
                _maxFullFrameLuminanceHDR = value;
                OnPropertyChanged();
            }
            get => _maxFullFrameLuminanceHDR;
        }

        public double MinLuminanceHDR
        {
            set
            {
                if (value == _minLuminanceHDR) return;
                _minLuminanceHDR = value;
                OnPropertyChanged();
            }
            get => _minLuminanceHDR;
        }

        public Visibility MHC2SupportUnknownWarning =>
            _monitor.IsSupportMHC2 == null
            ? Visibility.Visible : Visibility.Collapsed;

        public Visibility MHC2NotSupportedWarning => 
            _monitor.IsSupportMHC2 == false 
            ? Visibility.Visible : Visibility.Collapsed;

        public Visibility DuplicateDesktopWarning =>
            MHC2NotSupportedWarning != Visibility.Visible &&
            !_monitor.IsUnique 
            ? Visibility.Visible : Visibility.Collapsed;

        public Visibility HdrWarning =>
            MHC2NotSupportedWarning != Visibility.Visible &&
            _monitor.HdrActive 
            ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ProfilePathSDRWarning =>
            MHC2NotSupportedWarning != Visibility.Visible &&
            DuplicateDesktopWarning != Visibility.Visible &&
            UseIcc && ProfilePath.Equals("")
            ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ProfilePathHDRWarning =>
            MHC2NotSupportedWarning != Visibility.Visible &&
            DuplicateDesktopWarning != Visibility.Visible &&
            UseIccHDR && ProfilePathHDR.Equals("")
            ? Visibility.Visible : Visibility.Collapsed;

        public double CustomPercentage
        {
            set
            {
                if (value == _customPercentage) return;
                _customPercentage = value;
                OnPropertyChanged();
            }
            get => _customPercentage;
        }

        public List<string> ChangedProperties { get; } = new List<string>();

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}