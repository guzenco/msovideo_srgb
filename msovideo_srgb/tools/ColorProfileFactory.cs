using System;
using System.IO;

namespace msovideo_srgb
{
    public class ColorProfileFactory
    {
        private static void AddDesc(ICCProfileGenerator profileGenerator, string profileName)
        {
            profileGenerator.AddTag("desc", ICCProfileGenerator.MakeAsciiTag("MHC2 for " + Path.GetFileNameWithoutExtension(profileName)));
            profileGenerator.AddTag("cprt", ICCProfileGenerator.MakeAsciiTag("No copyright, use freely"));
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

        public static void CreateProfile(string profileName, uint resolution, Colorimetry.ColorSpace originColorSpace, Colorimetry.ColorSpace targetColorSpace, Colorimetry.Point white, Colorimetry.Point targetWhitePoint, double gamma)
        {
            var profileGenerator = new ICCProfileGenerator();

            AddDesc(profileGenerator, profileName);

            Matrix matrixCsc;
            if (!targetColorSpace.Equals(Colorimetry.Native))
            {
                AddMatrix(profileGenerator, targetColorSpace);
                matrixCsc = Colorimetry.CreateMatrix(originColorSpace, targetColorSpace);
            }
            else
            {
                AddMatrix(profileGenerator, originColorSpace);
                matrixCsc = Matrix.FromDiagonal(new double[] { 1, 1, 1 });
            }

            Matrix matrixWhite = Matrix.FromDiagonal(Matrix.One3x1());
            if (targetWhitePoint.Equals(Colorimetry.NativeWhite))
            {
                profileGenerator.AddTag("wtpt", ICCProfileGenerator.MakeXYZTag(Colorimetry.RGBToXYZ(white)));  
            }
            else
            {
                Matrix targetWhite = Colorimetry.RGBToXYZ(targetWhitePoint);
                matrixWhite = Matrix.FromDiagonal(Colorimetry.XYZScale(Colorimetry.RGBToXYZ(originColorSpace), Colorimetry.RGBToXYZ(white)).Inverse() * targetWhite);
                double scale = Math.Max(Math.Max(matrixWhite[0, 0], matrixWhite[1, 1]), matrixWhite[2, 2]);
                matrixWhite = Matrix.FromDiagonal(new double[] { matrixWhite[0, 0] / scale, matrixWhite[1, 1] / scale, matrixWhite[2, 2] / scale });
                profileGenerator.AddTag("wtpt", ICCProfileGenerator.MakeXYZTag(targetWhite));
            }


            ToneCurve gamaCurve = new GammaToneCurve(gamma);
            AddCurve(profileGenerator, gamaCurve, resolution);

            double[][] luts = new double[][] {
                    new double[] { 0, gamaCurve.SampleInverseAt(matrixWhite[0, 0]) },
                    new double[] { 0, gamaCurve.SampleInverseAt(matrixWhite[1, 1]) },
                    new double[] { 0, gamaCurve.SampleInverseAt(matrixWhite[2, 2]) }
                };

            Matrix matrix = matrixCsc;

            profileGenerator.AddTag("lumi", ICCProfileGenerator.MakeLuminanceTag(80));

            profileGenerator.AddTag("MHC2", ICCProfileGenerator.MakeMHC2(0, 80, matrix, luts));

            profileGenerator.SaveAs(profileName);
        }

        public static void CreateProfile(string profileName, uint resolution, ICCMatrixProfile profile, Colorimetry.ColorSpace targetColorSpace, Colorimetry.Point targetWhitePoint, double luminance, ToneCurve curve, ToneCurve gamma = null)
        {
            var profileGenerator = new ICCProfileGenerator();

            AddDesc(profileGenerator, profileName);

            Matrix matrixCSC = Matrix.FromDiagonal(Matrix.One3x1());
            if (!targetColorSpace.Equals(Colorimetry.Native))
            {
                AddMatrix(profileGenerator, targetColorSpace);
                matrixCSC = Colorimetry.CreateMatrix(profile.matrix, targetColorSpace);
            }
            else
            {
                AddMatrix(profileGenerator, profile.matrix);
            }

            Matrix matrixWhite = Matrix.FromDiagonal(Matrix.One3x1());
            if (targetWhitePoint.Equals(Colorimetry.NativeWhite))
            {
                profileGenerator.AddTag("wtpt", ICCProfileGenerator.MakeXYZTag(profile.whitePoint));
            }
            else
            {
                Matrix targetWhite = Colorimetry.RGBToXYZ(targetWhitePoint);
                matrixWhite = Matrix.FromDiagonal(Colorimetry.XYZScale(profile.matrix * Colorimetry.WhiteToWhiteAdaptation(Colorimetry.D50, profile.whitePoint), profile.whitePoint).Inverse() * targetWhite);
                double scale = Math.Max(Math.Max(matrixWhite[0, 0], matrixWhite[1, 1]), matrixWhite[2, 2]);
                matrixWhite = Matrix.FromDiagonal(new double[] { matrixWhite[0, 0] / scale, matrixWhite[1, 1] / scale, matrixWhite[2, 2] / scale });
                profileGenerator.AddTag("wtpt", ICCProfileGenerator.MakeXYZTag(targetWhite));
            }

            AddCurve(profileGenerator, curve, resolution);

            double[][] luts;

            if (gamma != null)
            {
                luts = new double[3][];
                for (int i = 0; i < 3; i++)
                {
                    luts[i] = new double[resolution];
                    for (int j = 1; j < resolution; j++)
                    {
                        double value = gamma.SampleAt(j / (resolution - 1.0));

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