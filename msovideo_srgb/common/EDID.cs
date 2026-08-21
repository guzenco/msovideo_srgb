using System;
using System.Linq;

namespace msovideo_srgb
{
    public class EDID
    {
        private static readonly byte[] Header = { 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00 };
        
        private byte[] _data;

        public EDID(byte[] data)
        {
            if (data.Length < 128) throw new EDIDException("EDID data length < 128 bytes");
            if (!data.Take(8).SequenceEqual(Header)) throw new EDIDException("EDID header mismatch");
            if ((data.Take(127).Sum(b => b) & 0xFF) == data[127]) throw new EDIDException("EDID checksum missmatch");

            _data = data;
        }

        public uint ManufacturerId => (uint)(_data[0x09] << 8) | _data[0x08];
        public uint ProductCodeId => (uint)(_data[0x0B] << 8) | _data[0x0A];

        public double Gamma => _data[0x17] != 0xFF ? (_data[0x17] + 100) / 100d : 2.2;

        public double RedX => ((_data[0x1B] << 2) | (_data[0x19] >> 6 & 0b11)) / 1024d;
        public double RedY => ((_data[0x1C] << 2) | (_data[0x19] >> 4 & 0b11)) / 1024d;

        public double GreenX => ((_data[0x1D] << 2) | (_data[0x19] >> 2 & 0b11)) / 1024d;
        public double GreenY => ((_data[0x1E] << 2) | (_data[0x19] >> 0 & 0b11)) / 1024d;

        public double BlueX => ((_data[0x1F] << 2) | (_data[0x1A] >> 6 & 0b11)) / 1024d;
        public double BlueY => ((_data[0x20] << 2) | (_data[0x1A] >> 4 & 0b11)) / 1024d;

        public double WhiteX => ((_data[0x21] << 2) | (_data[0x1A] >> 2 & 0b11)) / 1024d;
        public double WhiteY => ((_data[0x22] << 2) | (_data[0x1A] >> 0 & 0b11)) / 1024d;

        public Colorimetry.ColorSpace ColorSpace => new Colorimetry.ColorSpace
        {
            Red = new Colorimetry.Point { X = Math.Round(RedX, 3), Y = Math.Round(RedY, 3) },
            Green = new Colorimetry.Point { X = Math.Round(GreenX, 3), Y = Math.Round(GreenY, 3) },
            Blue = new Colorimetry.Point { X = Math.Round(BlueX, 3), Y = Math.Round(BlueY, 3) },
            White = new Colorimetry.Point { X = Math.Round(WhiteX, 3), Y = Math.Round(WhiteY, 3) }
        };
    }
}
