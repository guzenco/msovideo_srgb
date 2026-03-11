using System;
using System.IO;
using EDIDParser;

namespace msovideo_srgb
{
    public class ColorProfileFactory
    {
        private static void AddDesc(ICCProfileGenerator profileGenerator, string profileName)
        {
            profileGenerator.AddTag("desc", ICCProfileGenerator.MakeAsciiTag("MHC2 for " + Path.GetFileNameWithoutExtension(profileName)));
            profileGenerator.AddTag("cprt", ICCProfileGenerator.MakeAsciiTag("No copyright. Created with msovideo_srgb v" + AboutWindow.Version));
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

        private static void AddCurve(ICCProfileGenerator profileGenerator, ToneCurve curve, uint resolution)
        {
            var tagData = ICCProfileGenerator.MakeCurveTag(curve, resolution);
            profileGenerator.AddTag("rTRC", tagData);
            profileGenerator.AddTag("gTRC", tagData);
            profileGenerator.AddTag("bTRC", tagData);
        }

        private static void AddCurve(ICCProfileGenerator profileGenerator, ICCMatrixProfile profile, uint resolution)
        {
            var tagDataR = ICCProfileGenerator.MakeCurveTag(profile.trcs[0], resolution);
            var tagDataG = ICCProfileGenerator.MakeCurveTag(profile.trcs[1], resolution);
            var tagDataB = ICCProfileGenerator.MakeCurveTag(profile.trcs[2], resolution);
            profileGenerator.AddTag("rTRC", tagDataR);
            profileGenerator.AddTag("gTRC", tagDataG);
            profileGenerator.AddTag("bTRC", tagDataB);
        }

        public static void CreateProfile(string profileName, uint resolution)
        {
            var profileGenerator = new ICCProfileGenerator();

            AddDesc(profileGenerator, profileName);

            profileGenerator.AddTag("wtpt", ICCProfileGenerator.MakeXYZTag(Colorimetry.RGBToXYZ(Colorimetry.D65)));
            AddMatrix(profileGenerator, Colorimetry.sRGB);

            ToneCurve gamaCurve = new SrgbEOTF(0);
            AddCurve(profileGenerator, gamaCurve, resolution);

            profileGenerator.AddTag("lumi", ICCProfileGenerator.MakeLuminanceTag(80));

            profileGenerator.AddTag("MHC2", ICCProfileGenerator.MakeMHC2(-1, -1));

            profileGenerator.SaveAs(profileName);
        }

        public static void CreateProfile(string profileName, uint resolution, ExtendedEDID edid)
        {
            var profileGenerator = new ICCProfileGenerator();

            AddDesc(profileGenerator, profileName);

            var coords = edid.DisplayParameters.ChromaticityCoordinates;
            Colorimetry.ColorSpace edidColorSpace = new Colorimetry.ColorSpace
            {
                Red = new Colorimetry.Point { X = Math.Round(coords.RedX, 3), Y = Math.Round(coords.RedY, 3) },
                Green = new Colorimetry.Point { X = Math.Round(coords.GreenX, 3), Y = Math.Round(coords.GreenY, 3) },
                Blue = new Colorimetry.Point { X = Math.Round(coords.BlueX, 3), Y = Math.Round(coords.BlueY, 3) },
                White = Colorimetry.D65
            };
            Colorimetry.Point edidWhite = new Colorimetry.Point { X = Math.Round(coords.WhiteX, 3), Y = Math.Round(coords.WhiteY, 3) };
            double edidGamma = edid.DisplayParameters.DisplayGamma;
            ExtensionCTA861 cta = edid.ExtensionCTA861;

            profileGenerator.SetManufacturerID(edid.ManufacturerId);
            profileGenerator.setDeviceModel(edid.ProductCode);


            profileGenerator.AddTag("wtpt", ICCProfileGenerator.MakeXYZTag(Colorimetry.RGBToXYZ(Colorimetry.D65)));
            AddMatrix(profileGenerator, Colorimetry.sRGB);

            ToneCurve gamaCurve = new GammaToneCurve(edidGamma);
            AddCurve(profileGenerator, gamaCurve, resolution);

            profileGenerator.AddTag("lumi", ICCProfileGenerator.MakeLuminanceTag(cta.MaxFullFrameLuminance));

            double[][] luts = new double[][] {
                    new double[] { 0, 1 },
                    new double[] { 0, 1 },
                    new double[] { 0, 1 }
                };

            Matrix matrix = Matrix.FromDiagonal(Matrix.One3x1());

            profileGenerator.AddTag("MHC2", ICCProfileGenerator.MakeMHC2(cta.MinLuminance, cta.MaxLuminance, matrix, luts));

            profileGenerator.SaveAs(profileName);
        }

        public static void CreateProfile(string profileName, uint resolution, EDID edid, Colorimetry.ColorSpace targetColorSpace, Colorimetry.Point targetWhitePoint, bool reportWhiteD65, bool reportColorSpaceSRGB, bool reportGammaSRGB)
        {
            var profileGenerator = new ICCProfileGenerator();

            AddDesc(profileGenerator, profileName);

            Colorimetry.ColorSpace edidColorSpace;
            Colorimetry.Point edidWhite;
            double edidGamma;
            if (edid != null)
            {
                var coords = edid.DisplayParameters.ChromaticityCoordinates;
                edidColorSpace = new Colorimetry.ColorSpace
                {
                    Red = new Colorimetry.Point { X = Math.Round(coords.RedX, 3), Y = Math.Round(coords.RedY, 3) },
                    Green = new Colorimetry.Point { X = Math.Round(coords.GreenX, 3), Y = Math.Round(coords.GreenY, 3) },
                    Blue = new Colorimetry.Point { X = Math.Round(coords.BlueX, 3), Y = Math.Round(coords.BlueY, 3) },
                    White = Colorimetry.D65
                };
                edidWhite = new Colorimetry.Point { X = Math.Round(coords.WhiteX, 3), Y = Math.Round(coords.WhiteY, 3) };
                edidGamma = edid.DisplayParameters.DisplayGamma;

                profileGenerator.SetManufacturerID(edid.ManufacturerId);
                profileGenerator.setDeviceModel(edid.ProductCode);
            }
            else
            {
                edidColorSpace = Colorimetry.sRGB;
                edidWhite = Colorimetry.D65;
                edidGamma = 2.2;
            }

            Matrix targetWhite;
            Matrix matrixWhite = Matrix.FromDiagonal(Matrix.One3x1());
            if (targetWhitePoint.Equals(Colorimetry.NativeWhite))
            {
                targetWhite = Colorimetry.RGBToXYZ(edidWhite);
            }
            else
            {
                targetWhite = Colorimetry.RGBToXYZ(targetWhitePoint);
                matrixWhite = Matrix.FromDiagonal(Colorimetry.XYZScale(Colorimetry.RGBToXYZ(edidColorSpace), Colorimetry.RGBToXYZ(edidWhite)).Inverse() * targetWhite);
                double scale = Math.Max(Math.Max(matrixWhite[0, 0], matrixWhite[1, 1]), matrixWhite[2, 2]);
                matrixWhite = Matrix.FromDiagonal(new double[] { matrixWhite[0, 0] / scale, matrixWhite[1, 1] / scale, matrixWhite[2, 2] / scale });
            }

            Matrix reportedWhite = reportWhiteD65 ? Colorimetry.RGBToXYZ(Colorimetry.D65) : targetWhite;

            Matrix chromaticAdaptation = Colorimetry.WhiteToWhiteAdaptation(reportedWhite, Colorimetry.D50);
            profileGenerator.AddTag("chad", ICCProfileGenerator.MakeMatrixTag(chromaticAdaptation));
            reportedWhite = Colorimetry.D50;

            profileGenerator.AddTag("wtpt", ICCProfileGenerator.MakeXYZTag(reportedWhite));

            Colorimetry.ColorSpace finalColorSpace; 
            Matrix matrixCsc = Matrix.FromDiagonal(Matrix.One3x1());
            if (targetColorSpace.Equals(Colorimetry.Native))
            {
                finalColorSpace = edidColorSpace;
            }
            else
            {
                finalColorSpace = targetColorSpace;  
                matrixCsc = Colorimetry.CreateMatrix(edidColorSpace, targetColorSpace);
            }

            Colorimetry.ColorSpace reportedColorSpace = reportColorSpaceSRGB ? Colorimetry.sRGB : finalColorSpace;
            AddMatrix(profileGenerator, reportedColorSpace);

            ToneCurve gamaCurve = new GammaToneCurve(edidGamma);
            ToneCurve reportedCurve = reportGammaSRGB ? new SrgbEOTF(0) : gamaCurve;
            AddCurve(profileGenerator, reportedCurve, resolution);

            double[][] luts = new double[][] {
                    new double[] { 0, gamaCurve.SampleInverseAt(matrixWhite[0, 0]) },
                    new double[] { 0, gamaCurve.SampleInverseAt(matrixWhite[1, 1]) },
                    new double[] { 0, gamaCurve.SampleInverseAt(matrixWhite[2, 2]) }
                };

            Matrix matrix = matrixCsc;

            profileGenerator.AddTag("lumi", ICCProfileGenerator.MakeLuminanceTag(80));

            profileGenerator.AddTag("MHC2", ICCProfileGenerator.MakeMHC2(-1, -1, matrix, luts));

            profileGenerator.SaveAs(profileName);
        }

        public static void CreateProfile(string profileName, uint resolution, EDID edid, ICCMatrixProfile profile, Colorimetry.ColorSpace targetColorSpace, Colorimetry.Point targetWhitePoint, bool reportWhiteD65, bool reportColorSpaceSRGB, bool reportGammaSRGB, double luminance, ToneCurve curve = null, ToneCurve gamma = null)
        {
            var profileGenerator = new ICCProfileGenerator();

            AddDesc(profileGenerator, profileName);

            if (edid != null)
            {
                profileGenerator.SetManufacturerID(edid.ManufacturerId);
                profileGenerator.setDeviceModel(edid.ProductCode);
            }

            Matrix targetWhite;
            Matrix matrixWhite = Matrix.FromDiagonal(Matrix.One3x1());
            if (targetWhitePoint.Equals(Colorimetry.NativeWhite))
            {
                targetWhite = profile.whitePoint;
            }
            else
            {
                targetWhite = Colorimetry.RGBToXYZ(targetWhitePoint);
                matrixWhite = Matrix.FromDiagonal(Colorimetry.XYZScale(profile.matrix * Colorimetry.WhiteToWhiteAdaptation(Colorimetry.D50, profile.whitePoint), profile.whitePoint).Inverse() * targetWhite);
                double scale = Math.Max(Math.Max(matrixWhite[0, 0], matrixWhite[1, 1]), matrixWhite[2, 2]);
                matrixWhite = Matrix.FromDiagonal(new double[] { matrixWhite[0, 0] / scale, matrixWhite[1, 1] / scale, matrixWhite[2, 2] / scale });
            }

            Matrix reportWhite = reportWhiteD65 ? Colorimetry.RGBToXYZ(Colorimetry.D65) : targetWhite;

            Matrix chromaticAdaptation = Colorimetry.WhiteToWhiteAdaptation(reportWhite, Colorimetry.D50);
            profileGenerator.AddTag("chad", ICCProfileGenerator.MakeMatrixTag(chromaticAdaptation));
            reportWhite = Colorimetry.D50;

            profileGenerator.AddTag("wtpt", ICCProfileGenerator.MakeXYZTag(reportWhite));

            Matrix finalColorSpace;
            Matrix matrixCSC = Matrix.FromDiagonal(Matrix.One3x1());
            if (targetColorSpace.Equals(Colorimetry.Native))
            {
                finalColorSpace = profile.matrix;
            }
            else
            {
                finalColorSpace = Colorimetry.RGBToPCSXYZ(targetColorSpace);
                matrixCSC = Colorimetry.CreateMatrix(profile.matrix, targetColorSpace);
            }

            Matrix reportedColorSpace = reportColorSpaceSRGB ? Colorimetry.RGBToPCSXYZ(Colorimetry.sRGB) : finalColorSpace;
            AddMatrix(profileGenerator, reportedColorSpace);

            ToneCurve reportedCurve = reportGammaSRGB ? new SrgbEOTF(0) : curve;
            if (reportedCurve != null)
            {
                AddCurve(profileGenerator, reportedCurve, resolution);
            }
            else
            {
                AddCurve(profileGenerator, profile, resolution);
            }

            double[][] luts;

            if (gamma != null || profile.vcgt != null)
            {
                luts = new double[3][];
                for (int i = 0; i < 3; i++)
                {
                    luts[i] = new double[resolution];
                    for (int j = 1; j < resolution; j++)
                    {
                        double value = j / (resolution - 1.0);

                        if (gamma != null)
                        {
                            value = gamma.SampleAt(value);
                        }
                        else
                        {
                            value = profile.trcs[i].SampleAt(value);
                        }

                        value = profile.TrcSampleInverse(i, value * matrixWhite[i, i]);

                        luts[i][j] = value;
                    }
                }
            }
            else
            {
                luts = new double[][] {
                    new double[] { 0, profile.TrcSampleInverse(0, matrixWhite[0, 0]) },
                    new double[] { 0, profile.TrcSampleInverse(1, matrixWhite[1, 1]) },
                    new double[] { 0, profile.TrcSampleInverse(2, matrixWhite[2, 2]) }
                };
            }

            Matrix matrix = matrixCSC;

            profileGenerator.AddTag("lumi", ICCProfileGenerator.MakeLuminanceTag(luminance));

            profileGenerator.AddTag("MHC2", ICCProfileGenerator.MakeMHC2(profile.tagBlack * profile.luminance, luminance, matrix, luts));

            profileGenerator.SaveAs(profileName);
        }
    }
}