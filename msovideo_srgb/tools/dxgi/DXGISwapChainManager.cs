using System.Runtime.InteropServices;
using System;

namespace msovideo_srgb
{
    public static class DXGISwapChainManager
    {
        public const ulong WS_POPUP = 0x80000000;

        public const uint WS_EX_LAYERED = 0x00080000;
        public const uint WS_EX_TOPMOST = 0x00000008;
        public const uint WS_EX_TRANSPARENT = 0x00000020;
        public const uint WS_EX_TOOLWINDOW = 0x00000080;

        public const int SW_SHOW = 5;
        public const uint LWA_ALPHA = 0x00000002;

        public const uint DXGI_USAGE_RENDER_TARGET_OUTPUT = 0x00000020;
        public const uint DXGI_FORMAT_R8G8B8A8_UNORM = 28;
        public const uint DXGI_SWAP_EFFECT_FLIP_DISCARD = 4;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowEx(
            uint dwExStyle,
            string lpClassName,
            string lpWindowName,
            ulong dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam
        );

        [DllImport("user32.dll")]
        static extern bool DestroyWindow(
            IntPtr hWnd
        );

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(
            IntPtr hWnd,
            int nCmdShow
        );

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(
            IntPtr hwnd,
            ulong crKey,
            byte bAlpha,
            ulong dwFlags
        );

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern IntPtr SetFocus(
            IntPtr hWnd
        );

        [DllImport("dxgi.dll")]
        private static extern int CreateDXGIFactory1(
            [In] ref Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out IDXGIFactory1 factory
        );

        [DllImport("d3d11.dll", ExactSpelling = true)]
        private static extern int D3D11CreateDeviceAndSwapChain(
            [MarshalAs(UnmanagedType.Interface)] IDXGIAdapter1 adapter,
            uint driverType,
            IntPtr software,
            uint flags,
            IntPtr featureLevels,
            uint featureLevelsCount,
            uint sdkVersion,
            ref DXGI_SWAP_CHAIN_DESC swapChainDesc,
            out IDXGISwapChain swapChain,
            out ID3D11Device device,
            out uint featureLevel,
            out ID3D11DeviceContext context
        );

        public static void DisableFlipOptimizationForMoment(Display display)
        {
            var desc1 = GetDesc1(display);
            if (desc1 == null) return;

            var coord = desc1.Value.Base.DesktopCoordinates;
            var hwnd = CreateWindow(coord.left, coord.top);

            if (hwnd != IntPtr.Zero)
            {
                var swapChain = CreateSwapChain(display, hwnd);

                if (swapChain != null)
                {
                    swapChain.Present(0, 0);
                    Marshal.ReleaseComObject(swapChain);
                }

                DestroyWindow(hwnd);
            }
        }

        private static IntPtr CreateWindow(int x, int y)
        {
            IntPtr hwnd = CreateWindowEx(
                WS_EX_LAYERED | WS_EX_TOPMOST | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW,
                "STATIC",
                "",
                WS_POPUP,
                x, y, 1, 1,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero
            );

            SetLayeredWindowAttributes(hwnd, 0, 0, LWA_ALPHA);
            ShowWindow(hwnd, SW_SHOW);

            return hwnd;
        }

        private static IDXGISwapChain CreateSwapChain(Display display, IntPtr hwnd)
        {
            IDXGIAdapter1 adapter = GetAdapter(display);
            if (adapter == null) return null;

            DXGI_SWAP_CHAIN_DESC desc = new DXGI_SWAP_CHAIN_DESC
            {
                BufferCount = 2,
                BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT,
                OutputWindow = hwnd,
                BufferDesc = new DXGI_MODE_DESC
                {
                    Width = 1,
                    Height = 1,
                    Format = DXGI_FORMAT_R8G8B8A8_UNORM,
                },
                SampleDesc = new DXGI_SAMPLE_DESC
                {
                    Count = 1,
                    Quality = 0,
                },
                Windowed = true,
                SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD,
            };

            IDXGISwapChain swapChain;
            ID3D11Device d3dDevice;
            ID3D11DeviceContext context;

            int hr = D3D11CreateDeviceAndSwapChain(adapter, 0, IntPtr.Zero, 0, IntPtr.Zero, 0, 7, ref desc, out swapChain, out d3dDevice, out _, out context);
            Marshal.ReleaseComObject(adapter);

            if (hr != 0)
            {
                Marshal.ReleaseComObject(d3dDevice);
                Marshal.ReleaseComObject(context);
            }

            return swapChain;
        }

