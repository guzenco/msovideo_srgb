using System.Collections.Generic;

namespace msovideo_srgb
{
    public class MHC2CalibrationApplicator : CalibrationApplicator
    {
        private bool? IsSupportMHC2 { get; }
        private string ICCProfileNameSDR { get; }
        private string ICCProfileNameHDR { get; }

        private MHC2CalibrationApplicator(Display display) : base(display)
        {
            IsSupportMHC2 = DisplayColorProfileManager.IsSupportMHC2(Display);
            ICCProfileNameSDR = DisplayColorProfileManager.GetManagedProfileName(Display, false);
            ICCProfileNameHDR = DisplayColorProfileManager.GetManagedProfileName(Display, true);
        }

        public override string Name => "MHC2";

        public override bool CanClamp => IsSupportMHC2 != false && Display.IsSourceUnique;
        public override bool CanClampSDR => true;
        public override bool CanClampHDR => true;

        public override bool IsCalibrationActiveSDR => DisplayColorProfileManager.IsManagedProfileActive(Display, hdr: false);
        public override bool IsCalibrationActiveHDR => DisplayColorProfileManager.IsManagedProfileActive(Display, hdr: true);

        public override bool SupportHDR => true;
        public override bool SupportCurveResolution => true;
        public override bool SupportMatrixOptimization => true;
        public override bool HandleProfile => true;
        public override bool ProfileOptional => false;
        public override bool ProfileIncludeMHC2 => true;

        public override List<string> Warnings
        {
            get
            {
                List<string> warnings = new List<string>();

                if (IsSupportMHC2 == false)
                {
                    warnings.Add("MHC2 not supported");
                }
                else
                {
                    if (IsSupportMHC2 == null)
                    {
                        warnings.Add("MHC2 support unknown");
                    }
                    if (!Display.IsSourceUnique)
                    {
                        warnings.Add("Duplicate desktop mode – cannot clamp");
                    }
                    if (Display.AcmActive)
                    {
                        warnings.Add("ACM is active - treat target as Native and ignore ICC profile report settings");
                    }
                }
                return warnings;
            }
        }

        public static CalibrationApplicator Init(Display display)
        {
            return new MHC2CalibrationApplicator(display);
        }

        public override void Prepare()
        {
            var scope = DisplayColorProfileManager.GetDisplayUserScope(Display);

            if (scope == DisplayColorProfileManager.WcsProfileManagementScope.SystemWide)
            {
                DisplayColorProfileManager.SetDisplayUserScope(Display, DisplayColorProfileManager.WcsProfileManagementScope.CurrentUser);
            }
        }

        public override void UnapplySDR()
        {
            DisplayColorProfileManager.UnapplyProfile(Display, ICCProfileNameSDR, hdr: false);
        }

        public override void UnapplyHDR()
        {
            DisplayColorProfileManager.UnapplyProfile(Display, ICCProfileNameHDR, hdr: true);
        }

        public override void ApplySDR(Calibration calibration, ReportSettings reportSettings)
        {
            reportSettings.IncludeCalibration = true;
            ColorProfileFactory.CreateProfile(ICCProfileNameSDR, calibration, reportSettings);
            DisplayColorProfileManager.ApplyProfile(Display, ICCProfileNameSDR, hdr: false);
        }

        public override void ApplyHDR(Calibration calibration, ReportSettings reportSettings)
        {
            reportSettings.IncludeCalibration = true;
            ColorProfileFactory.CreateProfile(ICCProfileNameHDR, calibration, reportSettings);
            DisplayColorProfileManager.ApplyProfile(Display, ICCProfileNameHDR, hdr: true);
        }
    }
}
