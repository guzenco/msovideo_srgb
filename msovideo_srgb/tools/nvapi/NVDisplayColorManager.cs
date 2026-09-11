using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace msovideo_srgb
{
    public static class NVDisplayColorManager
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr NvAPI_QueryInterfaceDelegate(uint id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_InitializeDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GPU_GetColorSpaceConversionDelegate(
            uint displayId,
            ref Csc csc
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GPU_SetColorSpaceConversionDelegate(
            uint displayId,
            ref Csc csc
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GPU_GetDitherControlDelegate(
            uint displayId,
            ref Dither dither
         );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GPU_SetDitherControlDelegate(
            ulong gpuHandle,
            uint outputId,
            int state,
            int bits,
            int mode
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_EnumPhysicalGPUsDelegate(
            ulong[] gpuHandles,
            ref uint gpuCount
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GPU_GetConnectedDisplayIdsDelegate(
            ulong gpuHandles,
            [In, Out] NV_GPU_DISPLAYIDS[] displayIds,
            ref uint displayCount,
            uint flag
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_Disp_GetDisplayIdInfoDelegate(
            uint displayId,
            ref NV_DISPLAY_ID_INFO_DATA infoData
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_SYS_GetGpuAndOutputIdFromDisplayIdDelegate(
            uint displayId,
            ref ulong gpuHandle,
            ref uint outputId
        );

        private static NvAPI_QueryInterfaceDelegate NvAPI_QueryInterface;

        #pragma warning disable CS0649

        [ExternalFunction(0x0150E828)]
        private static NvAPI_InitializeDelegate NvAPI_Initialize;

        [ExternalFunction(0x8159E87A)]
        private static readonly NvAPI_GPU_GetColorSpaceConversionDelegate NvAPI_GPU_GetColorSpaceConversion;

        [ExternalFunction(0xFCABD23A)]
        private static readonly NvAPI_GPU_SetColorSpaceConversionDelegate NvAPI_GPU_SetColorSpaceConversion;

        [ExternalFunction(0x932AC8FB)]
        private static readonly NvAPI_GPU_GetDitherControlDelegate NvAPI_GPU_GetDitherControl;

        [ExternalFunction(0xDF0DFCDD)]
        private static readonly NvAPI_GPU_SetDitherControlDelegate NvAPI_GPU_SetDitherControl;

        [ExternalFunction(0xE5AC921F)]
        private static readonly NvAPI_EnumPhysicalGPUsDelegate NvAPI_EnumPhysicalGPUs;

        [ExternalFunction(0x0078DBA2)]
        private static readonly NvAPI_GPU_GetConnectedDisplayIdsDelegate NvAPI_GPU_GetConnectedDisplayIds;

        [ExternalFunction(0xBAE8AA5E)]
        private static readonly NvAPI_Disp_GetDisplayIdInfoDelegate NvAPI_Disp_GetDisplayIdInfo;

        [ExternalFunction(0x112BA1A5)]
        private static readonly NvAPI_SYS_GetGpuAndOutputIdFromDisplayIdDelegate NvAPI_SYS_GetGpuAndOutputIdFromDisplayId;

        #pragma warning restore CS0649

        static NVDisplayColorManager()
        {
            IntPtr hModule = LoadLibrary("nvapi64.dll");
            if (hModule != IntPtr.Zero)
            {
                IntPtr proc = GetProcAddress(hModule, "nvapi_QueryInterface");
                if (proc != IntPtr.Zero)
                {
                    NvAPI_QueryInterface = (NvAPI_QueryInterfaceDelegate)Marshal.GetDelegateForFunctionPointer(proc, typeof(NvAPI_QueryInterfaceDelegate));
                }
            }

            if (NvAPI_QueryInterface != null)
            {
                foreach (var field in typeof(NVDisplayColorManager).GetFields(BindingFlags.Static | BindingFlags.NonPublic))
                {
                    var externalFunction = field.GetCustomAttribute<ExternalFunctionAttribute>();
                    if (externalFunction == null) continue;

                    IntPtr functionPtr = NvAPI_QueryInterface(externalFunction.Id);
                    if (functionPtr == IntPtr.Zero) continue;

                    var function = Marshal.GetDelegateForFunctionPointer(functionPtr, field.FieldType);
                    field.SetValue(null, function);
                }
            }

            if (NvAPI_Initialize != null)
            {
                int status = NvAPI_Initialize();

                if (status == 0)
                {
                    Initialized = true;
                }
            }
        }

        public static bool Initialized { get; }

        public static uint? GetDisplayId(LUID targetAdapterId, uint targetId)
        {
            ulong[] gpuHandles = new ulong[64];
            uint gpuCount = 0;

            int status = NvAPI_EnumPhysicalGPUs(gpuHandles, ref gpuCount);
            if (status != 0) return null;

            for (int i = 0; i < gpuCount; i++)
            {
                NV_GPU_DISPLAYIDS[] displayIds = new NV_GPU_DISPLAYIDS[128];
                for (int j = 0; j < displayIds.Length; j++)
                {
                    displayIds[j].version = 0x30010;
                }
                uint displayCount = 128;

                status = NvAPI_GPU_GetConnectedDisplayIds(gpuHandles[i], displayIds, ref displayCount, 0);
                if (status != 0) continue;

                for (int j = 0; j < displayCount; j++)
                {

                    NV_DISPLAY_ID_INFO_DATA infoData = new NV_DISPLAY_ID_INFO_DATA
                    {
                        version = 0x10020,
                    };

                    status = NvAPI_Disp_GetDisplayIdInfo(displayIds[j].displayId, ref infoData);
                    if (status != 0) continue;

                    if (infoData.adapterId.Equals(targetAdapterId) && (infoData.targetId & 0xFFFF) == (targetId & 0xFFFF))
                    {
                        return displayIds[j].displayId;
                    }
                }
            }
            return null;
        }

        public static bool IsColorSpaceConversionActive(uint displayId)
        {
            var csc = GetColorSpaceConversion(displayId);

            return csc.useMatrix1 != 0 || csc.useMatrix2 != 0;
        }

        public static void ClearColorSpaceConversion(uint displayId, bool matrixOnly)
        {
            var activeCsc = GetColorSpaceConversion(displayId);

            var csc = new Csc
            {
                version = 0x1007C,
                contentColorSpace = matrixOnly ? activeCsc.contentColorSpace : 2,
                monitorColorSpace = matrixOnly ? activeCsc.monitorColorSpace : 0,
            };

            SetColorSpaceConversion(displayId, csc);
        }

        public static void SetColorspaceConversion(uint displayId, Calibration calibration)
        {
            if (calibration.DeGamma == null && calibration.RGBGains.DifferenceMax(Matrix.One3x1()) < 1E-10)
            {
                SetColorspaceConversionV1(displayId, calibration);
            }
            else
            {
                SetColorspaceConversionV2(displayId, calibration);
            }
        }

        private static unsafe void SetColorspaceConversionV1(uint displayId, Calibration calibration)
        {
            var csc = new Csc
            {
                version = 0x1007C,
                contentColorSpace = 2,
                monitorColorSpace = 2,
            };

            Matrix matrix = calibration.MatrixRGBToRGB;

            csc.useMatrix1 = 1;
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    csc.matrix1[i * 4 + j] = (float)matrix[i, j];
                }
            }

            SetColorSpaceConversion(displayId, csc);
        }

        private static unsafe void SetColorspaceConversionV2(uint displayId, Calibration calibration)
        {

            var gamma = new float[2, 1024, 3];
            fixed (float* buffer = gamma)
            {
                var csc = new Csc
                {
                    version = 0x200A0,
                    contentColorSpace = 2,
                    monitorColorSpace = 2,
                    degamma = buffer,
                    regamma = buffer + 0x3000 / sizeof(float),
                    buffer = buffer,
                    bufferSize = 0x6000,
                };

                csc.useMatrix1 = 1;
                for (var i = 0; i < 3; i++)
                {
                    for (var j = 0; j < 3; j++)
                    {
                        csc.matrix1[i * 4 + j] = (float)calibration.MatrixRGBToRGB[i, j];
                    }
                }

                Func<int, double, float> deGamma;
                Func<int, double, float> reGamma;
                if (calibration.DeGamma != null)
                {
                    deGamma = (j, v) => (float)calibration.DeGamma[j].SampleAt(v);
                    reGamma = (j, v) => (float)calibration.ReGamma[j].SampleAt(v);
                }
                else
                {
                    var srgb = new SrgbEOTF();
                    deGamma = (j, v) => (float)srgb.SampleAt(v);
                    reGamma = (j, v) => (float)(srgb.SampleInverseAt(v) * calibration.ReGamma[j].SampleAt(calibration.RGBGains[j]));
                }

                for (var i = 1; i < 1024; i++)
                {
                    var value = i / 1023d;
                    for (var j = 0; j < 3; j++)
                    {
                        gamma[0, i, j] = deGamma(j, value);
                        gamma[1, i, j] = reGamma(j, value);
                    }
                }
                
                SetColorSpaceConversion(displayId, csc);
            }
        }

        private static readonly string[] DitheringStateNames = { "Default", "Enable", "Disable" };
        private static readonly string[] DitheringBitsNames = { "6 bit", "8 bit", "10 bit", "12 bit" };
        private static readonly string[] DitheringModeNames = { "SpatialDynamic", "SpatialStatic", "SpatialDynamic2x2", "SpatialStatic2x2", "Temporal", "Round" };

        public static Dithering GetDithering(uint displayId)
        {
            Dither dither = new Dither
            {
                version = 0x10018,
            };

            int status = NvAPI_GPU_GetDitherControl(displayId, ref dither);

            if (status != 0)
            {
                return null;
            }

            Dithering dithering = new Dithering(
                dither.state,
                IsBitSet(dither.bitsCaps, dither.bits) ? CountSetBitsBeforeIndex(dither.bitsCaps, dither.bits) : -1,
                IsBitSet(dither.modeCaps, dither.mode) ? CountSetBitsBeforeIndex(dither.modeCaps, dither.mode) : -1,
                DitheringStateNames,
                dither.state == 1 ? GetNamesFromMask(dither.bitsCaps, DitheringBitsNames) : new string[0],
                dither.state == 1 ? GetNamesFromMask(dither.modeCaps, DitheringModeNames) : new string[0]
            );

            return dithering;
        }

        public static void SetDithering(uint displayId, Dithering dithering)
        {
            Dither dither = new Dither
            {
                version = 0x10018,
            };

            int status = NvAPI_GPU_GetDitherControl(displayId, ref dither);

            if (status != 0)
            {
                return;
            }

            ulong gpuHandle = 0;
            uint outputId = 0;

            status = NvAPI_SYS_GetGpuAndOutputIdFromDisplayId(displayId, ref gpuHandle, ref outputId);

            if (status != 0)
            {
                return;
            }

            int bits = GetIndexOfNthSetBit(dither.bitsCaps, dithering.Bits);
            bits = bits >= 0 ? bits : dither.bits;
            int mode = GetIndexOfNthSetBit(dither.modeCaps, dithering.Mode);
            mode = mode >= 0 ? mode : dither.mode;

            NvAPI_GPU_SetDitherControl(gpuHandle, outputId, dithering.State, bits, mode);
        }

        private static Csc GetColorSpaceConversion(uint displayId, uint version = 0x1007C)
        {
            Csc csc = new Csc()
            {
                version = version
            };

            int status = NvAPI_GPU_GetColorSpaceConversion(displayId, ref csc);

            if (status != 0)
            {
                throw new ExternalAPIException(nameof(NvAPI_GPU_GetColorSpaceConversion), status);
            }

            return csc;
        }

        private static void SetColorSpaceConversion(uint displayId, Csc csc)
        {
            int status = NvAPI_GPU_SetColorSpaceConversion(displayId, ref csc);

            if (status != 0)
            {
                throw new ExternalAPIException(nameof(NvAPI_GPU_SetColorSpaceConversion), status);
            }
        }

        private static string[] GetNamesFromMask(uint mask, string[] knownNames)
        {
            if (mask == 0) return new string[0];

            List<string> names = new List<string>();
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    if (i < knownNames.Length)
                    {
                        names.Add(knownNames[i]);
                    }
                    else
                    {
                        names.Add($"Unknown {i}");
                    }
                }
            }

            return names.ToArray();
        }

        private static bool IsBitSet(uint value, int n)
        {
            return (value & (1u << n)) != 0;
        }

        private static int CountSetBitsBeforeIndex(uint value, int n)
        {
            if (n < 0 || value == 0) return 0;         
            n = Math.Min(n, 32);

            int count = 0;
            for (int bit = 0; bit < n; bit++)
            {
                if (IsBitSet(value, bit))
                {
                    count++;
                }
            }

            return count;
        }

        private static int GetIndexOfNthSetBit(uint value, int n)
        {
            if (n < 0 || n >= 32 || value == 0) return -1;

            int count = 0;
            for (int bit = 0; bit < 32; bit++)
            {
                if (IsBitSet(value, bit))
                {
                    if (count == n)
                    {
                        return bit;
                    }
                    count++;
                }
            }

            return -1;
        }
    }

    /*
    observed pipeline: content degamma -> content to srgb -> srgb to monitor -> matrix2 -> matrix1 -> monitor gamma
    in hardware all the matrix stuff is done with a single matrix, i.e. these four multiplied together
    3x4 matrices cannot be multiplied with each other though, so only the 3x3 parts are combined "properly"
    and the offsets are simply added together
    */
    [StructLayout(LayoutKind.Sequential)]
    internal struct Csc
    {
        public uint version; // 0x1007C for V1, 0x200A0 for V2, 0x300B0 for v3

        public uint contentColorSpace; // built-in degamut/degamma transforms, 1 <= x <= 12, default 2 (probably srgb)

        public uint monitorColorSpace; // built-in gamut/gamma transforms, 0 <= x <= 12, default 0 (= csc disabled)
        public uint unknown1; // no idea, set to 0 by both get and set functions -> some type of error code?
        public uint unknown2; // also no idea, not modified by either function -> unused?
        public uint useMatrix1; // 1 to enable
        public unsafe fixed float matrix1[3 * 4]; // r/g/b gain and offset
        public uint useMatrix2;
        public unsafe fixed float matrix2[3 * 4];

        // v2 stuff
        public unsafe float* degamma; // pointer to degamma part of buffer (= first element)
        public unsafe float* regamma; // pointer to regamma part of buffer (= index 0x3000)

        public unsafe float* buffer; // float array of size 0x6000, contains interleaved rgb degamma followed by regamma

        public int bufferSize; // 0x6000

        // v3 stuff
        public uint unknown3;
        public uint unknown4;
        public uint unknown5;
        public uint unknown6;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Dither
    {
        public uint version; // 0x10018
        public int state;
        public int bits;
        public int mode;
        public uint bitsCaps;
        public uint modeCaps;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NV_GPU_DISPLAYIDS
    {
        public uint version; // 0x30010
        public uint connectorType;
        public uint displayId;

        private uint flags;

        public bool IsDynamic => (flags & (1u << 0)) != 0;
        public bool IsMultiStreamRootNode => (flags & (1u << 1)) != 0;
        public bool IsActive => (flags & (1u << 2)) != 0;
        public bool IsCluster => (flags & (1u << 3)) != 0;
        public bool IsOSVisible => (flags & (1u << 4)) != 0;
        public bool IsWFD => (flags & (1u << 5)) != 0;
        public bool IsConnected => (flags & (1u << 6)) != 0;
        public bool IsPhysicallyConnected => (flags & (1u << 17)) != 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NV_DISPLAY_ID_INFO_DATA
    {
        public uint version; // 0x10020
        public LUID adapterId;
        public uint targetId;

        public uint reserved1;
        public uint reserved2;
        public uint reserved3;
        public uint reserved4;
    }
}
