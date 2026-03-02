namespace msovideo_srgb
{
    public interface ToneCurve
    {
        double SampleAt(double x);
        double SampleInverseAt(double x);
    }
}