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

        public static void CreateProfile(string profileName, uint resolution, bool keepWhite, Colorimetry.ColorSpace originColorSpace, Colorimetry.ColorSpace targetColorSpace, Colorimetry.Point white, double gamma)
        {
            var profileGenerator = new ICCProfileGenerator();

            AddDesc(profileGenerator, profileName);

            if (keepWhite)
            {
                profileGenerator.AddTag("wtpt", ICCProfileGenerator.MakeXYZTag(Colorimetry.RGBToXYZ(Colorimetry.D65)));
            }
            else
            {
                profileGenerator.AddTag("wtpt", ICCProfileGenerator.MakeXYZTag(Colorimetry.RGBToXYZ(white)));
            }

            profileGenerator.AddTag("lumi", ICCProfileGenerator.MakeLuminanceTag(80));

            AddCurve(profileGenerator, new GammaToneCurve(gamma), resolution);

            Matrix matrix;
            if (!targetColorSpace.Equals(Colorimetry.Native))
            {
                AddMatrix(profileGenerator, targetColorSpace);
                matrix = Colorimetry.CreateMatrix(originColorSpace, targetColorSpace);
            }
            else
            {
                AddMatrix(profileGenerator, originColorSpace);
                matrix = Matrix.FromDiagonal(new double[] { 1, 1, 1 });
            }
            
            double[][] luts = new double[][] { new double[] { 0, 1 }, new double[] { 0, 1 }, new double[] { 0, 1 } };

            profileGenerator.AddTag("MHC2", ICCProfileGenerator.MakeMHC2(0, 80, matrix, luts));

            profileGenerator.SaveAs(profileName);
        }

        public static void CreateProfile(string profileName, uint resolution, bool keepWhite, ICCMatrixProfile profile, Colorimetry.ColorSpace targetColorSpace, ToneCurve curve, ToneCurve gamma = null)
        {
            var profileGenerator = new ICCProfileGenerator();

            AddDesc(profileGenerator, profileName);

            if (keepWhite)
            {
                profileGenerator.AddTag("wtpt", ICCProfileGenerator.MakeXYZTag(Colorimetry.RGBToXYZ(Colorimetry.D65)));
            }
            else
            {
                profileGenerator.AddTag("wtpt", ICCProfileGenerator.MakeXYZTag(profile.whitePoint));
            }

            profileGenerator.AddTag("lumi", ICCProfileGenerator.MakeLuminanceTag(profile.luminance));

            AddCurve(profileGenerator, curve, resolution);

            Matrix matrix;
            if (!targetColorSpace.Equals(Colorimetry.Native))
            {
                AddMatrix(profileGenerator, targetColorSpace);
                matrix = Colorimetry.CreateMatrix(profile.matrix, targetColorSpace);
            }
            else
            {
                AddMatrix(profileGenerator, profile.matrix);
                matrix = Matrix.FromDiagonal(new double[] { 1, 1, 1 });
            }

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

                        value = profile.trcs[i].SampleInverseAt(value);

                        if (profile.vcgt != null)
                        {
                            value = profile.vcgt[i].SampleAt(value);
                        }

                        luts[i][j] = value;
                    }
                }
            }
            else
            {
                luts = new double[][] { new double[] { 0, 1 }, new double[] { 0, 1 }, new double[] { 0, 1 } };
            }

            profileGenerator.AddTag("MHC2", ICCProfileGenerator.MakeMHC2(profile.tagBlack * profile.luminance, profile.luminance, matrix, luts));

            profileGenerator.SaveAs(profileName);
        }
    }
}