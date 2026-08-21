using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace msovideo_srgb
{
    public static class DisplayColorProfileManager
    {
        internal const uint CLASS_MONITOR = 0x6D6E7472;

        public enum WcsProfileManagementScope : uint
        {
            SystemWide = 0,
            CurrentUser = 1
        }

        internal enum COLORPROFILETYPE : uint
        {
            CPT_ICC = 0,
            CPT_DMP = 1,
            CPT_CAMP = 2,
            CPT_GMMP = 3
        }

        internal enum COLORPROFILESUBTYPE : uint
        {
            CPST_PERCEPTUAL = 0,
            CPST_RELATIVE_COLORIMETRIC = 1,
            CPST_SATURATION = 2,
            CPST_ABSOLUTE_COLORIMETRIC = 3,
            CPST_NONE = 4,
            CPST_RGB_WORKING_SPACE = 5,
            CPST_CUSTOM_WORKING_SPACE = 6,
            CPST_STANDARD_DISPLAY_COLOR_MODE = 7,
            CPST_EXTENDED_DISPLAY_COLOR_MODE = 8
        }

        internal enum WCS_DEVICE_CAPABILITIES_TYPE: uint
        {
            VideoCardGammaTable = 1,
            MicrosoftHardwareColorV2 = 2,
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WCS_DEVICE_MHC2_CAPABILITIES
        {
            public uint Size;
            [MarshalAs(UnmanagedType.Bool)]
            public bool SupportsMhc2;
            public uint RegammaLutEntryCount;
            public uint CscXyzMatrixRows;
            public uint CscXyzMatrixColumns; 
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("mscms.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int ColorProfileAddDisplayAssociation(
            WcsProfileManagementScope scope,
            string profileName,
            LUID targetAdapterID,
            uint sourceID,
            [MarshalAs(UnmanagedType.Bool)] bool setAsDefault,
            [MarshalAs(UnmanagedType.Bool)] bool associateAsAdvancedColor);

        [DllImport("mscms.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int ColorProfileRemoveDisplayAssociation(
            WcsProfileManagementScope scope,
            string profileName,
            LUID targetAdapterID,
            uint sourceID,
            [MarshalAs(UnmanagedType.Bool)] bool dissociateAdvancedColor);

        [DllImport("mscms.dll", CharSet = CharSet.Unicode)]
        private static extern int ColorProfileGetDisplayDefault(
            WcsProfileManagementScope scope,
            LUID targetAdapterID,
            uint sourceID,
            COLORPROFILETYPE profileType,
            COLORPROFILESUBTYPE profileSubType,
            out IntPtr profileNamePtr);

        [DllImport("mscms.dll", CharSet = CharSet.Unicode)]
        private static extern int ColorProfileSetDisplayDefaultAssociation(
            WcsProfileManagementScope scope,
            string profileName,
            COLORPROFILETYPE profileType,
            COLORPROFILESUBTYPE profileSubType,
            LUID targetAdapterID,
            uint sourceID);

        [DllImport("mscms.dll")]
        private static extern int ColorProfileGetDisplayUserScope(
            LUID targetAdapterID,
            uint sourceID,
            out WcsProfileManagementScope scope
        );

        [DllImport("mscms.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WcsSetUsePerUserProfiles(
            string pDeviceName,
            uint dwDeviceClass,
            [MarshalAs(UnmanagedType.Bool)] bool usePerUserProfiles);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private delegate int ColorProfileGetDeviceCapabilitiesDelegate(
            WcsProfileManagementScope scope,
            LUID targetAdapterID,
            uint sourceID,
            WCS_DEVICE_CAPABILITIES_TYPE capsType,
            ref WCS_DEVICE_MHC2_CAPABILITIES outputCapabilities
        );

        private static readonly ColorProfileGetDeviceCapabilitiesDelegate ColorProfileGetDeviceCapabilities;

        static DisplayColorProfileManager()
        {
            IntPtr hModule = LoadLibrary("mscms.dll");
            if (hModule != IntPtr.Zero)
            {
                IntPtr proc = GetProcAddress(hModule, "ColorProfileGetDeviceCapabilities");
                if (proc != IntPtr.Zero)
                {
                    ColorProfileGetDeviceCapabilities = (ColorProfileGetDeviceCapabilitiesDelegate)Marshal.GetDelegateForFunctionPointer(proc, typeof(ColorProfileGetDeviceCapabilitiesDelegate));
                }
            }
        }

        public static void AddAssociation(Display display, string profileName, bool hdr)
        {
            int hr = ColorProfileAddDisplayAssociation(
                WcsProfileManagementScope.CurrentUser,
                profileName,
                display.SourceAdapterId,
                display.SourceId,
                false,
                hdr);

            if (hr != 0) Marshal.ThrowExceptionForHR(hr);
        }

        public static void RemoveAssociation(Display display, string profileName, bool hdr)
        {
            int hr = ColorProfileRemoveDisplayAssociation(
                WcsProfileManagementScope.CurrentUser,
                profileName,
                display.SourceAdapterId,
                display.SourceId,
                hdr);
        }
        public static string GetProfile(Display display, bool hdr)
        {
            IntPtr profileNamePtr;
            int hr = ColorProfileGetDisplayDefault(
                WcsProfileManagementScope.CurrentUser,
                display.SourceAdapterId,
                display.SourceId,
                COLORPROFILETYPE.CPT_ICC,
                hdr ? COLORPROFILESUBTYPE.CPST_EXTENDED_DISPLAY_COLOR_MODE : COLORPROFILESUBTYPE.CPST_STANDARD_DISPLAY_COLOR_MODE,
                out profileNamePtr);

            if (hr != 0)
            {
                if (Marshal.GetExceptionForHR(hr) is FileNotFoundException)
                {
                    return "";
                }

                Marshal.ThrowExceptionForHR(hr);
            }

            try
            {
                string profileName = null;
                if (profileNamePtr != IntPtr.Zero)
                {
                    profileName = Marshal.PtrToStringUni(profileNamePtr);
                }
                return profileName;
            }
            finally
            {
                LocalFree(profileNamePtr);
            }
        }

        public static void SetProfile(Display display, string profileName, bool hdr)
        {
            int hr = ColorProfileSetDisplayDefaultAssociation(
                WcsProfileManagementScope.CurrentUser,
                profileName,
                COLORPROFILETYPE.CPT_ICC,
                hdr ? COLORPROFILESUBTYPE.CPST_EXTENDED_DISPLAY_COLOR_MODE : COLORPROFILESUBTYPE.CPST_STANDARD_DISPLAY_COLOR_MODE,
                display.SourceAdapterId,
                display.SourceId);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public static WcsProfileManagementScope GetDisplayUserScope(Display display)
        {
            WcsProfileManagementScope scope;
            int hr = ColorProfileGetDisplayUserScope(
                display.SourceAdapterId,
                display.SourceId,
                out scope);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return scope;
        }

        public static void SetDisplayUserScope(Display display, WcsProfileManagementScope usePerUserProfiles)
        {
            WcsSetUsePerUserProfiles(
                display.GetDriver(),
                CLASS_MONITOR,
                usePerUserProfiles == WcsProfileManagementScope.CurrentUser
            );
        }

        public static bool? IsSupportMHC2(Display display)
        {
            if (ColorProfileGetDeviceCapabilities == null)
            {
                return null;
            }

            var outputCapabilities = new WCS_DEVICE_MHC2_CAPABILITIES();
            int hr = ColorProfileGetDeviceCapabilities(
                WcsProfileManagementScope.CurrentUser,
                display.SourceAdapterId,
                display.SourceId,
                WCS_DEVICE_CAPABILITIES_TYPE.MicrosoftHardwareColorV2,
                ref outputCapabilities);

            if (hr != 0)
            {
                return null;
            }

            return outputCapabilities.SupportsMhc2;
        }
    }
}
