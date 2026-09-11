using System.Collections.Generic;

namespace msovideo_srgb
{
    public class NVAPICalibrationApplicator : CalibrationApplicator
    {
        private uint DisplayId { get; }

        private NVAPICalibrationApplicator(Display display, uint displayId) : base(display)
        {
            DisplayId = displayId;
        }

        public override string Name => "NVAPI";

        public override bool CanClamp => !Display.HdrActive && !Display.AcmActive;
        public override bool CanClampSDR => true;

        public override bool IsCalibrationActiveSDR => NVDisplayColorManager.IsColorSpaceConversionActive(DisplayId);

        public override bool SupportDithering => true;
        public override Dithering Dithering
        {
            get
            {
                return NVDisplayColorManager.GetDithering(DisplayId);
            }
            set
            {
                if(value != null)
                {
                    NVDisplayColorManager.SetDithering(DisplayId, value);
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
            if (!NVDisplayColorManager.Initialized) return null;

            uint? displayId = NVDisplayColorManager.GetDisplayId(display.TargetAdapterId, display.TargetId);

            if (displayId == null) return null;

            return new NVAPICalibrationApplicator(display, displayId.Value);
        }

        public override void UnapplySDR()
        {
            if (IsCalibrationActiveSDR)
            {
                if (!Display.HdrActive || (Display.HdrActive && Display.AcmActive))
                {
                    NVDisplayColorManager.ClearColorSpaceConversion(DisplayId, matrixOnly: Display.AcmActive);
                }
                else
                {
                    throw new ExternalAPIException("Clamp cannot be disabled while HDR is active. Colors will display incorrectly. Disable clamp before switching to HDR.");
                }
            }
        }

        public override void ApplySDR(Calibration calibration, ReportSettings reportSettings)
        {
            NVDisplayColorManager.SetColorspaceConversion(DisplayId, calibration);
            DXGISwapChainManager.DisableFlipOptimizationForMoment(Display);
        }
    }
}
