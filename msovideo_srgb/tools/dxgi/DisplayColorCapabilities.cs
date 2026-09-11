using System;

namespace msovideo_srgb
{
    public static class DisplayColorCapabilities
    {      
        public struct ColorCapabilities
        {
            public double PeakLuminance;
            public double MaxFullFrameLuminance;
            public double MinLuminance;
        }

        public static ColorCapabilities? GetColorCapabilities(Display display)
        {
            try
            {
                DXGI_OUTPUT_DESC1? desc1 = DXGISwapChainManager.GetDesc1(display);

                if (desc1 == null) return null;

                ColorCapabilities colorCapabilities = new ColorCapabilities()
                {
                    PeakLuminance = desc1.Value.MaxLuminance,
                    MaxFullFrameLuminance = desc1.Value.MaxFullFrameLuminance,
                    MinLuminance = desc1.Value.MinLuminance,
                };

                return colorCapabilities;        
            }
            catch (Exception) { }

            return null;
        }
    }
}
