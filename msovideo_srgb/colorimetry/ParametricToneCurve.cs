using System;

namespace msovideo_srgb
{
    public class ParametricToneCurve : ToneCurve
    {
        private double _functionType;
        private double _g;
        private double _a;
        private double _b;
        private double _c;
        private double _d;
        private double _e;
        private double _f;

        public ParametricToneCurve(uint functionType, double[] parametrs)
        {
            Array.Resize(ref parametrs, 7);

            _functionType = functionType;  
            _g = parametrs[0];
            _a = parametrs[1];
            _b = parametrs[2];
            _c = parametrs[3];
            _d = parametrs[4];
            _e = parametrs[5];
            _f = parametrs[6];
        }

        public bool IsAbsolute() => true;

        public double SampleAt(double x)
        {
            if (x > 1) x = 1;
            if (x < 0) x = 0;

            double y;
            switch (_functionType)
            {
                case 0:
                    y = Math.Pow(x, _g);
                    break;

                case 1:
                    if(x >= -_b / _a)
                    {
                        y = Math.Pow(_a * x + _b, _g);
                    }
                    else
                    {
                        y = 0;
                    }
                    break;

                case 2:
                    if (x >= -_b / _a)
                    {
                        y = Math.Pow(_a * x + _b, _g) + _c;
                    }
                    else
                    {
                        y = _c;
                    }
                    break;

                case 3:
                    if (x >= _d)
                    {
                        y = Math.Pow(_a * x + _b, _g);
                    }
                    else
                    {
                        y = _c * x;
                    }
                    break;

                case 4:
                    if (x >= _d)
                    {
                        y = Math.Pow(_a * x + _b, _g) + _e;
                    }
                    else
                    {
                        y = _c * x + _f;
                    }
                    break;

                default:
                    throw new NotSupportedException($"Unsupported function type " + _functionType);
            }

            if (y <= 0) return 0;
            if (y >= 1) return 1; 

            return y;
        }

        public double SampleInverseAt(double y)
        {
            if (y <= SampleAt(0)) return 0;
            if (y >= SampleAt(1)) return 1;

            double x;
            switch (_functionType)
            {
                case 0:
                    x = Math.Pow(y, 1 / _g);
                    break;

                case 1:
                    if (y >= SampleAt(-_b / _a))
                    {
                        x = (Math.Pow(y, 1.0 / _g) - _b) / _a;
                    }
                    else
                    {
                        x = 0;
                    }
                    break;

                case 2:
                    if (y >= SampleAt(-_b / _a))
                    {
                        x = (Math.Pow(y - _c, 1.0 / _g) - _b) / _a;
                    }
                    else
                    {
                        x = 0;
                    }
                    break;

                case 3:
                    if (y >= SampleAt(_d))
                    {
                        x = (Math.Pow(y, 1.0 / _g) - _b) / _a;
                    }
                    else
                    {
                       x = y / _c;
                    }
                    break;

                case 4:
                    if (y >= SampleAt(_d))
                    {
                        x = (Math.Pow(y - _e, 1.0 / _g) - _b) / _a;
                    }
                    else
                    {
                        x = (y - _f) / _c;
                    }
                    break;

                default:
                    throw new NotSupportedException($"Unsupported function type " +  _functionType);
            }

            if (x <= 0) return 0;
            if (x >= 1) return 1;

            return x;
        }
    }
}
