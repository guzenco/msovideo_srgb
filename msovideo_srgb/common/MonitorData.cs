using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace msovideo_srgb
{
    public class MonitorData : INotifyPropertyChanged
    {
        public static readonly Func<Display, CalibrationApplicator>[] ApplicatorFactories = new Func<Display, CalibrationApplicator>[]
        {
            MHC2CalibrationApplicator.Init,
            NVAPICalibrationApplicator.Init,
            ADLCalibrationApplicator.Init,
        };

        public event PropertyChangedEventHandler PropertyChanged;

        private bool? _clamped;
        private int _applicator;

        private MainViewModel _viewModel;

        public MonitorData(MainViewModel viewModel, int number, Display display)
        {
            _clamped = false;
            _viewModel = viewModel;

            Number = number;
            Display = display;

            Edid = Display.GetEDID();

            Applicators = ApplicatorFactories.Select(init => init(Display)).Where(a => a != null).ToArray();

            ICCProfileNameSDR = DisplayColorProfileManager.GetManagedProfileName(Display, false);

            Exceptions = new List<Exception>();
        }

        public int Number { get; }
        public EDID Edid { get; }
        public Display Display { get; }
        public CalibrationApplicator[] Applicators { get; }
        public string ICCProfileNameSDR { get; }
        public List<Exception> Exceptions { get; }

        private void UpdateClamp(bool doClamp)
        {
            ActionScheduler.Clear(Path);
            ActionScheduler.SetPriority(Path, -Number);

            if (DitheringApplicator >= 0 && DitheringApplicator < Applicators.Length && Applicators[DitheringApplicator].RequiresDitheringRestore)
            {
                Dithering dithering = new Dithering(DitheringState, DitheringBits, DitheringMode);
                ActionScheduler.Add(Path, () => Applicators[DitheringApplicator].Dithering = dithering, HandleNonCriticalException);
            }

            foreach (var applicator in Applicators)
            {
                if (applicator == ActiveApplicator) continue;

                ActionScheduler.Add(Path, applicator.Prepare, HandleNonCriticalException);
                ActionScheduler.Add(Path, applicator.UnapplySDR, HandleNonCriticalException);
                if (applicator.SupportHDR)
                {
                    ActionScheduler.Add(Path, applicator.UnapplyHDR, HandleNonCriticalException);
                }
            }

            if (ActiveApplicator == null) return;

            ActionScheduler.Add(Path, ActiveApplicator.Prepare, HandleClampException);

            if (!doClamp || !CanClampSDR || !(UseEdid || UseIcc))
            {
                ActionScheduler.Add(Path, ActiveApplicator.UnapplySDR, HandleClampException);
            }
            if (ActiveApplicator.SupportHDR && (!doClamp || !CanClampHDR || !(UseIccHDR || OverrideMetadataHDR)))
            {
                ActionScheduler.Add(Path, ActiveApplicator.UnapplyHDR, HandleClampException);
            }
            if (!ActiveApplicator.HandleProfile)
            {
                ActionScheduler.Add(Path, () => DisplayColorProfileManager.UnapplyProfile(Display, ICCProfileNameSDR, hdr: false, force: false), HandleNonCriticalException);
            }

            if (!doClamp || !CanClamp) return;

            if (CanClampSDR)
            {
                Calibration calibration = null;
                ReportSettings reportSettings = new ReportSettings();

                if (Edid != null)
                {
                    reportSettings.ManufacturerId = Edid.ManufacturerId;
                    reportSettings.ProductCodeId = Edid.ProductCodeId;
                }

                reportSettings.IncludeMHC2 = ActiveApplicator.ProfileIncludeMHC2;
                reportSettings.CurvesResolution = ActiveApplicator.SupportCurveResolution ? CurveResolution : 256;
                reportSettings.ReportWhiteD65 = ReportWhiteD65 || AcmActive;
                reportSettings.ReportColorSpaceSRGB = ReportColorSpaceSRGB && !AcmActive;
                reportSettings.ReportGammaSRGB = ReportGammaSRGB && !AcmActive;

                if (ExcludeHdrMetadata)
                {
                    if (AcmActive)
                    {
                        var colorCapabilities = DisplayColorCapabilities.GetColorCapabilities(Display);
                        if (colorCapabilities != null)
                        {
                            reportSettings.PeakLuminanceOverride = colorCapabilities?.PeakLuminance;
                            reportSettings.MaxFullFrameLuminanceOverride = colorCapabilities?.MaxFullFrameLuminance;
                            reportSettings.MinLuminanceOverride = colorCapabilities?.MinLuminance;
                        }
                    }
                    else
                    {
                        reportSettings.PeakLuminanceOverride = -1;
                        reportSettings.MinLuminanceOverride = -1;
                    }
                }

                if (UseEdid)
                {
                    calibration = new Calibration(Edid, TargetColorSpace, TargetWhitePoint);
                }
                else if (UseIcc)
                {
                    var profile = ICCMatrixProfile.FromFile(ProfilePath);

                    Matrix rgbGains = Matrix.One3x1();
                    if (!TargetWhitePoint.Equals(Colorimetry.NativeWhite))
                    {
                        rgbGains = Colorimetry.RGBGainsForWhite(profile.matrixXYZ, TargetWhitePoint);
                    }

                    double luminance = profile.Luminance(rgbGains);
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

                    if (ActiveApplicator.SupportMatrixOptimization && OptimizeMatrix)
                    {
                        reportSettings.OptimizeMatrix = true;
                        reportSettings.OptimizeMatrixAcmMode = AcmActive;
                    }

                    calibration = new Calibration(profile, TargetColorSpace, TargetWhitePoint, luminance, UseVcgt, gamma);
                }

                if (calibration != null)
                {
                    ActionScheduler.Add(Path, () => ActiveApplicator.ApplySDR(calibration, reportSettings), HandleClampException);

                    if (!ActiveApplicator.HandleProfile && (!ActiveApplicator.ProfileOptional || CreateProfile))
                    {
                        ActionScheduler.Add(Path, () =>
                        {
                            ColorProfileFactory.CreateProfile(ICCProfileNameSDR, calibration, reportSettings);
                            DisplayColorProfileManager.ApplyProfile(Display, ICCProfileNameSDR, hdr: false, force: false);
                        }, HandleNonCriticalException);
                    }
                }
            }

            if (CanClampHDR)
            {
                Calibration calibration = null;
                ReportSettings reportSettings = new ReportSettings();

                if (Edid != null)
                {
                    reportSettings.ManufacturerId = Edid.ManufacturerId;
                    reportSettings.ProductCodeId = Edid.ProductCodeId;
                }

                reportSettings.IncludeMHC2 = true;
                reportSettings.CurvesResolution = ActiveApplicator.SupportCurveResolution ? CurveResolution : 256;
                reportSettings.CurveOverride = new SrgbEOTF();
                reportSettings.PeakLuminanceOverride = OverrideMetadataHDR ? (double?)PeakLuminanceHDR : null;
                reportSettings.MaxFullFrameLuminanceOverride = OverrideMetadataHDR ? (double?)MaxFullFrameLuminanceHDR : null;
                reportSettings.MinLuminanceOverride = OverrideMetadataHDR ? (double?)MinLuminanceHDR : null;

                if (UseIccHDR)
                {
                    var profile = ICCMatrixProfile.FromFile(ProfilePathHDR);

                    Matrix rgbGains = Matrix.One3x1();
                    if (!TargetWhitePointHDR.Equals(Colorimetry.NativeWhite))
                    {
                        rgbGains = Colorimetry.RGBGainsForWhite(profile.matrixXYZ, TargetWhitePointHDR);
                    }

                    double luminance = profile.Luminance(rgbGains);
                    luminance = Math.Min(luminance, TargetPeak);

                    ToneCurve gamma = null;
                    if (CalibrateGammaHDR)
                    {
                        gamma = new ST2084(profile.tagBlack * profile.luminance, luminance, BPCThreshold);
                    }

                    calibration = new Calibration(profile, Colorimetry.Native, TargetWhitePointHDR, luminance, gamma: gamma);
                }
                else if (OverrideMetadataHDR)
                {
                    calibration = new Calibration(Edid, Colorimetry.Native, Colorimetry.NativeWhite);
                }

                if (calibration != null)
                {
                    ActionScheduler.Add(Path, () => ActiveApplicator.ApplyHDR(calibration, reportSettings), HandleClampException);
                }
            }
        }

        public void ReapplyClamp()
        {
            try
            {
                Exceptions.Clear();
                _viewModel.OnExceptionsClear();
                var clamped = CanClamp && Clamp;
                UpdateClamp(clamped);
                _clamped = clamped;
                OnPropertyChanged(nameof(CanClamp));
                OnPropertyChanged(nameof(Clamped));
            }
            catch (Exception e)
            {
                HandleClampException(e);
            }
        }

        private void HandleClampException(Exception e)
        {
            Exceptions.Add(e);

            Application.Current.Dispatcher.Invoke(() =>
            {
                ActionScheduler.Clear(Path);
            });

            _viewModel.OnException();

            try
            {
                _clamped = false;
                if (Clamp || ActiveApplicator?.IsCalibrationActiveSDR == true || ActiveApplicator?.IsCalibrationActiveHDR == true)
                {
                    _clamped = null;
                }
            }
            catch
            {
                _clamped = null;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(Clamped));
            });
        }

        private void HandleNonCriticalException(Exception e)
        {
            Exceptions.Add(e);
            _viewModel.OnNonCriticalException();
        }

        public bool? Clamped
        {
            set
            {
                try
                {
                    Exceptions.Clear();
                    _viewModel.OnExceptionsClear();
                    Clamp = value == true;
                    UpdateClamp(value == true);
                    _clamped = Clamp;
                    OnPropertyChanged(nameof(Clamped));
                }
                catch (Exception e)
                {
                    HandleClampException(e);
                    return;
                }
                finally
                {
                    _viewModel.OnClampChanged(this);
                }
            }
            get => _clamped;
        }

        public CalibrationApplicator ActiveApplicator => Applicator >= 0 && Applicator < Applicators.Length ? Applicators[Applicator] : null;

        public bool CanClamp => ActiveApplicator != null && ActiveApplicator.CanClamp && (CanClampSDR || CanClampHDR);

        public bool CanClampSDR => ActiveApplicator.CanClampSDR && (UseEdid || (UseIcc && ProfilePath != ""));

        public bool CanClampHDR => ActiveApplicator.SupportHDR && ActiveApplicator.CanClampHDR && ((UseIccHDR && ProfilePathHDR != "") || (OverrideMetadataHDR && !UseIccHDR));

        public string Name => Display.HaveFriendlyDeviceName ? Display.FriendlyDeviceName : Display.DeviceID;
        public string Path => Display.DevicePath;

        public bool HdrActive => Display.HdrActive;
        public bool AcmActive => Display.AcmActive;

        public string Mode => HdrActive && AcmActive ? "HDR/ACM" : HdrActive ? "HDR" : AcmActive ? "ACM" : "SDR";

        public bool UseEdid
        {
            set => UseIcc = !value;
            get => !UseIcc;
        }

        [Persistent("applicator", 0)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Applicator))]
        public int Applicator
        {
            get => _applicator;
            set
            {
                _applicator = value;
                OnPropertyChanged(nameof(ActiveApplicator));
            }
        }

        [Persistent("clamp", false)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Clamp))]
        public bool Clamp { get; set; }

        [Persistent("target", 0)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Target))]
        public int Target { set; get; }

        [Persistent("resolution", 2)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Resolution))]
        public int Resolution { set; get; }

        [Persistent("use_icc", false)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.UseIcc))]
        public bool UseIcc { set; get; }

        [Persistent("icc_path", "")]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.ProfilePath))]
        public string ProfilePath { set; get; }

        [Persistent("limit_luminance", false)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.LimitLuminance))]
        public bool LimitLuminance { set; get; }

        [Persistent("max_luminance", 80)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.LimitLuminance))]
        public int MaxLuminance { set; get; }

        [Persistent("calibrate_gamma", false)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Gamma))]
        public bool CalibrateGamma { set; get; }

        [Persistent("selected_gamma", 0)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Gamma))]
        public int SelectedGamma { set; get; }

        [Persistent("custom_gamma", 2.2)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Gamma))]
        public double CustomGamma { set; get; }

        [Persistent("custom_percentage", 100)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Gamma))]
        public double CustomPercentage { set; get; }

        [Persistent("use_vcgt", false)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Gamma))]
        public bool UseVcgt { set; get; }

        [Persistent("optimize_matrix", true)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.OptimizeMatrix))]
        public bool OptimizeMatrix { set; get; }

        [Persistent("target_white", 0)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.TargetWhite))]
        public int TargetWhite { set; get; }

        [Persistent("custom_white_x", 0.3127)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.TargetWhite))]
        public double CustomWhiteX { set; get; }

        [Persistent("custom_white_y", 0.3290)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.TargetWhite))]
        public double CustomWhiteY { set; get; }

        [Persistent("create_profile", true)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Report))]
        public bool CreateProfile { set; get; }

        [Persistent("report_white_d65", false)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Report))]
        public bool ReportWhiteD65 { set; get; }

        [Persistent("report_color_space_srgb", false)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Report))]
        public bool ReportColorSpaceSRGB { set; get; }

        [Persistent("report_gamma_srgb", false)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Report))]
        public bool ReportGammaSRGB { set; get; }

        [Persistent("exclude_hdr_metadata", false)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Report))]
        public bool ExcludeHdrMetadata { set; get; }

        [Persistent("use_icc_hdr", false)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.UseIccHDR))]
        public bool UseIccHDR { set; get; }

        [Persistent("icc_path_hdr", "")]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.ProfilePathHDR))]
        public string ProfilePathHDR { set; get; }

        [Persistent("calibrate_gamma_hdr", false)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.GammaHDR))]
        public bool CalibrateGammaHDR { set; get; }

        [Persistent("target_peak", 10000)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.GammaHDR))]
        public int TargetPeak { set; get; }

        [Persistent("bpc_threshold", 80)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.GammaHDR))]
        public double BPCThreshold { set; get; }

        [Persistent("target_white_hdr", 0)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.TargetWhiteHDR))]
        public int TargetWhiteHDR { set; get; }

        [Persistent("custom_white_hdr_x", 0.3127)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.TargetWhiteHDR))]
        public double CustomWhiteHdrX { set; get; }

        [Persistent("custom_white_hdr_y", 0.3290)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.TargetWhiteHDR))]
        public double CustomWhiteHdrY { set; get; }

        [Persistent("override_metadata_hdr", false)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.OverrideMetadataHDR))]
        public bool OverrideMetadataHDR { set; get; }

        [Persistent("peak_luminance_hdr", 10000)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.OverrideMetadataHDR))]
        public int PeakLuminanceHDR { set; get; }

        [Persistent("max_full_frame_luminance_hdr", 10000)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.OverrideMetadataHDR))]
        public int MaxFullFrameLuminanceHDR { set; get; }

        [Persistent("min_luminance_hdr", 0)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.OverrideMetadataHDR))]
        public double MinLuminanceHDR { set; get; }

        [Persistent("dithering_applicaton", -1)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Dithering))]
        public int DitheringApplicator { set; get; }

        [Persistent("dithering_state", -1)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Dithering))]
        public int DitheringState { set; get; }

        [Persistent("dithering_bits", -1)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Dithering))]
        public int DitheringBits { set; get; }

        [Persistent("dithering_mode", -1)]
        [BindToProperty(typeof(SettingsSourceMap), nameof(SettingsSourceMap.Dithering))]
        public int DitheringMode { set; get; }

        private Colorimetry.ColorSpace TargetColorSpace => !AcmActive ? Colorimetry.ColorSpaces[Target] : Colorimetry.Native;

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