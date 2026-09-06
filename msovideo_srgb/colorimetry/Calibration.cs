using System;

namespace msovideo_srgb
{
    public class Calibration
    {
        public ToneCurve[] DeGamma { get; set; }
        public ToneCurve[] ReGamma { get; set; }

        public Matrix MatrixXYZToXYZ { get; set; }
        public Matrix MatrixRGBToRGB { get; set; }
        public Matrix RGBGains { get; set; }

        public double PeakLuminance { get; set; }
        public double MaxFullFrameLuminance { get; set; }
        public double MinLuminance { get; set; }

        public Colorimetry.ColorSpace NativeColorSpace { get; set; }
        public Matrix NativeColorSpacePCS { get; set; }
        public Matrix NativeWhite { get; set; }

        public ToneCurve[] FinalGamma { get; set; }
        public Colorimetry.ColorSpace TargetColorSpace { get; set; }
        public Matrix TargetColorSpacePCS { get; set; }
        public Matrix TargetWhite { get; set; }

        public Calibration(EDID edid, Colorimetry.ColorSpace targetColorSpace, Colorimetry.Point targetWhitePoint)
        {
            var profileGenerator = new ICCProfileGenerator();

            Colorimetry.ColorSpace edidColorSpace = Colorimetry.sRGB;
            Colorimetry.Point edidWhite = Colorimetry.D65;
            double edidGamma = 2.2;
            if (edid != null)
            {
                edidColorSpace = edid.ColorSpace;
                edidWhite = edidColorSpace.White;
                edidGamma = edid.Gamma;

                edidColorSpace.White = Colorimetry.D65;

                profileGenerator.SetManufacturerID(edid.ManufacturerId);
                profileGenerator.SetDeviceModel(edid.ProductCodeId);
            }

            NativeColorSpace = edidColorSpace;
            NativeColorSpacePCS = Colorimetry.RGBToPCSXYZ(NativeColorSpace);
            NativeWhite = Colorimetry.XYToXYZ(edidWhite);
            
            RGBGains = Matrix.One3x1();
            if (targetWhitePoint.Equals(Colorimetry.NativeWhite))
            {
                TargetWhite = NativeWhite;
            }
            else
            {
                TargetWhite = Colorimetry.XYToXYZ(targetWhitePoint);
                RGBGains = Colorimetry.RGBGainsForWhite(NativeColorSpacePCS, NativeWhite, TargetWhite);
                ApplyRGBGainsToNative(RGBGains);
            }

            CreateCSCMatrices(targetColorSpace);

            ToneCurve gamma = new GammaToneCurve(edidGamma);

            FinalGamma = new ToneCurve[3];
            ReGamma = new ToneCurve[3];
            for (int i = 0; i < 3; i++)
            {
                FinalGamma[i] = new ScaledToneCurve(sampleAt: (x) => gamma.SampleAt(x));
                ReGamma[i] = new ScaledToneCurve(sampleAt: (x) => gamma.SampleInverseAt(x));
            }

            PeakLuminance = 80;
            MaxFullFrameLuminance = 80;
            MinLuminance = 0;
        }

