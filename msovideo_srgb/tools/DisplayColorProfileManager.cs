using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using WindowsDisplayAPI;

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

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);

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

        [DllImport("mscms.dll", CharSet = CharSet.Unicode)]
        private static extern int ColorProfileGetDisplayList(
            WcsProfileManagementScope scope,
            LUID targetAdapterID,
            uint sourceID,
            out IntPtr profileList,
            out uint profileCount);

        public static void AddAssociation(Display display, string profileName, bool hdr)
        {
            var luidAndSource = FindAdapterAndSource(display.DevicePath);
            int hr = ColorProfileAddDisplayAssociation(
                WcsProfileManagementScope.CurrentUser,
                profileName,
                luidAndSource.Item1,
                luidAndSource.Item2,
                false,
                hdr);

            if (hr != 0) Marshal.ThrowExceptionForHR(hr);
        }

        public static void RemoveAssociation(Display display, string profileName, bool hdr)
        {
            var luidAndSource = FindAdapterAndSource(display.DevicePath);
            int hr = ColorProfileRemoveDisplayAssociation(
                WcsProfileManagementScope.CurrentUser,
                profileName,
                luidAndSource.Item1,
                luidAndSource.Item2,
                hdr);
        }
        public static string GetProfile(Display display, bool hdr)
        {
            var luidAndSource = FindAdapterAndSource(display.DevicePath);

            IntPtr profileNamePtr;
            int hr = ColorProfileGetDisplayDefault(
                WcsProfileManagementScope.CurrentUser,
                luidAndSource.Item1,
                luidAndSource.Item2,
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

        public static void SetProfile(Display display, string profilePath, bool hdr)
        {
            var luidAndSource = FindAdapterAndSource(display.DevicePath);

            int hr = ColorProfileSetDisplayDefaultAssociation(
                WcsProfileManagementScope.CurrentUser,
                profilePath,
                COLORPROFILETYPE.CPT_ICC,
                hdr ? COLORPROFILESUBTYPE.CPST_EXTENDED_DISPLAY_COLOR_MODE : COLORPROFILESUBTYPE.CPST_STANDARD_DISPLAY_COLOR_MODE,
                luidAndSource.Item1,
                luidAndSource.Item2);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public static WcsProfileManagementScope GetDisplayUserScope(Display display)
        {
            var luidAndSource = FindAdapterAndSource(display.DevicePath);

            WcsProfileManagementScope scope;
            int hr = ColorProfileGetDisplayUserScope(
                luidAndSource.Item1,
                luidAndSource.Item2,
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
                display.DeviceKey,
                CLASS_MONITOR,
                usePerUserProfiles == WcsProfileManagementScope.CurrentUser
            );
        }

        public static string[] GetDisplayProfiles(Display display)
        {
            var luidAndSource = FindAdapterAndSource(display.DevicePath);

            IntPtr profileListPtr;
            uint profileCount;
            int hr = ColorProfileGetDisplayList(
                WcsProfileManagementScope.CurrentUser,
                luidAndSource.Item1,
                luidAndSource.Item2,
                out profileListPtr,
                out profileCount);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            var profileNames = new string[profileCount];
            try
            {
                if (profileCount > 0) {
                    IntPtr[] ptrs = new IntPtr[profileCount];
                    Marshal.Copy(profileListPtr, ptrs, 0, (int)profileCount);

                    for (int i = 0; i < profileCount; i++)
                    {
                        string profileName = Marshal.PtrToStringUni(ptrs[i]);
                        profileNames[i] = profileName;
                    }
                }
            }
            finally
            {
                LocalFree(profileListPtr);
            }
            
            return profileNames;
        }

        public static bool IsDisplaySourceIdUnique(Display display)
        {
            var adapterAndSourceIds = DisplayConfigManager.FindAdapterAndSourceIds();

            if (adapterAndSourceIds.ContainsKey(display.DevicePath))
            {
                var adapterAndSource = adapterAndSourceIds[display.DevicePath];
                var counts = adapterAndSourceIds.Values.GroupBy(v => v).ToDictionary(g => g.Key, g => g.Count());
                return counts[adapterAndSource] == 1;
            }

            return false;
        }

        internal static Tuple<LUID, uint> FindAdapterAndSource(string devicePath)
        {
            var adapterAndSourceIds = DisplayConfigManager.FindAdapterAndSourceIds();

            if (adapterAndSourceIds.ContainsKey(devicePath))
            { 
                return adapterAndSourceIds[devicePath];
            }

            throw new DisplayNotFoundException("Display not found in DisplayConfig enumeration.");
        }
    }

}
