using System;

namespace msovideo_srgb
{
    public class ScaledToneCurve : ToneCurve
    {
        private readonly Func<double, double> _sampleAt;
        private readonly Func<double, double> _sampleInverseAt;
        private readonly double _black;
        private readonly double _white;
        private readonly double _targetBlack;
        private readonly double _targetWhite;

        public ScaledToneCurve(ToneCurve curve, double black = 0, double white = 1)
        {
            _sampleAt = (x) => curve.SampleAt(x);
            _sampleInverseAt = (x) => curve.SampleInverseAt(x);
            _black = _sampleAt(0);
            _white = curve.IsAbsolute() ? 1 : _sampleAt(1);
            _targetBlack = curve.IsAbsolute() ? _black : black;
            _targetWhite = white;
        }

        public ScaledToneCurve(bool isAbsolute = false, Func<double, double> sampleAt = null, Func<double, double> sampleInverseAt = null, double black = 0, double white = 1)
        {
            _sampleAt = sampleAt;
            _sampleInverseAt = sampleInverseAt;
            _black = _sampleAt(0);
            _white = isAbsolute ? 1 : _sampleAt(1);
            _targetBlack = isAbsolute ? _black : black;
            _targetWhite = white;
        }

        public bool IsAbsolute() => false;

        private double Scale(double x)
        {
            return _targetBlack + (x - _black) / (_white - _black) * (_targetWhite - _targetBlack);
        }

        public double SampleAt(double x)
        {
            return Scale(_sampleAt(x));
        }

        public double SampleInverseAt(double x)
        {
            return _sampleInverseAt(Scale(x));
        }
    }
}