        public Calibration(ICCMatrixProfile profile, Colorimetry.ColorSpace targetColorSpace, Colorimetry.Point targetWhitePoint, double luminance,
            bool useVcgt = false,
            ToneCurve gamma = null)
        {
            NativeColorSpacePCS = profile.matrix;
            NativeColorSpace = new Colorimetry.ColorSpace(Colorimetry.PCSXYZToXYZ(NativeColorSpacePCS, Colorimetry.XYToXYZ(Colorimetry.D65)));
            NativeWhite = profile.whitePoint;
            
            RGBGains = Matrix.One3x1();
            if (targetWhitePoint.Equals(Colorimetry.NativeWhite))
            {
                if (gamma == null && !useVcgt && profile.vcgt != null)
                {
                    Matrix appliedRGBGains = Matrix.FromDiagonal(new double[] {
                        profile.TrcSample(0, profile.vcgt[0].SampleAt(1), true, Matrix.One3x1()),
                        profile.TrcSample(1, profile.vcgt[1].SampleAt(1), true, Matrix.One3x1()),
                        profile.TrcSample(2, profile.vcgt[2].SampleAt(1), true, Matrix.One3x1())
                    });

                    Matrix reverseRGBGains = appliedRGBGains.Inverse() * Matrix.One3x1();

                    TargetWhite = Colorimetry.PCSXYZToXYZ(NativeColorSpacePCS, NativeWhite) * reverseRGBGains;
                    TargetWhite /= TargetWhite[1];
                    ApplyRGBGainsToNative(reverseRGBGains);
                }
                else
                {
                    TargetWhite = NativeWhite;
                }
            }
            else
            {
                TargetWhite = Colorimetry.XYToXYZ(targetWhitePoint);
                RGBGains = Colorimetry.RGBGainsForWhite(NativeColorSpacePCS, NativeWhite, TargetWhite);
                ApplyRGBGainsToNative(RGBGains);
            }

            CreateCSCMatrices(targetColorSpace);

            double currentLuminance = profile.Luminance(RGBGains, gamma);
            RGBGains *= Math.Min(luminance, currentLuminance) / currentLuminance;

            FinalGamma = new ToneCurve[3];
            ReGamma = new ToneCurve[3];
            if (gamma != null || (useVcgt && profile.vcgt != null))
            {
                DeGamma = new ToneCurve[3];

                for (int i = 0; i < 3; i++)
                {
                    int _i = i;
                    ToneCurve scale;
                    if (gamma != null)
                    {
                        FinalGamma[i] = gamma;
                        if (gamma.IsAbsolute())
                        {
                            DeGamma[i] = new ScaledToneCurve(gamma, white: RGBGains[i]);
                            scale = new GammaToneCurve(1);
                        }
                        else
                        {
                            DeGamma[i] = new ScaledToneCurve(gamma);
                            scale = new ScaledToneCurve(new GammaToneCurve(1), black: profile.trcBlack, white: RGBGains[i]);
                        }
                    }
                    else
                    {
                        FinalGamma[i] = new ScaledToneCurve(isAbsolute: true, sampleAt: (x) => profile.TrcSample(_i, x, !useVcgt, RGBGains));
                        DeGamma[i] = new ScaledToneCurve(isAbsolute: false, sampleAt: (x) => profile.TrcSample(_i, x, !useVcgt), white: profile.TrcSample(_i, 1, !useVcgt));
                        scale = new ScaledToneCurve(new GammaToneCurve(1), profile.TrcSample(_i, 0), RGBGains[i]);
                    }
                    ReGamma[i] = new ScaledToneCurve(isAbsolute: true, sampleAt: (x) => profile.TrcSampleInverse(_i, scale.SampleAt(x)));
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    int _i = i;
                    FinalGamma[i] = new ScaledToneCurve(isAbsolute: true, sampleAt: (x) => profile.TrcSample(_i, x, !useVcgt, RGBGains));
                    ReGamma[i] = new ScaledToneCurve(isAbsolute: true, sampleAt: (x) => profile.TrcSampleInverse(_i, x));
                }
            }

            PeakLuminance = luminance;
            MaxFullFrameLuminance = luminance;
            MinLuminance = profile.tagBlack * profile.luminance;
        }

        private void ApplyRGBGainsToNative(Matrix rgbGains)
        {
            Matrix newNativeXYZ = Colorimetry.PCSXYZToXYZ(NativeColorSpacePCS, NativeWhite) * Matrix.FromDiagonal(rgbGains);
            Matrix newNativeWhite = newNativeXYZ * Matrix.One3x1();
            newNativeXYZ /= newNativeWhite[1];
            newNativeWhite /= newNativeWhite[1];

            NativeColorSpacePCS = Colorimetry.XYZToPCSXYZ(newNativeXYZ, newNativeWhite);
            NativeColorSpace = new Colorimetry.ColorSpace(Colorimetry.PCSXYZToXYZ(NativeColorSpacePCS, Colorimetry.XYToXYZ(Colorimetry.D65)));
            NativeWhite = newNativeWhite;
        }

        private void CreateCSCMatrices(Colorimetry.ColorSpace targetColorSpace)
        {
            if (targetColorSpace.Equals(Colorimetry.Native))
            {
                TargetColorSpace = NativeColorSpace;
                TargetColorSpacePCS = NativeColorSpacePCS;
                MatrixXYZToXYZ = Matrix.Identity();
                MatrixRGBToRGB = Matrix.Identity();
            }
            else
            {
                TargetColorSpace = targetColorSpace;
                TargetColorSpacePCS = Colorimetry.RGBToPCSXYZ(TargetColorSpace);
                MatrixXYZToXYZ = Colorimetry.XYZToXYZ(TargetColorSpace, NativeColorSpace);
                MatrixRGBToRGB = Colorimetry.RGBToRGB(TargetColorSpace, NativeColorSpace);
            }
        }
    }
}
