using System;

namespace msovideo_srgb
{
    public class GammaToneCurve : ToneCurve
    {
        private readonly double _gamma;
        private double _a = 1;
        private double _b;
        private readonly double _c;

        private readonly double _black;

        public unsafe GammaToneCurve(double gamma, double black = 0, double outputOffset = 1, bool relative = false)
        {
            _black = black;
            if (_black == 0)
            {
                _gamma = gamma;
                return;
            }

            if (outputOffset == 1)
            {
                _gamma = !relative
                    ? gamma
                    : Math.Log((_black - 1) * Math.Pow(2, gamma) / (_black * Math.Pow(2, gamma) - 1), 2);
                _a = 1 - _black;
                _c = _black;
            }
            else
            {
                var outBlack = outputOffset * _black;
                var btWhite = 1 - outBlack;
                var btBlack = _black - outBlack;
                _c = outBlack;

                if (!relative)
                {
                    _gamma = gamma;
                    CalculateBT1886(btWhite, btBlack);
                }
                else
                {
                    // assume sane values for black and gamma
                    double lowD = 1;
                    double highD = 8;

                    // what the hell
                    var low = *(ulong*)&lowD;
                    var high = *(ulong*)&highD;

                    var target = Math.Pow(0.5, gamma);

                    while (true)
                    {
                        var mid = (low + high) / 2;
                        _gamma = *(double*)&mid;
                        CalculateBT1886(btWhite, btBlack);
                        var sample = SampleAt(0.5);
                        if (sample == target || low == mid || high == mid)
                        {
                            break;
                        }

                        if (sample > target)
                        {
                            low = mid;
                        }
                        else
                        {
                            high = mid;
                        }
                    }
                }
            }
        }

        public bool IsAbsolute() => false;

        private void CalculateBT1886(double white, double black)
        {
            var lwg = Math.Pow(white, 1 / _gamma);
            var lbg = Math.Pow(black, 1 / _gamma);
            _a = Math.Pow(lwg - lbg, _gamma);
            _b = lbg / (lwg - lbg);
        }

        public double SampleAt(double x)
        {
            if (x >= 1) return 1;
            var res = _a * Math.Pow(Math.Max(x + _b, 0), _gamma) + _c;
            return (res - _black) / (1.0 - _black);
        }

        public double SampleInverseAt(double x)
        {
            if (_a != 1) throw new NotSupportedException();
            if (x >= 1) return 1;
            return Math.Pow(x, 1 / _gamma);
        }
    }
}