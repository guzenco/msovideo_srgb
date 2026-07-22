using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
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
            MHCProfileName = new string(MHCProfileName.Where(c => !System.IO.Path.GetInvalidFileNameChars().Contains(c)).ToArray());
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
            }
            else
            {
                EdidColorSpace = Colorimetry.sRGB;
            }

            ProfilePath = "";
            MaxLuminance = 80;
            CustomGamma = 2.2;
            CustomPercentage = 100;
            UseVcgt = false;
            OptimizeMatrix = true;
            Resolution = 2;
            ProfilePathHDR = "";
            TargetPeak = 10000;
            BPCThreshold = 80;
            CustomWhiteX = CustomWhiteHdrX = Colorimetry.D65.X;
            CustomWhiteY = CustomWhiteHdrY = Colorimetry.D65.Y;
            ReportWhiteD65 = ReportColorSpaceSRGB = ReportGammaSRGB = false;
        }

        public static ExtendedEDID GetEDID(string path, Display display)
        {
            try
            {
                var registryPath = "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Enum\\DISPLAY\\";
                registryPath += string.Join("\\", path.Split('#').Skip(1).Take(2));
                return new ExtendedEDID((byte[])Registry.GetValue(registryPath + "\\Device Parameters", "EDID", null));
            }
            catch
            {
                return null;
            }
        }
        public MonitorData(MainViewModel viewModel, int number, Display display, string path, bool hdrActive, 
            bool clampSdr, bool useIcc, string profilePath,
            bool limitLuminance, int maxLuminance,
            bool calibrateGamma, int selectedGamma, double customGamma, double customPercentage, bool useVcgt, bool optimizeMatrix, int targetWhite, double customWhiteX, double customWhiteY,
            bool reportWhiteD65, bool reportColorSpaceSRGB, bool reportGammaSRGB,
            int target, int resolution,
            bool useIccHDR, string profilePathHDR, bool calibrateGammaHDR, int peakTarget, double bpcThreshold, int targetWhiteHDR, double customWhiteHdrX, double customWhiteHdrY):
            this(viewModel, number, display, path, hdrActive, clampSdr)
        {
            UseIcc = useIcc;
            ProfilePath = profilePath;
            LimitLuminance = limitLuminance;
            MaxLuminance = maxLuminance;
            CalibrateGamma = calibrateGamma;
            SelectedGamma = selectedGamma;
            CustomGamma = customGamma;
            CustomPercentage = customPercentage;
            UseVcgt = useVcgt;
            OptimizeMatrix = optimizeMatrix;
            TargetWhite = targetWhite;
            CustomWhiteX = customWhiteX;
            CustomWhiteY = customWhiteY;
            ReportWhiteD65 = reportWhiteD65;
            ReportColorSpaceSRGB = reportColorSpaceSRGB;
            ReportGammaSRGB = reportGammaSRGB;
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
        public ExtendedEDID Edid { get; }
        public Display Display { get; }
        public string Path { get; }
        public bool ClampSdr { get; set; }
        public bool HdrActive { get; }
        public string MHCProfileName { get; }
        public string MHCProfileNameSDR => "[SDR] " + MHCProfileName + ".icm";
        public string MHCProfileNameHDR => "[HDR] " + MHCProfileName + ".icm";
        public string MHCProfileNameDefaultHDR => "[HDR] " + MHCProfileName + " default.icm";

        public const string MHCProfileNameReset = "msovideo_srgb_no_transform.icm";

        private void ApplyProfile(string profileName, bool hdr)
        {
            ColorProfileFactory.CreateProfile(MHCProfileNameReset, CurveResolution);

            DisplayColorProfileManager.AddAssociation(Display, MHCProfileNameReset, hdr);
            DisplayColorProfileManager.SetProfile(Display, MHCProfileNameReset, hdr);

            DisplayColorProfileManager.AddAssociation(Display, profileName, hdr);
            DisplayColorProfileManager.SetProfile(Display, profileName, hdr);

            DisplayColorProfileManager.RemoveAssociation(Display, MHCProfileNameReset, hdr);

            if (!UseIccHDR && DisplayColorProfileManager.GetProfile(Display, true).Equals("") && Edid != null && Edid.ExtensionCTA861 != null)
            {
                ColorProfileFactory.CreateProfile(MHCProfileNameDefaultHDR, CurveResolution, Edid);
                DisplayColorProfileManager.AddAssociation(Display, MHCProfileNameDefaultHDR, true);
                DisplayColorProfileManager.SetProfile(Display, MHCProfileNameDefaultHDR, true);
            }
        }

        private void UnapplyProfile(string profileName, bool hdr, bool force)
        {
            if (DisplayColorProfileManager.GetProfile(Display, hdr).Equals(profileName))
            {
                if (force)
                {
                    ColorProfileFactory.CreateProfile(MHCProfileNameReset, CurveResolution);

                    DisplayColorProfileManager.AddAssociation(Display, MHCProfileNameReset, hdr);
                    DisplayColorProfileManager.SetProfile(Display, MHCProfileNameReset, hdr);

                    DisplayColorProfileManager.RemoveAssociation(Display, profileName, hdr);

                    DisplayColorProfileManager.RemoveAssociation(Display, MHCProfileNameReset, hdr);
                }
                else
                {
                    DisplayColorProfileManager.RemoveAssociation(Display, profileName, hdr);
                }

                if (Edid != null && Edid.ExtensionCTA861 != null)
                {
                    ColorProfileFactory.CreateProfile(MHCProfileNameDefaultHDR, CurveResolution, Edid);
                    DisplayColorProfileManager.AddAssociation(Display, MHCProfileNameDefaultHDR, hdr);
                    DisplayColorProfileManager.SetProfile(Display, MHCProfileNameDefaultHDR, hdr);
                    DisplayColorProfileManager.RemoveAssociation(Display, MHCProfileNameDefaultHDR, hdr);
                    if (!hdr && DisplayColorProfileManager.GetProfile(Display, true).Equals(MHCProfileNameDefaultHDR))
                    {
                        DisplayColorProfileManager.RemoveAssociation(Display, MHCProfileNameDefaultHDR, true);
                    }
                }
                
            }
        }

        private void UpdateClamp(bool doClamp)
        {
            var scope = DisplayColorProfileManager.GetDisplayUserScope(Display);

            if (scope == DisplayColorProfileManager.WcsProfileManagementScope.SystemWide) {
                DisplayColorProfileManager.SetDisplayUserScope(Display, DisplayColorProfileManager.WcsProfileManagementScope.CurrentUser);
            }

            if (_clamped)
            {
                UnapplyProfile(MHCProfileNameSDR, false, !doClamp);
                UnapplyProfile(MHCProfileNameHDR, true, !doClamp);
            }

            if (!doClamp) return;

            if (UseEdid)
                ColorProfileFactory.CreateProfile(MHCProfileNameSDR, CurveResolution, Edid, TargetColorSpace, TargetWhitePoint,
                    reportWhiteD65: ReportWhiteD65 || HdrActive,
                    reportColorSpaceSRGB: ReportColorSpaceSRGB && !HdrActive,
                    reportGammaSRGB: ReportGammaSRGB && !HdrActive);
            else if (UseIcc)
            {
                var profile = ICCMatrixProfile.FromFile(ProfilePath);

                Matrix matrixWhite = Matrix.Identity();
                if (!TargetWhitePoint.Equals(Colorimetry.NativeWhite))
                {
                    matrixWhite = Colorimetry.CreateWhiteMatrix(profile.matrix, profile.whitePoint, TargetWhitePoint);
                }

                double luminance = profile.Luminance(matrixWhite);
                if (LimitLuminance)
                {
                    luminance = Math.Min(luminance, MaxLuminance);
                }

                ToneCurve gamma = null;
                if (CalibrateGamma)
                {
                    var tagBlack = profile.tagBlack;
                    
                    tagBlack *= profile.luminance / luminance;

                    switch (SelectedGamma)
                    {
                        case 0:
                            gamma = new SrgbEOTF();
                            break;
                        case 1:
                            gamma = new GammaToneCurve(2.4, tagBlack, 0);
                            break;
                        case 2:
                            gamma = new GammaToneCurve(CustomGamma, tagBlack, CustomPercentage / 100);
                            break;
                        case 3:
                            gamma = new GammaToneCurve(CustomGamma, tagBlack, CustomPercentage / 100, true);
                            break;
                        case 4:
                            gamma = new LstarEOTF();
                            break;
                        default:
                            throw new NotSupportedException("Unsupported gamma type " + SelectedGamma);
                    }
                }

                ColorProfileFactory.CreateProfile(MHCProfileNameSDR, CurveResolution, Edid, profile, TargetColorSpace, TargetWhitePoint, luminance,
                        reportWhiteD65: ReportWhiteD65 || HdrActive,
                        reportColorSpaceSRGB: ReportColorSpaceSRGB && !HdrActive,
                        reportGammaSRGB: ReportGammaSRGB && !HdrActive,
                        useVcgt: UseVcgt,
                        optimizeMatrix: OptimizeMatrix,
                        acmMode: HdrActive,
                        gamma: gamma);
            }

            ApplyProfile(MHCProfileNameSDR, false);

            if(UseIccHDR)
            {
                var profile = ICCMatrixProfile.FromFile(ProfilePathHDR);

                
                Matrix matrixWhite = Matrix.Identity();
                if (!TargetWhitePointHDR.Equals(Colorimetry.NativeWhite))
                {
                    matrixWhite = Colorimetry.CreateWhiteMatrix(profile.matrix, profile.whitePoint, TargetWhitePointHDR);
                }

                double luminance = profile.Luminance(matrixWhite);

                ToneCurve gamma = null;
                if (CalibrateGammaHDR)
                {
                    gamma = new ST2084(TargetPeak, profile.trcBlack * profile.luminance, luminance, BPCThreshold);
                    luminance = profile.Luminance(matrixWhite, gamma);
                }

                ColorProfileFactory.CreateProfile(MHCProfileNameHDR, CurveResolution, Edid, profile, TargetColorSpace, TargetWhitePointHDR, luminance,
                        gamma: gamma,
                        curve: new SrgbEOTF());

                ApplyProfile(MHCProfileNameHDR, true);
            }
        }

        private void HandleClampException(Exception e)
        {
            try
            {
                if (e is DisplayNotFoundException) return;
                MessageBox.Show(e.Message);
                _clamped = DisplayColorProfileManager.GetProfile(Display, false).Equals(MHCProfileNameSDR) && (!UseIccHDR || DisplayColorProfileManager.GetProfile(Display, true).Equals(MHCProfileNameHDR));
                ClampSdr = _clamped;
                OnPropertyChanged(nameof(Clamped));
            }
            catch { }
            finally
            {
                _viewModel.SaveConfig();
            }
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

        public bool CanClamp => IsUnique && ((UseEdid && !EdidColorSpace.Equals(TargetColorSpace)) || (UseIcc && ProfilePath != ""));

        public bool IsUnique => DisplayColorProfileManager.IsDisplaySourceIdUnique(Path);

        public bool UseEdid
        {
            set => UseIcc = !value;
            get => !UseIcc;
        }

        public bool UseIcc { set; get; }

        public string ProfilePath { set; get; }

        public bool LimitLuminance { set; get; }

        public int MaxLuminance { set; get; }

        public bool CalibrateGamma { set; get; }

        public int SelectedGamma { set; get; }

        public double CustomGamma { set; get; }

        public double CustomPercentage { set; get; }

        public bool UseVcgt { set; get; }

        public bool OptimizeMatrix { set; get; }

        public int TargetWhite { set; get; }

        public double CustomWhiteX { set; get; }

        public double CustomWhiteY { set; get; }

        public bool ReportWhiteD65 { set; get; }

        public bool ReportColorSpaceSRGB { set; get; }

        public bool ReportGammaSRGB { set; get; }

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