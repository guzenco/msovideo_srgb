namespace msovideo_srgb
{
    public class ReportSettings
    {
        public ReportSettings(
            uint curvesResolution = 256,
            bool includeMHC2 = false,
            bool includeCalibration = false,
            bool optimizeMatrix = false,
            bool optimizeMatrixAcmMode = false,
            bool reportWhiteD65 = false,
            bool reportColorSpaceSRGB = false,
            bool reportGammaSRGB = false,
            ToneCurve curveOverride = null,
            double? peakLuminanceOverride = null,
            double? maxFullFrameLuminanceOverride = null,
            double? minLuminanceOverride = null)
        {
            CurvesResolution = curvesResolution;
            IncludeMHC2 = includeMHC2;
            IncludeCalibration = includeCalibration;
            OptimizeMatrix = optimizeMatrix;
            OptimizeMatrixAcmMode = optimizeMatrixAcmMode;
            ReportWhiteD65 = reportWhiteD65;
            ReportColorSpaceSRGB = reportColorSpaceSRGB;
            ReportGammaSRGB = reportGammaSRGB;
            CurveOverride = curveOverride;
            PeakLuminanceOverride = peakLuminanceOverride;
            MaxFullFrameLuminanceOverride = maxFullFrameLuminanceOverride;
            MinLuminanceOverride = minLuminanceOverride;
        }

        public uint ManufacturerId { get; set; }
        public uint ProductCodeId { get; set; }
        public uint CurvesResolution { get; set; }
        public bool IncludeMHC2 { get; set; }
        public bool IncludeCalibration { get; set; }
        public bool OptimizeMatrix { get; set; }
        public bool OptimizeMatrixAcmMode { get; set; }
        public bool ReportWhiteD65 { get; set; }
        public bool ReportColorSpaceSRGB { get; set; }
        public bool ReportGammaSRGB { get; set; }
        public ToneCurve CurveOverride { get; set; }
        public double? PeakLuminanceOverride { get; set; }
        public double? MaxFullFrameLuminanceOverride { get; set; }
        public double? MinLuminanceOverride { get; set; }
    }
}
