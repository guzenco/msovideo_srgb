using System.Collections.Generic;

namespace msovideo_srgb
{
    public abstract class CalibrationApplicator
    {
        protected Display Display { get; }

        protected CalibrationApplicator(Display display)
        {
            Display = display;
        }

        public abstract string Name { get; }

        public abstract bool CanClamp { get; }
        public virtual bool CanClampSDR => false;
        public virtual bool CanClampHDR => false;

        public abstract bool IsCalibrationActiveSDR { get; }
        public virtual bool IsCalibrationActiveHDR => false;

        public virtual bool SupportHDR => false;
        public virtual bool SupportCurveResolution => false;
        public virtual bool SupportMatrixOptimization => false;
        public virtual bool HandleProfile => false;
        public virtual bool ProfileOptional => true;
        public virtual bool ProfileIncludeMHC2 => false;

        public virtual bool SupportDithering => false;
        public virtual bool RequiresDitheringRestore => false;
        public virtual Dithering Dithering { get; set; }

        public abstract List<string> Warnings { get; }

        public virtual void Prepare() { }

        public abstract void UnapplySDR();
        public virtual void UnapplyHDR() { }

        public abstract void ApplySDR(Calibration calibration, ReportSettings profileSettings);
        public virtual void ApplyHDR(Calibration calibration, ReportSettings profileSettings) { }
    }
}