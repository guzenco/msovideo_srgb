using System;

namespace msovideo_srgb
{
    public class ST2084 : ToneCurve
    {
        private const double m1 = 2610.0 / 16384.0;
        private const double m2 = 2523.0 / 32.0;
        private const double c1 = 3424.0 / 4096.0;
        private const double c2 = 2413.0 / 128.0;
        private const double c3 = 2392.0 / 128.0;

        private double _maxLuminance;
        private double _displayMaxLuminance;
        private double _displayMinLuminance;
        private double _bpsThreashold;

        public ST2084(double maxLuminance = 10000.0, double displayMinLuminance = 0, double displayMaxLuminance = 10000.0, double bpsThreashold = 0)
        {
            _maxLuminance = maxLuminance;
            _displayMinLuminance = displayMinLuminance;
            _displayMaxLuminance = displayMaxLuminance;
            _bpsThreashold = bpsThreashold;
        }

        public bool IsAbsolute() => true;

        public double SampleAt(double x)
        {
            double pow = Math.Pow(x, 1.0 / m2);
            double L = 10000 * Math.Pow(Math.Max(pow - c1, 0) / (c2 - c3 * pow), 1.0 / m1);

            L = Math.Min(L, _maxLuminance);

            if (L < _bpsThreashold)
            {
                L = _displayMinLuminance + L / _bpsThreashold * (_bpsThreashold - _displayMinLuminance);
            }

            L /= _displayMaxLuminance;

            L = Math.Min(L, 1);

            return L;
        }

        public double SampleInverseAt(double x)
        {
            double L = x * 10000;

            double pow = Math.Pow(L / 10000, m1);
            double res = Math.Pow((c1 + c2 * pow) / (1.0 + c3 * pow), m2);

            return res;
        }
    }
}