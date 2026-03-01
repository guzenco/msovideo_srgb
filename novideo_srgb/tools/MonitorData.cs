using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using EDIDParser;
using EDIDParser.Descriptors;
using EDIDParser.Enums;
using Microsoft.Win32;
using WindowsDisplayAPI;

namespace novideo_srgb
{
    public class MonitorData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _clamped;

        private MainViewModel _viewModel;

        public MonitorData(MainViewModel viewModel, int number, Display display, string path, bool hdrActive, bool clampSdr)
        {
            _viewModel = viewModel;
            Number = number;

            Edid = GetEDID(path, display);

            Name = Edid.Descriptors.OfType<StringDescriptor>()
                .FirstOrDefault(x => x.Type == StringDescriptorType.MonitorName)?.Value ?? "<no name>";

            Display = display;
            Path = path;
            MHCProfileName = Name + " " + string.Join("#", Path.Split('#').Skip(1).Take(2)) + ".icm";
            ClampSdr = clampSdr;
            HdrActive = hdrActive;

            if (Edid != null)
            {
                var coords = Edid.DisplayParameters.ChromaticityCoordinates;
                EdidColorSpace = new Colorimetry.ColorSpace
                {
                    Red = new Colorimetry.Point { X = Math.Round(coords.RedX, 3), Y = Math.Round(coords.RedY, 3) },
                    Green = new Colorimetry.Point { X = Math.Round(coords.GreenX, 3), Y = Math.Round(coords.GreenY, 3) },
                    Blue = new Colorimetry.Point { X = Math.Round(coords.BlueX, 3), Y = Math.Round(coords.BlueY, 3) },
                    White = Colorimetry.D65
                };
                EdidWhite = new Colorimetry.Point { X = Math.Round(coords.WhiteX, 3), Y = Math.Round(coords.WhiteY, 3) };
                EdidGamma = Edid.DisplayParameters.DisplayGamma;
            }
            else
            {
                EdidColorSpace = Colorimetry.sRGB;
                EdidWhite = Colorimetry.D65;
                EdidGamma = 2.2;
            }

            KeepWhite = true;
            ProfilePath = "";
            CustomGamma = 2.2;
            CustomPercentage = 100;
            Resolution = 1024;
        }

        public static EDID GetEDID(string path, Display display)
        {
            try
            {
                var registryPath = "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Enum\\DISPLAY\\";
                registryPath += string.Join("\\", path.Split('#').Skip(1).Take(2));
                return new EDID((byte[])Registry.GetValue(registryPath + "\\Device Parameters", "EDID", null));
            }
            catch
            {
                return null;
            }
        }
        public MonitorData(MainViewModel viewModel, int number, Display display, string path, bool hdrActive, bool clampSdr, bool useIcc, string profilePath,
            bool calibrateGamma,
            int selectedGamma, double customGamma, double customPercentage, int target, bool keepWhite) :
            this(viewModel, number, display, path, hdrActive, clampSdr)
        {
            UseIcc = useIcc;
            ProfilePath = profilePath;
            CalibrateGamma = calibrateGamma;
            SelectedGamma = selectedGamma;
            CustomGamma = customGamma;
            CustomPercentage = customPercentage;
            Target = target;
            KeepWhite = keepWhite;
        }

        public int Number { get; }
        public string Name { get; }
        public EDID Edid { get; }
        public Display Display { get; }
        public string Path { get; }
        public bool ClampSdr { get; set; }
        public bool HdrActive { get; }
        public string MHCProfileName { get; }

        private void UpdateClamp(bool doClamp)
        {
            if (_clamped)
            {
                //Novideo.DisableColorSpaceConversion(_output);
            }

            if (!doClamp) return;

            if (UseEdid)
                ;
                //Novideo.SetColorSpaceConversion(_output, Colorimetry.RGBToRGB(TargetColorSpace, EdidColorSpace));
            else if (UseIcc)
            {
                var profile = ICCMatrixProfile.FromFile(ProfilePath);
                if (CalibrateGamma)
                {
                    var trcBlack = profile.trcBlack;
                    var tagBlack = profile.tagBlack;
                    
                    ToneCurve gamma;

                    switch (SelectedGamma)
                    {
                        case 0:
                            gamma = new SrgbEOTF(trcBlack);
                            break;
                        case 1:
                            gamma = new GammaToneCurve(2.4, trcBlack, tagBlack, 0);
                            break;
                        case 2:
                            gamma = new GammaToneCurve(CustomGamma, trcBlack, tagBlack, CustomPercentage / 100);
                            break;
                        case 3:
                            gamma = new GammaToneCurve(CustomGamma, trcBlack, tagBlack, CustomPercentage / 100, true);
                            break;
                        case 4:
                            gamma = new LstarEOTF(trcBlack);
                            break;
                        default:
                            throw new NotSupportedException("Unsupported gamma type " + SelectedGamma);
                    }

                    //Novideo.SetColorSpaceConversion(_output, profile, TargetColorSpace, gamma);
                }
                else
                {
                    //Novideo.SetColorSpaceConversion(_output, profile, TargetColorSpace);
                }
            }
        }

        private void HandleClampException(Exception e)
        {
            MessageBox.Show(e.Message);
            //_clamped = Novideo.IsColorSpaceConversionActive(_output);
            ClampSdr = _clamped;
            _viewModel.SaveConfig();
            OnPropertyChanged(nameof(Clamped));
        }
        
        public bool Clamped
        {
            set
            {
                try
                {
                    UpdateClamp(value);
                    ClampSdr = value;
                    _viewModel.SaveConfig();
                }
                catch (Exception e)
                {
                    HandleClampException(e);
                    return;
                }

                _clamped = value;
                OnPropertyChanged();
            }
            get => _clamped;
        }

        public void ReapplyClamp()
        {
            try
            {
                var clamped = CanClamp && ClampSdr;
                UpdateClamp(clamped);
                _clamped = clamped;
                OnPropertyChanged(nameof(CanClamp));
            }
            catch (Exception e)
            {
                HandleClampException(e);
            }
        }

        public bool CanClamp => !HdrActive && (UseEdid && !EdidColorSpace.Equals(TargetColorSpace) || UseIcc && ProfilePath != "");

        public bool UseEdid
        {
            set => UseIcc = !value;
            get => !UseIcc;
        }

        public bool UseIcc { set; get; }

        public string ProfilePath { set; get; }

        public bool CalibrateGamma { set; get; }

        public int SelectedGamma { set; get; }

        public double CustomGamma { set; get; }

        public double CustomPercentage { set; get; }

        public int Target { set; get; }

        public bool KeepWhite { set; get; }

        public uint Resolution { set; get; }

        public Colorimetry.ColorSpace EdidColorSpace { get; }
        public Colorimetry.Point EdidWhite { get; }
        public double EdidGamma { get; }

        private Colorimetry.ColorSpace TargetColorSpace => Colorimetry.ColorSpaces[Target];

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}