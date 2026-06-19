using System;

namespace msovideo_srgb
{
    public class LstarEOTF : ToneCurve
    {
        public bool IsAbsolute() => false;

        public double SampleAt(double x)
        {
            if (x >= 1) return 1;
            if (x <= 0) return 0;

            const double delta = 6 / 29d;

            x = (x + 0.16) / 1.16;
            
            double result;
            if (x > delta)
            {
                result = x * x * x;
            }
            else
            {
                result = 3 * (delta * delta) * (x - 4 / 29d);
            }

            return result;
        }

        public double SampleInverseAt(double x)
        {
            throw new NotImplementedException();
        }
    }
}