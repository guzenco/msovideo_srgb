using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using EDIDParser;

namespace novideo_srgb
{
    public class AdvancedViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private MonitorData _monitor;

        private int _target;
        private bool _keepWhite;
        private bool _useIcc;
        private string _profilePath;
        private bool _calibrateGamma;
        private int _selectedGamma;
        private double _customGamma;
        private double _customPercentage;

        public AdvancedViewModel()
        {
            throw new NotSupportedException();
        }

        public AdvancedViewModel(MonitorData monitor)
        {
            _monitor = monitor;

            _target = monitor.Target;
            _keepWhite = monitor.KeepWhite;
            _useIcc = monitor.UseIcc;
            _profilePath = monitor.ProfilePath;
            _calibrateGamma = monitor.CalibrateGamma;
            _selectedGamma = monitor.SelectedGamma;
            _customGamma = monitor.CustomGamma;
            _customPercentage = monitor.CustomPercentage;
        }

        public void ApplyChanges()
        {
            ChangedCalibration |= _monitor.Target != _target;
            _monitor.Target = _target;
            ChangedCalibration |= _monitor.KeepWhite != _keepWhite;
            _monitor.KeepWhite = _keepWhite;
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

        public bool KeepWhite
        {
            set
            {
                if (value == _keepWhite) return;
                _keepWhite = value;
                OnPropertyChanged();
            }
            get => _keepWhite;
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