using System.Collections.Generic;

namespace msovideo_srgb
{
    public class ADLCalibrationApplicator : CalibrationApplicator
    {
        private ADLDisplayColorManager.DisplayId DisplayId { get; }

        private ADLCalibrationApplicator(Display display, ADLDisplayColorManager.DisplayId displayId) : base(display)
        {
            DisplayId = displayId;
        }

        public override string Name => "ADL";

        public override bool CanClamp => !Display.HdrActive && !Display.AcmActive;
        public override bool CanClampSDR => true;

        public override bool IsCalibrationActiveSDR => ADLDisplayColorManager.IsColorSpaceConversionActive(DisplayId);

        public override bool SupportMatrixOptimization => true;

        public override bool SupportDithering => true;
        public override bool RequiresDitheringRestore => true;
        public override Dithering Dithering
        {
            get
            {
                return ADLDisplayColorManager.GetDithering(DisplayId);
            }
            set
            {
                if(value != null)
                {
                    ADLDisplayColorManager.SetDithering(DisplayId, value);
                }
            }
        }

        public override List<string> Warnings
        {
            get
            {
                List<string> warnings = new List<string>();

                if (Display.HdrActive && Display.AcmActive)
                {
                    warnings.Add("HDR or ACM is active - cannot clamp");
                }
                else if (Display.HdrActive)
                {
                    warnings.Add("HDR is active - cannot clamp");
                }
                else if (Display.AcmActive)
                {
                    warnings.Add("ACM is active - cannot clamp");
                }

                return warnings;
            }
        }

        public static CalibrationApplicator Init(Display display)
        {
            if (!ADLDisplayColorManager.Initialized) return null;

            var displayId = ADLDisplayColorManager.GetDisplayId(display);

            if (displayId == null) return null;

            return new ADLCalibrationApplicator(display, displayId);
        }

        public override void UnapplySDR()
        {
            if (!Display.HdrActive && !Display.AcmActive && IsCalibrationActiveSDR)
            {
                ADLDisplayColorManager.ClearColorSpaceConversion(DisplayId);
            }
        }

        public override void ApplySDR(Calibration calibration, ReportSettings reportSettings)
        {
            ADLDisplayColorManager.SetColorspaceConversion(DisplayId, calibration, reportSettings.OptimizeMatrix);
        }
    }
}
