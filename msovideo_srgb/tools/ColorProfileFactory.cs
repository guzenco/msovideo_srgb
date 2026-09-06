using System;
using System.IO;

namespace msovideo_srgb
{
    public class ColorProfileFactory
    {
        private static void AddDesc(ICCProfileGenerator profileGenerator, string profileName, bool mhc2)
        {
            profileGenerator.AddTag("desc", ICCProfileGenerator.MakeAsciiTag($"{(mhc2 ? "MHC2" : "ICC")} for {Path.GetFileNameWithoutExtension(profileName)}"));
            profileGenerator.AddTag("cprt", ICCProfileGenerator.MakeAsciiTag($"No copyright. Created with msovideo_srgb v{AboutWindow.Version}"));
        }

        private static void AddMatrix(ICCProfileGenerator profileGenerator, Colorimetry.ColorSpace target)
        {
            var matrixXYZ = Colorimetry.RGBToPCSXYZ(target);
            AddMatrix(profileGenerator, matrixXYZ);
        }

        private static void AddMatrix(ICCProfileGenerator profileGenerator, Matrix matrixXYZ)
        {
            profileGenerator.AddTag("rXYZ", ICCProfileGenerator.MakeXYZTag(matrixXYZ[0, 0], matrixXYZ[1, 0], matrixXYZ[2, 0]));
            profileGenerator.AddTag("gXYZ", ICCProfileGenerator.MakeXYZTag(matrixXYZ[0, 1], matrixXYZ[1, 1], matrixXYZ[2, 1]));
            profileGenerator.AddTag("bXYZ", ICCProfileGenerator.MakeXYZTag(matrixXYZ[0, 2], matrixXYZ[1, 2], matrixXYZ[2, 2]));
        }

        private static void AddCurve(ICCProfileGenerator profileGenerator, ToneCurve[] curves, uint resolution)
        {
            byte[] tagDataR = ICCProfileGenerator.MakeCurveTag(curves[0], resolution);
            byte[] tagDataG = ICCProfileGenerator.MakeCurveTag(curves[1], resolution);
            byte[] tagDataB = ICCProfileGenerator.MakeCurveTag(curves[2], resolution);
            profileGenerator.AddTag("rTRC", tagDataR);
            profileGenerator.AddTag("gTRC", tagDataG);
            profileGenerator.AddTag("bTRC", tagDataB);
        }

        private static void AddCurve(ICCProfileGenerator profileGenerator, ToneCurve curve, uint resolution)
        {
            var tagData = ICCProfileGenerator.MakeCurveTag(curve, resolution);
            profileGenerator.AddTag("rTRC", tagData);
            profileGenerator.AddTag("gTRC", tagData);
            profileGenerator.AddTag("bTRC", tagData);
        }

        private static Matrix OptimizeMatrix(Matrix matrixCSC, Func<int, double, double> sampleAt)
        {
            ToneCurve srgbCurve = new SrgbEOTF();

            Matrix white = Colorimetry.XYToXYZ(Colorimetry.D65);
            Matrix white3x3 = Matrix.FromDiagonal(white);

            Matrix rgbToXYZ = Colorimetry.RGBToXYZ(Colorimetry.sRGB);

            Matrix target = rgbToXYZ.Inverse() * matrixCSC * rgbToXYZ;
            target = target.Map(x => x > 0 ? x < 1 ? x : 1 : 0);
            target = Matrix.FromDiagonal(target * white).Inverse() * white3x3 * target;

            Matrix identityMatrix = Matrix.Identity();
            Matrix finalMatrixOptimization = identityMatrix;

            for (int i = 0; i < 10000; i++)
            {
                Matrix result = rgbToXYZ.Inverse() * matrixCSC * finalMatrixOptimization * rgbToXYZ;

                result = result.Map(x => x > 0 ? x < 1 ? x : 1 : 0);

                result = result.Map((r, c, x) => sampleAt(r, srgbCurve.SampleInverseAt(x)));

                result = Matrix.FromDiagonal(result * white).Inverse() * white3x3 * result;

                Matrix matrixOptimization = result.Inverse() * target;

                if (identityMatrix.DifferenceMax(matrixOptimization) < 1E-10)
                {
                    break;
                }

                matrixOptimization = 0.9 * identityMatrix + 0.1 * matrixOptimization;
                finalMatrixOptimization = finalMatrixOptimization * matrixOptimization;

            }

            return finalMatrixOptimization;
        }

