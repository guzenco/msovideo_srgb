using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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

        public MonitorData(MainViewModel viewModel, int number, Display display, string path, bool hdrActive)
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

            Clamp = false;
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

        public int Number { get; }
        public string Name { get; }
        public ExtendedEDID Edid { get; }
        public Display Display { get; }
        public string Path { get; }
        public bool HdrActive { get; }
        public string MHCProfileName { get; }
        public string MHCProfileNameSDR => "[SDR] " + MHCProfileName + ".icm";
        public string MHCProfileNameHDR => "[HDR] " + MHCProfileName + ".icm";
        public string MHCProfileNameDefaultHDR => "[HDR] " + MHCProfileName + " default.icm";

        public const string MHCProfileNameReset = "msovideo_srgb_no_transform.icm";

        public const string MHCProfileNamePattern = @"^\[(?:SDR|HDR)\]\s.+#[^\s]+(?:\.icm| default\.icm)$";
        
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

        private void RemoveWrongProfileAssociations()
        {
            var profiles = ICCProfileGenerator.GetGeneratedProfiles();

            string profileNameSDR = DisplayColorProfileManager.GetProfile(Display, false);
            if (profiles.Contains(profileNameSDR))
            {
                profiles.Remove(profileNameSDR);
            }
            else
            {
                profileNameSDR = "";
            }

            string profileNameHDR = DisplayColorProfileManager.GetProfile(Display, true);
            if (profiles.Contains(profileNameHDR))
            {
                profiles.Remove(profileNameHDR);
            }
            else
            {
                profileNameHDR = "";
            }

            foreach (string profileName in profiles)
            {
                if (!Regex.IsMatch(profileName, MHCProfileNamePattern)) continue;

                DisplayColorProfileManager.RemoveAssociation(Display, profileName, false);
                DisplayColorProfileManager.RemoveAssociation(Display, profileName, true);
            }

            if (Regex.IsMatch(profileNameSDR, MHCProfileNamePattern) && profileNameSDR != MHCProfileNameSDR)
            {
                UnapplyProfile(profileNameSDR, false, true);
            }

            if (Regex.IsMatch(profileNameHDR, MHCProfileNamePattern) && profileNameHDR != MHCProfileNameHDR && profileNameHDR != MHCProfileNameDefaultHDR)
            {
                UnapplyProfile(profileNameHDR, true, true);
            }
        }

        private void UpdateClamp(bool doClamp)
        {
            var scope = DisplayColorProfileManager.GetDisplayUserScope(Display);

            if (scope == DisplayColorProfileManager.WcsProfileManagementScope.SystemWide) {
                DisplayColorProfileManager.SetDisplayUserScope(Display, DisplayColorProfileManager.WcsProfileManagementScope.CurrentUser);
            }

            RemoveWrongProfileAssociations();
            if (_clamped || !doClamp)
            {
                UnapplyProfile(MHCProfileNameSDR, false, !doClamp || !(UseEdid || UseIcc));
                UnapplyProfile(MHCProfileNameHDR, true, !doClamp || !UseIccHDR);
            }
            else
            {
                if (!(UseEdid || UseIcc))
                {
                    UnapplyProfile(MHCProfileNameSDR, false, true);
                }
                if (!UseIccHDR)
                {
                    UnapplyProfile(MHCProfileNameHDR, true, true);
                }
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
                Clamp = _clamped;
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
                    Clamp = value;
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
                var clamped = CanClamp && Clamp;
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

        public bool CanClamp => IsSupportMHC2 != false && IsUnique && ((UseEdid && !EdidColorSpace.Equals(TargetColorSpace)) || (UseIcc && ProfilePath != ""));

        public bool? IsSupportMHC2 => DisplayColorProfileManager.IsSupportMHC2(Display);

        public bool IsUnique => DisplayColorProfileManager.IsDisplaySourceIdUnique(Display);

        public bool UseEdid
        {
            set => UseIcc = !value;
            get => !UseIcc;
        }

        [Persistent("clamp")]
        public bool Clamp { get; set; }

        [Persistent("target")]
        public int Target { set; get; }

        [Persistent("resolution", 2)]
        public int Resolution { set; get; }

        [Persistent("use_icc")]
        public bool UseIcc { set; get; }

        [Persistent("icc_path")]
        public string ProfilePath { set; get; }

        [Persistent("limit_luminance", false)]
        public bool LimitLuminance { set; get; }

        [Persistent("max_luminance", 80)]
        public int MaxLuminance { set; get; }

        [Persistent("calibrate_gamma")]
        public bool CalibrateGamma { set; get; }

        [Persistent("selected_gamma")]
        public int SelectedGamma { set; get; }

        [Persistent("custom_gamma")]
        public double CustomGamma { set; get; }

        [Persistent("custom_percentage")]
        public double CustomPercentage { set; get; }

        [Persistent("use_vcgt", false)]
        public bool UseVcgt { set; get; }

        [Persistent("optimize_matrix", true)]
        public bool OptimizeMatrix { set; get; }

        [Persistent("target_white", 0)]
        public int TargetWhite { set; get; }

        [Persistent("custom_white_x", 0.3127)]
        public double CustomWhiteX { set; get; }

        [Persistent("custom_white_y", 0.3290)]
        public double CustomWhiteY { set; get; }

        [Persistent("report_white_d65", false)]
        public bool ReportWhiteD65 { set; get; }

        [Persistent("report_color_space_srgb", false)]
        public bool ReportColorSpaceSRGB { set; get; }

        [Persistent("report_gamma_srgb", false)]
        public bool ReportGammaSRGB { set; get; }

        [Persistent("use_icc_hdr", false)]
        public bool UseIccHDR { set; get; }

        [Persistent("icc_path_hdr", "")]
        public string ProfilePathHDR { set; get; }

        [Persistent("calibrate_gamma_hdr", false)]
        public bool CalibrateGammaHDR { set; get; }

        [Persistent("target_peak", 10000)]
        public int TargetPeak { set; get; }

        [Persistent("bpc_threshold", 80)]
        public double BPCThreshold { set; get; }

        [Persistent("target_white_hdr", 0)]
        public int TargetWhiteHDR { set; get; }

        [Persistent("custom_white_hdr_x", 0.3127)]
        public double CustomWhiteHdrX { set; get; }

        [Persistent("custom_white_hdr_y", 0.3290)]
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