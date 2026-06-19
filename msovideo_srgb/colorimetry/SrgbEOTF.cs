using System;

namespace msovideo_srgb
{
    public class SrgbEOTF : ToneCurve
    {
        public bool IsAbsolute() => false;

        public double SampleAt(double x)
        {
            if (x >= 1) return 1;

            double result;
            if (x <= 0.04045)
            {
                result = x / 12.92;
            }
            else
            {
                result = Math.Pow((x + 0.055) / 1.055, 2.4);
            }

            return result;
        }

        public double SampleInverseAt(double x)
        {
            if (x >= 1) return 1;

            if (x <= 0.0031308) return 12.92 * x;
            return 1.055 * Math.Pow(x, 1 / 2.4) - 0.055;
        }
    }
}