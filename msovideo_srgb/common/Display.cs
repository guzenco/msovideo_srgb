using EDIDParser;
using Microsoft.Win32;
using System.Linq;

namespace msovideo_srgb
{
    public class Display
    {
        public string DevicePath { get; set; }
        public string FriendlyDeviceName { get; set; }

        public bool IsSourceUnique { get; set; }
        public LUID SourceAdapterId { get; set; }
        public uint SourceId { get; set; }
        public string SourceDeviceName { get; set; }

        public bool HdrActive { get; set; }
        public bool AcmActive { get; set; }

        public string RegistryPath => "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Enum\\DISPLAY\\" + string.Join("\\", DevicePath.Split('#').Skip(1).Take(2));

        public string GetDriver()
        {
            try
            {
                return (string)Registry.GetValue(RegistryPath, "Driver", null);
            }
            catch
            {
                return null;
            }
        }

        public EDID GetEDID()
        {
            try
            {
                return new EDID((byte[])Registry.GetValue(RegistryPath + "\\Device Parameters", "EDID", null));
            }
            catch
            {
                return null;
            }
        }
    }
}
