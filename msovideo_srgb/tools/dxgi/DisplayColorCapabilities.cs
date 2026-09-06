using System;
using System.Runtime.InteropServices;
namespace msovideo_srgb
{
    public class DisplayColorCapabilities
    {      
        [DllImport("dxgi.dll")]
        private static extern int CreateDXGIFactory1([In] ref Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out IDXGIFactory1 factory);

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
                Guid factoryGuid = typeof(IDXGIFactory1).GUID;
                int hr = CreateDXGIFactory1(ref factoryGuid, out IDXGIFactory1 factory);

                if (hr != 0 || factory == null) return null;

                uint adapterIndex = 0;
                IDXGIAdapter1 adapter;
                while (factory.EnumAdapters1(adapterIndex, out adapter) == 0)
                {
                    DXGI_ADAPTER_DESC1 adesc;
                    adapter.GetDesc1(out adesc);
                    if (adesc.AdapterLuid.Equals(display.SourceAdapterId))
                    {
                        uint outputIndex = 0;
                        IDXGIOutput output;
                        while (adapter.EnumOutputs(outputIndex, out output) == 0)
                        {
                            Guid output6Guid = typeof(IDXGIOutput6).GUID;
                            IntPtr output6Ptr;
                            hr = Marshal.QueryInterface(Marshal.GetIUnknownForObject(output), ref output6Guid, out output6Ptr);

                            if (hr == 0 && output6Ptr != IntPtr.Zero)
                            {
                                IDXGIOutput6 output6 = (IDXGIOutput6)Marshal.GetObjectForIUnknown(output6Ptr);
                                DXGI_OUTPUT_DESC1 desc1;
                                hr = output6.GetDesc1(out desc1);
                                if (hr == 0 && desc1.Base.DeviceName == display.SourceDeviceName)
                                {
                                    ColorCapabilities colorCapabilities = new ColorCapabilities()
                                    {
                                        PeakLuminance = desc1.MaxLuminance,
                                        MaxFullFrameLuminance = desc1.MaxFullFrameLuminance,
                                        MinLuminance = desc1.MinLuminance,
                                    };
                                    Marshal.ReleaseComObject(output);
                                    Marshal.ReleaseComObject(adapter);
                                    Marshal.ReleaseComObject(factory);
                                    return colorCapabilities;
                                }
                            }
                            Marshal.ReleaseComObject(output);
                            outputIndex++;
                        }
                    }
                    Marshal.ReleaseComObject(adapter);
                    adapterIndex++;
                }
                Marshal.ReleaseComObject(factory);
            }
            catch (Exception) { }
            return null;
        }
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
        public UIntPtr DedicatedVideoMemory;
        public UIntPtr DedicatedSystemMemory;
        public UIntPtr SharedSystemMemory;
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
        public uint Rotation;
        public IntPtr Monitor;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DXGI_OUTPUT_DESC1
    {
        public DXGI_OUTPUT_DESC Base;
        public uint BitsPerColor;
        public uint ColorSpace;
        public float RedPrimaryX;
        public float RedPrimaryY;
        public float GreenPrimaryX;
        public float GreenPrimaryY;
        public float BluePrimaryX;
        public float BluePrimaryY;
        public float WhitePointX;
        public float WhitePointY;
        public float MinLuminance;
        public float MaxLuminance;
        public float MaxFullFrameLuminance;
    }

    [ComImport, Guid("770aae78-f26f-4dba-a829-253c83d1b387"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDXGIFactory1
    {
        [PreserveSig] int SetPrivateData();
        [PreserveSig] int SetPrivateDataInterface();
        [PreserveSig] int GetPrivateData();
        [PreserveSig] int GetParent();
        [PreserveSig] int EnumAdapters();
        [PreserveSig] int MakeWindowAssociation();
        [PreserveSig] int GetWindowAssociation();
        [PreserveSig] int CreateSwapChain();
        [PreserveSig] int CreateSoftwareAdapter();

        [PreserveSig] int EnumAdapters1(uint Adapter, [MarshalAs(UnmanagedType.Interface)] out IDXGIAdapter1 ppAdapter);
    }

    [ComImport, Guid("29038f61-3839-4626-91fd-086879011a05"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDXGIAdapter1
    {
        [PreserveSig] int SetPrivateData();
        [PreserveSig] int SetPrivateDataInterface();
        [PreserveSig] int GetPrivateData();
        [PreserveSig] int GetParent();

        [PreserveSig] int EnumOutputs(uint Output, [MarshalAs(UnmanagedType.Interface)] out IDXGIOutput ppOutput);

        [PreserveSig] int GetDesc();
        [PreserveSig] int CheckInterfaceSupport();

        [PreserveSig] int GetDesc1(out DXGI_ADAPTER_DESC1 desc);
    }

    [ComImport, Guid("ae02eedb-c735-4690-8d52-5a8dc20213aa"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDXGIOutput
    {
        [PreserveSig] int SetPrivateData();
        [PreserveSig] int SetPrivateDataInterface();
        [PreserveSig] int GetPrivateData();
        [PreserveSig] int GetParent();

        [PreserveSig] int GetDesc(out DXGI_OUTPUT_DESC pDesc);
    }

    [ComImport, Guid("068346e8-aaec-4b84-add7-137f513f77a1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDXGIOutput6
    {
        [PreserveSig] int SetPrivateData();
        [PreserveSig] int SetPrivateDataInterface();
        [PreserveSig] int GetPrivateData();
        [PreserveSig] int GetParent();
        [PreserveSig] int GetDesc();
        [PreserveSig] int GetDisplayModeList();
        [PreserveSig] int FindClosestMatchingMode();
        [PreserveSig] int WaitForVBlank();
        [PreserveSig] int TakeOwnership();
        [PreserveSig] int ReleaseOwnership();
        [PreserveSig] int GetGammaControlCapabilities();
        [PreserveSig] int SetGammaControl();
        [PreserveSig] int GetGammaControl();
        [PreserveSig] int SetDisplaySurface();
        [PreserveSig] int GetDisplaySurfaceData();
        [PreserveSig] int GetFrameStatistics();
        [PreserveSig] int GetDisplayModeList1();
        [PreserveSig] int FindClosestMatchingMode1();
        [PreserveSig] int GetDisplaySurfaceData1();
        [PreserveSig] int DuplicateOutput();
        [PreserveSig] int SupportsOverlays();
        [PreserveSig] int CheckOverlaySupport();
        [PreserveSig] int heckOverlayColorSpaceSuppor();
        [PreserveSig] int DuplicateOutput1();

        [PreserveSig] int GetDesc1(out DXGI_OUTPUT_DESC1 pDesc1);
    }
}
