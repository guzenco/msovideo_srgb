using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using EDIDParser;

namespace msovideo_srgb
{
    public class AdvancedViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private MonitorData _monitor;

        private int _target;
        private int _resolution;
        private bool _useIcc;
        private string _profilePath;
        private bool _calibrateGamma;
        private int _selectedGamma;
        private double _customGamma;
        private double _customPercentage;
        private int _targetWhite;
        private double _customWhiteX;
        private double _customWhiteY;
        private bool _reportWhiteD65;
        private bool _reportColorSpaceSRGB;
        private bool _reportGammaSRGB;
        private bool _useIccHDR;
        private string _profilePathHDR;
        private bool _calibrateGammaHDR;
        private int _targetPeak;
        private double _bpcThreshold;
        private int _targetWhiteHDR;
        private double _customWhiteHdrX;
        private double _customWhiteHdrY;

        public AdvancedViewModel()
        {
            throw new NotSupportedException();
        }

        public AdvancedViewModel(MonitorData monitor)
        {
            _monitor = monitor;

            _target = monitor.Target;
            _resolution = monitor.Resolution;
            _useIcc = monitor.UseIcc;
            _profilePath = monitor.ProfilePath;
            _calibrateGamma = monitor.CalibrateGamma;
            _selectedGamma = monitor.SelectedGamma;
            _customGamma = monitor.CustomGamma;
            _customPercentage = monitor.CustomPercentage;
            _targetWhite = monitor.TargetWhite;
            _customWhiteX = monitor.CustomWhiteX;
            _customWhiteY = monitor.CustomWhiteY;
            _reportWhiteD65 = monitor.ReportWhiteD65;
            _reportColorSpaceSRGB = monitor.ReportColorSpaceSRGB;
            _reportGammaSRGB = monitor.ReportGammaSRGB;
            _useIccHDR = monitor.UseIccHDR;
            _profilePathHDR = monitor.ProfilePathHDR;
            _calibrateGammaHDR = monitor.CalibrateGammaHDR;
            _targetPeak = monitor.TargetPeak;
            _bpcThreshold = monitor.BPCThreshold;
            _targetWhiteHDR = monitor.TargetWhiteHDR;
            _customWhiteHdrX = monitor.CustomWhiteHdrX;
            _customWhiteHdrY = monitor.CustomWhiteHdrY;
        }

        public void ApplyChanges()
        {
            ChangedCalibration |= _monitor.Target != _target;
            _monitor.Target = _target;
            ChangedCalibration |= _monitor.Resolution != _resolution;
            _monitor.Resolution = _resolution;
            ChangedCalibration |= _monitor.UseIcc != _useIcc;
            _monitor.UseIcc = _useIcc;
            ChangedCalibration |= _monitor.ProfilePath != _profilePath;
            _monitor.ProfilePath = _profilePath;
            ChangedCalibration |= _monitor.CalibrateGamma != _calibrateGamma;
            _monitor.CalibrateGamma = _calibrateGamma;
            ChangedCalibration |= _monitor.SelectedGamma != _selectedGamma;
            _monitor.SelectedGamma = _selectedGamma;
            ChangedCalibration |= _monitor.CustomGamma != _customGamma;
            _monitor.CustomGamma = _customGamma;
            ChangedCalibration |= _monitor.CustomPercentage != _customPercentage;
            _monitor.CustomPercentage = _customPercentage;
            ChangedCalibration |= _monitor.TargetWhite != _targetWhite;
            _monitor.TargetWhite = TargetWhite;
            ChangedCalibration |= _monitor.CustomWhiteX != _customWhiteX;
            _monitor.CustomWhiteX = CustomWhiteX;
            ChangedCalibration |= _monitor.CustomWhiteY != _customWhiteY;
            _monitor.CustomWhiteY = CustomWhiteY;
            ChangedCalibration |= _monitor.ReportWhiteD65 != _reportWhiteD65;
            _monitor.ReportWhiteD65 = ReportWhiteD65;
            ChangedCalibration |= _monitor.ReportColorSpaceSRGB != _reportColorSpaceSRGB;
            _monitor.ReportColorSpaceSRGB = ReportColorSpaceSRGB;
            ChangedCalibration |= _monitor.ReportGammaSRGB != _reportGammaSRGB;
            _monitor.ReportGammaSRGB = ReportGammaSRGB;
            ChangedCalibration |= _monitor.UseIccHDR != _useIccHDR;
            _monitor.UseIccHDR = _useIccHDR;
            ChangedCalibration |= _monitor.ProfilePathHDR != _profilePathHDR;
            _monitor.ProfilePathHDR = _profilePathHDR;
            ChangedCalibration |= _monitor.CalibrateGammaHDR != _calibrateGammaHDR;
            _monitor.CalibrateGammaHDR = _calibrateGammaHDR;
            ChangedCalibration |= _monitor.TargetPeak != _targetPeak;
            _monitor.TargetPeak = TargetPeak;
            ChangedCalibration |= _monitor.BPCThreshold != _bpcThreshold;
            _monitor.BPCThreshold = BPCThreshold;
            ChangedCalibration |= _monitor.TargetWhiteHDR != _targetWhiteHDR;
            _monitor.TargetWhiteHDR = TargetWhiteHDR;
            ChangedCalibration |= _monitor.CustomWhiteHdrX != _customWhiteHdrX;
            _monitor.CustomWhiteHdrX = CustomWhiteHdrX;
            ChangedCalibration |= _monitor.CustomWhiteHdrY != _customWhiteHdrY;
            _monitor.CustomWhiteHdrY = _customWhiteHdrY;
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
                OnPropertyChanged(nameof(EdidWarning));
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
                OnPropertyChanged(nameof(EdidWarning));
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
            }
            get => _profilePath;
        }

        public string ProfileName => Path.GetFileName(ProfilePath);

        public bool CalibrateGamma
        {
            set
            {
                if (value == _calibrateGamma) return;
                _calibrateGamma = value;
                OnPropertyChanged();
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

        public int Target
        {
            set
            {
                if (value == _target) return;
                _target = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EdidWarning));
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

        public Visibility HdrWarning => _monitor.HdrActive ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EdidWarning => HdrWarning != Visibility.Visible && UseEdid && Colorimetry.ColorSpaces[_target].Equals(_monitor.EdidColorSpace)
            ? Visibility.Visible
            : Visibility.Collapsed;

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

        public bool ChangedCalibration { get; set; }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}