using EDIDParser;
using System;

namespace msovideo_srgb
{
    public class ExtensionCTA861
    {
        public double MaxLuminance { get; }
        public double MaxFullFrameLuminance { get; }
        public double MinLuminance { get; }

        public ExtensionCTA861(double max, double maxFullFrame, double min)
        {
            MaxLuminance = max;
            MaxFullFrameLuminance = maxFullFrame;
            MinLuminance = min;
        }
    }

    public class ExtendedEDID : EDID
    {
        private readonly byte[] _rawData;

        public ExtendedEDID(byte[] rawData) : base(rawData)
        {
            _rawData = rawData;
        }

        public ExtensionCTA861 ExtensionCTA861 {
            get 
            {

                for (int i = 1; i <= NumberOfExtensions; i++)
                {
                    int offset = i * 128;
                    byte[] block = new byte[128];
                    Array.Copy(_rawData, offset, block, 0, 128);

                    if (block[0] == 0x02)
                    {
                        var hdr = ParseCtaBlock(block);
                        if (hdr != null)
                        {
                            return hdr;
                        }
                    }
                }
                return null;
            }
        }

        private ExtensionCTA861 ParseCtaBlock(byte[] block)
        {
            int dtdOffset = block[2];
            int pos = 4;
            int end = dtdOffset;

            while (pos < end)
            {
                byte tagLen = block[pos++];
                int blockType = (tagLen & 0xE0) >> 5;
                int blockLen = tagLen & 0x1F;
                if (blockLen == 0) continue;

                if (blockType == 0x07)
                {
                    if (pos >= end) break;
                    byte extTag = block[pos];
                    if (extTag == 0x06)
                    {
                        int payloadStart = pos + 1;
                        if (payloadStart + 4 < pos + blockLen)
                        {
                            byte eotf = block[payloadStart + 0];
                            byte smFlags = block[payloadStart + 1];
                            byte maxByte = block[payloadStart + 2];
                            byte maxFrameByte = block[payloadStart + 3];
                            byte minByte = block[payloadStart + 4];

                            double max = 50 * Math.Pow(2, maxByte / 32);
                            double maxFrame = 50 * Math.Pow(2, maxFrameByte / 32);
                            double min = max * Math.Pow(minByte / 255.0, 2) / 100;

                            return new ExtensionCTA861(max, maxFrame, min);
                        }
                    }
                }
                pos += blockLen;
            }
            return null;
        }

    }
}
