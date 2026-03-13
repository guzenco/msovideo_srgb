using System;
using System.IO;
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

        private const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;

        [DllImport("user32.dll")]
        private static extern int GetDisplayConfigBufferSizes(
            uint flags,
            out uint numPathArrayElements,
            out uint numModeInfoArrayElements);

        [DllImport("user32.dll")]
        private static extern int QueryDisplayConfig(
            uint flags,
            ref uint numPathArrayElements,
            [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
            ref uint numModeInfoArrayElements,
            [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
            IntPtr currentTopologyId);

        [DllImport("user32.dll")]
        private static extern int DisplayConfigGetDeviceInfo(
            ref DISPLAYCONFIG_TARGET_DEVICE_NAME deviceName);

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
        public static extern bool WcsSetUsePerUserProfiles(
            string pDeviceName,
            uint dwDeviceClass,
            [MarshalAs(UnmanagedType.Bool)] bool usePerUserProfiles);

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

            if (hr != 0) Marshal.ThrowExceptionForHR(hr);
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

            if (Marshal.GetExceptionForHR(hr) is FileNotFoundException)
            {
                return "";
            }

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            string profileName = null;
            if (profileNamePtr != IntPtr.Zero)
            {
                profileName = Marshal.PtrToStringUni(profileNamePtr);
                Marshal.FreeCoTaskMem(profileNamePtr);
            }

            return profileName;
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

        private static Tuple<LUID, uint> FindAdapterAndSource(string devicePath)
        {
            GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out uint numPaths, out uint numModes);

            var paths = new DISPLAYCONFIG_PATH_INFO[numPaths];
            var modes = new DISPLAYCONFIG_MODE_INFO[numModes];

            QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref numPaths, paths, ref numModes, modes, IntPtr.Zero);

            foreach (var path in paths)
            {
                var source = path.sourceInfo;
                var target = path.targetInfo;

                var targetName = new DISPLAYCONFIG_TARGET_DEVICE_NAME
                {
                    header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                    {
                        type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                        size = Marshal.SizeOf(typeof(DISPLAYCONFIG_TARGET_DEVICE_NAME)),
                        adapterId = target.adapterId,
                        id = target.id
                    }
                };
               
                if (DisplayConfigGetDeviceInfo(ref targetName) == 0)
                {
                    if (string.Equals(targetName.monitorDevicePath, devicePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return Tuple.Create(source.adapterId, source.id);
                    }
                }
            }

            throw new InvalidOperationException("Display not found in DisplayConfig enumeration.");
        }
    }

}