        internal static DXGI_OUTPUT_DESC1? GetDesc1(Display display)
        {
            IDXGIAdapter1 adapter = GetAdapter(display);
            if (adapter == null) return null;

            try
            {
                uint outputIndex = 0;
                IDXGIOutput output;
                while (adapter.EnumOutputs(outputIndex, out output) == 0)
                {
                    Guid output6Guid = typeof(IDXGIOutput6).GUID;
                    IntPtr output6Ptr;
                    int hr = Marshal.QueryInterface(Marshal.GetIUnknownForObject(output), ref output6Guid, out output6Ptr);
                    Marshal.ReleaseComObject(output);

                    if (hr == 0 && output6Ptr != IntPtr.Zero)
                    {
                        IDXGIOutput6 output6 = (IDXGIOutput6)Marshal.GetObjectForIUnknown(output6Ptr);
                        DXGI_OUTPUT_DESC1 desc1;
                        hr = output6.GetDesc1(out desc1);
                        Marshal.ReleaseComObject(output6);
                        Marshal.Release(output6Ptr);

                        if (hr == 0 && desc1.Base.DeviceName == display.SourceDeviceName)
                        {
                            return desc1;
                        }
                    }
                    outputIndex++;
                }
            }
            finally
            {
                Marshal.ReleaseComObject(adapter);
            }

            return null;
        }

        internal static IDXGIAdapter1 GetAdapter(Display display)
        {
            Guid factoryGuid = typeof(IDXGIFactory1).GUID;
            int hr = CreateDXGIFactory1(ref factoryGuid, out IDXGIFactory1 factory);
            if (hr != 0 || factory == null) return null;

            try
            {
                uint adapterIndex = 0;
                IDXGIAdapter1 adapter;
                while (factory.EnumAdapters1(adapterIndex, out adapter) == 0)
                {
                    DXGI_ADAPTER_DESC1 adesc;
                    adapter.GetDesc1(out adesc);
                    if (adesc.AdapterLuid.Equals(display.SourceAdapterId))
                    {
                        return adapter;
                    }
                    Marshal.ReleaseComObject(adapter);
                    adapterIndex++;
                }
            }
            finally
            {
                Marshal.ReleaseComObject(factory);
            }

            return null;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WNDCLASS
    {
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DXGI_MODE_DESC
    {
        public uint Width;
        public uint Height;
        public DXGI_RATIONAL RefreshRate;
        public uint Format;
        public uint ScanlineOrdering;
        public uint Scaling;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DXGI_RATIONAL
    {
        public int Numerator;
        public int Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DXGI_SAMPLE_DESC
    {
        public int Count;
        public int Quality;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DXGI_SWAP_CHAIN_DESC
    {
        public DXGI_MODE_DESC BufferDesc;
        public DXGI_SAMPLE_DESC SampleDesc;
        public uint BufferUsage;
        public uint BufferCount;
        public IntPtr OutputWindow;
        [MarshalAs(UnmanagedType.Bool)] public bool Windowed;
        public uint SwapEffect;
        public uint Flags;
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

    [ComImport, Guid("310d36a0-d2e7-4c0a-aa04-6a9d23b8886a"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDXGISwapChain
    {
        [PreserveSig] int SetPrivateData();
        [PreserveSig] int SetPrivateDataInterface();
        [PreserveSig] int GetPrivateData();
        [PreserveSig] int GetParent();
        [PreserveSig] int GetDevice();

        [PreserveSig] int Present(uint syncInterval, uint flags);
    }

    [ComImport, Guid("db6f6ddb-ac77-4e88-8253-819df9bbf140"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ID3D11Device { }

    [ComImport, Guid("c0bfa96c-e089-44fb-8eaf-26f8796190da"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ID3D11DeviceContext { }

}