        private static void AddMHC2(ICCProfileGenerator profileGenerator, Calibration calibration, ReportSettings reportSettings)
        {
            double peakLuminance = reportSettings.PeakLuminanceOverride ?? calibration.PeakLuminance;
            double minLuminance = reportSettings.MinLuminanceOverride ?? calibration.MinLuminance;

            if (!reportSettings.IncludeCalibration)
            {
                profileGenerator.AddTag("MHC2", ICCProfileGenerator.MakeMHC2(minLuminance, peakLuminance, Matrix.Identity(), new double[][] { new double[] { 0, 1 }, new double[] { 0, 1 }, new double[] { 0, 1 } }));
                return;
            }

            Matrix matrix = calibration.MatrixXYZToXYZ;
            double[][] luts = new double[3][];
            if (reportSettings.OptimizeMatrix)
            {
                Matrix matrixToOpt = reportSettings.OptimizeMatrixAcmMode ? Colorimetry.XYZToXYZ(Colorimetry.sRGB, calibration.NativeColorSpace) : matrix;

                Matrix matrixOptimization = OptimizeMatrix(matrixToOpt, (i, x) => (new ScaledToneCurve(calibration.FinalGamma[i]).SampleAt(x)));

                matrix = matrix * matrixOptimization;
            }

            for (int i = 0; i < 3; i++)
            {
                if (calibration.DeGamma != null)
                {
                    luts[i] = new double[reportSettings.CurvesResolution];

                    for (int j = 1; j < reportSettings.CurvesResolution; j++)
                    {
                        double value = j / (reportSettings.CurvesResolution - 1.0);
                        value = calibration.DeGamma[i].SampleAt(value);
                        value = calibration.ReGamma[i].SampleAt(value);
                        luts[i][j] = value;
                    }
                }
                else
                {
                    luts[i] = new double[] { 0, calibration.ReGamma[i].SampleAt(calibration.RGBGains[i]) };
                }
            }

            profileGenerator.AddTag("MHC2", ICCProfileGenerator.MakeMHC2(minLuminance, peakLuminance, matrix, luts));
        }

        public static void CreateProfile(string profileName, uint resolution)
        {
            var profileGenerator = new ICCProfileGenerator();

            AddDesc(profileGenerator, profileName, true);

            profileGenerator.AddTag("wtpt", ICCProfileGenerator.MakeXYZTag(Colorimetry.XYToXYZ(Colorimetry.D65)));
            AddMatrix(profileGenerator, Colorimetry.sRGB);

            ToneCurve gamaCurve = new SrgbEOTF();
            AddCurve(profileGenerator, gamaCurve, resolution);

            profileGenerator.AddTag("lumi", ICCProfileGenerator.MakeLuminanceTag(80));

            profileGenerator.AddTag("MHC2", ICCProfileGenerator.MakeMHC2(-1, -1));

            profileGenerator.SaveAs(profileName);
        }

        public static ICCProfileGenerator CreateProfile(string profileName, Calibration calibration, ReportSettings reportSettings)
        {
            var profileGenerator = new ICCProfileGenerator();

            AddDesc(profileGenerator, profileName, reportSettings.IncludeMHC2);

            profileGenerator.SetManufacturerID(reportSettings.ManufacturerId);
            profileGenerator.SetDeviceModel(reportSettings.ProductCodeId);

            Matrix reportWhite = reportSettings.ReportWhiteD65 ? Colorimetry.XYToXYZ(Colorimetry.D65) : calibration.TargetWhite;

            Matrix chromaticAdaptation = Colorimetry.WhiteToWhiteAdaptation(reportWhite, Colorimetry.D50);
            profileGenerator.AddTag("chad", ICCProfileGenerator.MakeMatrixTag(chromaticAdaptation));
            reportWhite = Colorimetry.D50;

            profileGenerator.AddTag("wtpt", ICCProfileGenerator.MakeXYZTag(reportWhite));

            Matrix reportedColorSpace = reportSettings.ReportColorSpaceSRGB ? Colorimetry.RGBToPCSXYZ(Colorimetry.sRGB) : calibration.TargetColorSpacePCS;
            AddMatrix(profileGenerator, reportedColorSpace);


            if (reportSettings.CurveOverride != null)
            {
                AddCurve(profileGenerator, reportSettings.CurveOverride, reportSettings.CurvesResolution);
            }
            else if (reportSettings.ReportGammaSRGB)
            {
                AddCurve(profileGenerator, new SrgbEOTF(), reportSettings.CurvesResolution);
            }
            else
            {
                AddCurve(profileGenerator, calibration.FinalGamma, reportSettings.CurvesResolution);
            }

            double maxFullFrameLuminance = reportSettings.MaxFullFrameLuminanceOverride ?? calibration.MaxFullFrameLuminance;

            profileGenerator.AddTag("lumi", ICCProfileGenerator.MakeLuminanceTag(maxFullFrameLuminance));

            if (reportSettings.IncludeMHC2)
            {
                AddMHC2(profileGenerator, calibration, reportSettings);
            }

            profileGenerator.SaveAs(profileName);

            return profileGenerator;
        }
    }
}