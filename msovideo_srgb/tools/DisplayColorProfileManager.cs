using System;
using System.IO;
using System.Runtime.InteropServices;
using WindowsDisplayAPI;

namespace msovideo_srgb
{
    public static class DisplayColorProfileManager
    {
        internal enum WcsProfileManagementScope : uint
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

        [StructLayout(LayoutKind.Sequential)]
        internal struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct DXGI_ADAPTER_DESC1
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string Description;
            public uint VendorId;
            public uint DeviceId;
            public uint SubSysId;
            public uint Revision;
            public IntPtr DedicatedVideoMemory;
            public IntPtr DedicatedSystemMemory;
            public IntPtr SharedSystemMemory;
            public LUID AdapterLuid;
            public uint Flags;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct DXGI_OUTPUT_DESC
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            public RECT DesktopCoordinates;
            [MarshalAs(UnmanagedType.Bool)]
            public bool AttachedToDesktop;
            public int Rotation;
            public IntPtr Monitor;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [ComImport, Guid("770aae78-f26f-4dba-a829-253c83d1b387")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IDXGIFactory1
        {
            void SetPrivateData();
            void SetPrivateDataInterface();
            void GetPrivateData();
            void GetParent();

            [PreserveSig]
            int EnumAdapters1(uint adapterIndex, out IDXGIAdapter1 adapter);
        }

        [ComImport, Guid("29038f61-3839-4626-91fd-086879011a05")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IDXGIAdapter1
        {
            void SetPrivateData();
            void SetPrivateDataInterface();
            void GetPrivateData();
            void GetParent();

            [PreserveSig]
            int EnumOutputs(uint outputIndex, out IDXGIOutput output);

            [PreserveSig]
            int GetDesc1(out DXGI_ADAPTER_DESC1 desc);
        }

        [ComImport, Guid("ae02eedb-c735-4690-8d52-5a8dc20213aa")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IDXGIOutput
        {
            void SetPrivateData();
            void SetPrivateDataInterface();
            void GetPrivateData();
            void GetParent();

            [PreserveSig]
            int GetDesc(out DXGI_OUTPUT_DESC desc);
        }

        [DllImport("dxgi.dll", ExactSpelling = true)]
        private static extern int CreateDXGIFactory1(
            [In] ref Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out IDXGIFactory1 factory);

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


        public static void AddAssociation(Display display, string profileName, bool hdr)
        {
            var luidAndSource = FindAdapterAndSource(display.DisplayName);
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
            var luidAndSource = FindAdapterAndSource(display.DisplayName);
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
            var luidAndSource = FindAdapterAndSource(display.DisplayName);

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
            var luidAndSource = FindAdapterAndSource(display.DisplayName);

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

        private static Tuple<LUID, uint> FindAdapterAndSource(string deviceName)
        {
            Guid factoryGuid = typeof(IDXGIFactory1).GUID;
            int result = CreateDXGIFactory1(ref factoryGuid, out IDXGIFactory1 factory);
            if (result != 0) Marshal.ThrowExceptionForHR(result);

            uint adapterIndex = 0;
            while (factory.EnumAdapters1(adapterIndex, out IDXGIAdapter1 adapter) == 0)
            {
                adapter.GetDesc1(out DXGI_ADAPTER_DESC1 desc);
                uint outputIndex = 0;
                while (adapter.EnumOutputs(outputIndex, out IDXGIOutput output) == 0)
                {
                    output.GetDesc(out DXGI_OUTPUT_DESC outputDesc);
                    if (string.Equals(outputDesc.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase))
                    {
                        return Tuple.Create(desc.AdapterLuid, outputIndex);
                    }
                    outputIndex++;
                }
                adapterIndex++;
            }

            throw new InvalidOperationException("Display not found in DXGI enumeration.");
        }
    }

}
