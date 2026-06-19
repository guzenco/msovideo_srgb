namespace msovideo_srgb
{
    public interface ToneCurve
    {
        bool IsAbsolute();
        double SampleAt(double x);
        double SampleInverseAt(double x);
    }
}