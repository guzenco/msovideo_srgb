using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using EDIDParser;
using EDIDParser.Descriptors;
using EDIDParser.Enums;
using Microsoft.Win32;
using WindowsDisplayAPI;

namespace msovideo_srgb
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
            MHCProfileName = Name + " " + string.Join("#", Path.Split('#').Skip(1).Take(2));
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

            ProfilePath = "";
            CustomGamma = 2.2;
            CustomPercentage = 100;
            Resolution = 2;
            ProfilePathHDR = "";
            TargetPeak = 10000;
            BPCThreshold = 80;
            CustomWhiteX = CustomWhiteHdrX = Colorimetry.D65.X;
            CustomWhiteY = CustomWhiteHdrY = Colorimetry.D65.Y;
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
        public MonitorData(MainViewModel viewModel, int number, Display display, string path, bool hdrActive, 
            bool clampSdr, bool useIcc, string profilePath, bool calibrateGamma, int selectedGamma, double customGamma, double customPercentage, int targetWhite, double customWhiteX, double customWhiteY,
            int target, int resolution,
            bool useIccHDR, string profilePathHDR, bool calibrateGammaHDR, int peakTarget, double bpcThreshold, int targetWhiteHDR, double customWhiteHdrX, double customWhiteHdrY):
            this(viewModel, number, display, path, hdrActive, clampSdr)
        {
            UseIcc = useIcc;
            ProfilePath = profilePath;
            CalibrateGamma = calibrateGamma;
            SelectedGamma = selectedGamma;
            CustomGamma = customGamma;
            CustomPercentage = customPercentage;
            TargetWhite = targetWhite;
            CustomWhiteX = customWhiteX;
            CustomWhiteY = customWhiteY;
            Target = target;
            Resolution = resolution;
            UseIccHDR = useIccHDR;
            ProfilePathHDR = profilePathHDR;
            CalibrateGammaHDR = calibrateGammaHDR;
            TargetPeak = peakTarget;
            BPCThreshold = bpcThreshold;
            TargetWhiteHDR = targetWhiteHDR;
            CustomWhiteHdrX = customWhiteHdrX;
            CustomWhiteHdrY = customWhiteHdrY;
        }

        public int Number { get; }
        public string Name { get; }
        public EDID Edid { get; }
        public Display Display { get; }
        public string Path { get; }
        public bool ClampSdr { get; set; }
        public bool HdrActive { get; }
        public string MHCProfileName { get; }
        public string MHCProfileNameSDR => "[SDR] " + MHCProfileName + ".icm";
        public string MHCProfileNameHDR => "[HDR] " + MHCProfileName + ".icm";

        private void UpdateClamp(bool doClamp)
        {
            var scope = DisplayColorProfileManager.GetDisplayUserScope(Display);

            if (scope == DisplayColorProfileManager.WcsProfileManagementScope.SystemWide) {
                DisplayColorProfileManager.SetDisplayUserScope(Display, DisplayColorProfileManager.WcsProfileManagementScope.CurrentUser);
            }

            if (_clamped)
            {
                if (DisplayColorProfileManager.GetProfile(Display, false).Equals(MHCProfileNameSDR))
                {
                    DisplayColorProfileManager.RemoveAssociation(Display, MHCProfileNameSDR, false);
                }
                if (DisplayColorProfileManager.GetProfile(Display, true).Equals(MHCProfileNameHDR))
                {
                    DisplayColorProfileManager.RemoveAssociation(Display, MHCProfileNameHDR, true);
                }
            }

            if (!doClamp) return;

            Thread.Sleep(100);

            bool reportD65 = HdrActive;
            bool iccV4 = HdrActive;

            if (UseEdid)
                ColorProfileFactory.CreateProfile(MHCProfileNameSDR, CurveResolution, EdidColorSpace, TargetColorSpace, EdidWhite, TargetWhitePoint, reportD65, EdidGamma);
            else if (UseIcc)
            {
                var profile = ICCMatrixProfile.FromFile(ProfilePath);

                double luminance = profile.luminance;
                if (!TargetWhitePoint.Equals(Colorimetry.NativeWhite))
                {
                    var matrixWhite = Matrix.FromDiagonal(Colorimetry.XYZScale(profile.matrix * Colorimetry.WhiteToWhiteAdaptation(Colorimetry.D50, profile.whitePoint), profile.whitePoint).Inverse() * Colorimetry.RGBToXYZ(TargetWhitePoint));
                    double scale = Math.Max(Math.Max(matrixWhite[0, 0], matrixWhite[1, 1]), matrixWhite[2, 2]);
                    Matrix newWhiteLumi = Matrix.FromValues(new[,]
                    {
                        { matrixWhite[0,0] / scale },
                        { matrixWhite[1,1] / scale },
                        { matrixWhite[2,2] / scale }
                    });
                    luminance *= (profile.matrix * newWhiteLumi)[1];
                }

                if (CalibrateGamma)
                {
                    var trcBlack = profile.trcBlack;
                    var tagBlack = profile.tagBlack;
                    
                    ToneCurve curve;
                    ToneCurve gamma;

                    switch (SelectedGamma)
                    {
                        case 0:
                            curve = new SrgbEOTF(0);
                            gamma = new SrgbEOTF(trcBlack);
                            break;
                        case 1:
                            curve = new GammaToneCurve(2.4, 0, tagBlack, 0);
                            gamma = new GammaToneCurve(2.4, trcBlack, tagBlack, 0);
                            break;
                        case 2:
                            curve = new GammaToneCurve(CustomGamma, 0, tagBlack, CustomPercentage / 100);
                            gamma = new GammaToneCurve(CustomGamma, trcBlack, tagBlack, CustomPercentage / 100);
                            break;
                        case 3:
                            curve = new GammaToneCurve(CustomGamma, 0, tagBlack, CustomPercentage / 100, true);
                            gamma = new GammaToneCurve(CustomGamma, trcBlack, tagBlack, CustomPercentage / 100, true);
                            break;
                        case 4:
                            curve = new LstarEOTF(0);
                            gamma = new LstarEOTF(trcBlack);
                            break;
                        default:
                            throw new NotSupportedException("Unsupported gamma type " + SelectedGamma);
                    }

                    ColorProfileFactory.CreateProfile(MHCProfileNameSDR, CurveResolution, iccV4, profile, TargetColorSpace, TargetWhitePoint, reportD65, luminance, curve, gamma);
                }
                else
                {
                    ColorProfileFactory.CreateProfile(MHCProfileNameSDR, CurveResolution, iccV4, profile, TargetColorSpace, TargetWhitePoint, reportD65, luminance, new GammaToneCurve(EdidGamma));
                }
            }
            DisplayColorProfileManager.AddAssociation(Display, MHCProfileNameSDR, false);
            DisplayColorProfileManager.SetProfile(Display, MHCProfileNameSDR, false);

            if(UseIccHDR)
            {
                var profile = ICCMatrixProfile.FromFile(ProfilePathHDR);

                double luminance = profile.luminance;
                if (!TargetWhitePointHDR.Equals(Colorimetry.NativeWhite))
                {
                    var matrixWhite = Matrix.FromDiagonal(Colorimetry.XYZScale(profile.matrix * Colorimetry.WhiteToWhiteAdaptation(Colorimetry.D50, profile.whitePoint), profile.whitePoint).Inverse() * Colorimetry.RGBToXYZ(TargetWhitePointHDR));
                    double scale = Math.Max(Math.Max(matrixWhite[0, 0], matrixWhite[1, 1]), matrixWhite[2, 2]);
                    Matrix newWhiteLumi = Matrix.FromValues(new[,]
                    {
                        { matrixWhite[0,0] / scale },
                        { matrixWhite[1,1] / scale },
                        { matrixWhite[2,2] / scale }
                    });
                    luminance *= (profile.matrix * newWhiteLumi)[1];
                }

                if (CalibrateGammaHDR)
                {
                    
                    var gamma = new ST2084(TargetPeak, profile.trcBlack * profile.luminance, luminance, BPCThreshold);

                    Matrix newTrcLumi = Matrix.FromValues(new[,]
                    {
                        { gamma.SampleAt(1) },
                        { gamma.SampleAt(1) },
                        { gamma.SampleAt(1) }
                    });

                    luminance *= (profile.matrix * newTrcLumi)[1];

                    ColorProfileFactory.CreateProfile(MHCProfileNameHDR, CurveResolution, true, profile, TargetColorSpace, TargetWhitePointHDR, false, luminance, new SrgbEOTF(0), gamma);
                }
                else
                {
                    ColorProfileFactory.CreateProfile(MHCProfileNameHDR, CurveResolution, true, profile, TargetColorSpace, TargetWhitePointHDR, false, luminance, new SrgbEOTF(0));
                }

                DisplayColorProfileManager.AddAssociation(Display, MHCProfileNameHDR, true);
                DisplayColorProfileManager.SetProfile(Display, MHCProfileNameHDR, true);
            }
        }

        private void HandleClampException(Exception e)
        {
            MessageBox.Show(e.Message);
            _clamped = DisplayColorProfileManager.GetProfile(Display, false).Equals(MHCProfileNameSDR) && (!UseIccHDR || DisplayColorProfileManager.GetProfile(Display, true).Equals(MHCProfileNameHDR));
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

        public string Mode => HdrActive ? "HDR/ACM " : "SDR";

        public bool CanClamp => (UseEdid && !EdidColorSpace.Equals(TargetColorSpace) || UseIcc && ProfilePath != "");

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

        public int TargetWhite { set; get; }

        public double CustomWhiteX { set; get; }

        public double CustomWhiteY { set; get; }

        public int Target { set; get; }

        public int Resolution { set; get; }

        public bool UseIccHDR { set; get; }

        public string ProfilePathHDR { set; get; }

        public bool CalibrateGammaHDR { set; get; }

        public int TargetPeak { set; get; }

        public double BPCThreshold { set; get; }

        public int TargetWhiteHDR { set; get; }

        public double CustomWhiteHdrX { set; get; }

        public double CustomWhiteHdrY { set; get; }

        public Colorimetry.ColorSpace EdidColorSpace { get; }
        public Colorimetry.Point EdidWhite { get; }
        public double EdidGamma { get; }

        private Colorimetry.ColorSpace TargetColorSpace => !HdrActive ? Colorimetry.ColorSpaces[Target]: Colorimetry.Native;

        private uint[] Resolutions = new uint[] { 256, 1024, 4096 };
        private uint CurveResolution => Resolutions[Resolution];

        private Colorimetry.Point[] TargerWhites = new Colorimetry.Point[] { Colorimetry.NativeWhite, Colorimetry.D50_xy, Colorimetry.D65, Colorimetry.D93 };
        private Colorimetry.Point TargetWhitePoint => TargetWhite < TargerWhites.Length ? TargerWhites[TargetWhite] : new Colorimetry.Point { X = CustomWhiteX, Y = CustomWhiteY };
        private Colorimetry.Point TargetWhitePointHDR => TargetWhiteHDR < TargerWhites.Length ? TargerWhites[TargetWhiteHDR] : new Colorimetry.Point { X = CustomWhiteHdrX, Y = CustomWhiteHdrY };

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}