using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace msovideo_srgb
{
    public static class ADLDisplayColorManager
    {
        private const int ADL_DISPLAY_DISPLAYINFO_DISPLAYCONNECTED = 0x00000001;
        private const int ADL_DISPLAY_DISPLAYINFO_DISPLAYMAPPED = 0x00000002;

        private const int ADL_GAMUT_SPACE_CIE_RGB = (1 << 3);
        private const int ADL_WHITE_POINT_6500K = (1 << 1);
        private const int ADL_EDID_REGAMMA_PREDEFINED_SRGB = (1 << 1);

        private const int ADL_GAMUT_REFERENCE_DESTINATION = 0;
        private const int ADL_GAMUT_REFERENCE_SOURCE = (1 << 0);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        private delegate IntPtr MallocCallback(
            int size
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ADL2_Main_Control_CreateDelegate(
            MallocCallback callback,
            int iEnumConnectedAdapters,
            out IntPtr context
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ADL2_Main_Control_DestroyDelegate(
            IntPtr context
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ADL2_Adapter_NumberOfAdapters_GetDelegate(
            IntPtr context,
            out int numAdapters
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ADL2_Adapter_AdapterInfo_GetDelegate(
            IntPtr context,
            [Out] AdapterInfo[] adapterInfos,
            int inputSize
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ADL2_Adapter_Active_GetDelegate(
            IntPtr context,
            int adapterIndex,
            [MarshalAs(UnmanagedType.Bool)] out bool status
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ADL2_Display_DisplayInfo_GetDelegate(
            IntPtr context,
            int adapterIndex,
            out int numDisplays,
            out IntPtr displayInfos,
            int forceDetect
        );

        private delegate int ADL2_Display_EdidData_GetDelegate(
            IntPtr context,
            int adapterIndex,
            int displayIndex,
            ref EDIDData edidData
        );

        private delegate int ADL2_Display_DitherState_GetDelegate(
            IntPtr context,
            int adapterIndex,
            int displayIndex,
            out int ditherState
        );

        private delegate int ADL2_Display_DitherState_SetDelegate(
            IntPtr context,
            int adapterIndex,
            int displayIndex,
            int ditherState
        );

        private delegate int ADL2_Display_Gamut_GetDelegate(
            IntPtr context,
            int adapterIndex,
            int displayIndex,
            int gamutRef,
            out GamutData gamutData
        );

        private delegate int ADL2_Display_Gamut_SetDelegate(
            IntPtr context,
            int adapterIndex,
            int displayIndex,
            int gamutRef,
            ref GamutData gamutData
        );

        private delegate int ADL2_Display_RegammaR1_GetDelegate(
            IntPtr context,
            int adapterIndex,
            int displayIndex,
            out RegammaEx regamma
        );

        private delegate int ADL2_Display_RegammaR1_SetDelegate(
            IntPtr context,
            int adapterIndex,
            int displayIndex,
            ref RegammaEx regamma
        );

#pragma warning disable CS0649

        [ExternalFunction]
        private static ADL2_Main_Control_CreateDelegate ADL2_Main_Control_Create;

        [ExternalFunction]
        private static ADL2_Main_Control_DestroyDelegate ADL2_Main_Control_Destroy;

        [ExternalFunction]
        private static ADL2_Adapter_NumberOfAdapters_GetDelegate ADL2_Adapter_NumberOfAdapters_Get;

        [ExternalFunction]
        private static ADL2_Adapter_AdapterInfo_GetDelegate ADL2_Adapter_AdapterInfo_Get;

        [ExternalFunction]
        private static ADL2_Adapter_Active_GetDelegate ADL2_Adapter_Active_Get;

        [ExternalFunction]
        private static ADL2_Display_DisplayInfo_GetDelegate ADL2_Display_DisplayInfo_Get;

        [ExternalFunction]
        private static ADL2_Display_EdidData_GetDelegate ADL2_Display_EdidData_Get;

        [ExternalFunction]
        private static ADL2_Display_DitherState_GetDelegate ADL2_Display_DitherState_Get;

        [ExternalFunction]
        private static ADL2_Display_DitherState_SetDelegate ADL2_Display_DitherState_Set;

        [ExternalFunction]
        private static ADL2_Display_Gamut_GetDelegate ADL2_Display_Gamut_Get;

        [ExternalFunction]
        private static ADL2_Display_Gamut_SetDelegate ADL2_Display_Gamut_Set;

        [ExternalFunction]
        private static ADL2_Display_RegammaR1_GetDelegate ADL2_Display_RegammaR1_Get;

        [ExternalFunction]
        private static ADL2_Display_RegammaR1_SetDelegate ADL2_Display_RegammaR1_Set;

#pragma warning restore CS0649

        static ADLDisplayColorManager()
        {
            IntPtr hModule = LoadLibrary("atiadlxx.dll");
            if (hModule != IntPtr.Zero)
            {
                foreach (var field in typeof(ADLDisplayColorManager).GetFields(BindingFlags.Static | BindingFlags.NonPublic))
                {
                    var externalFunction = field.GetCustomAttribute<ExternalFunctionAttribute>();
                    if (externalFunction == null) continue;

                    IntPtr functionPtr = GetProcAddress(hModule, field.Name);
                    if (functionPtr == IntPtr.Zero) return;

                    var function = Marshal.GetDelegateForFunctionPointer(functionPtr, field.FieldType);
                    field.SetValue(null, function);
                }

                Initialized = true;
            }
        }

        public static bool Initialized { get; }

        public static DisplayId GetDisplayId(Display display)
        {
            using (var context = new ADLContext())
            {
                if (context == IntPtr.Zero) return null;

                int status = ADL2_Adapter_NumberOfAdapters_Get(context, out int numberAdapters);
                if (status != 0 || numberAdapters == 0) return null;

                var adapterInfos = new AdapterInfo[numberAdapters];
                status = ADL2_Adapter_AdapterInfo_Get(context, adapterInfos, Marshal.SizeOf<AdapterInfo>() * numberAdapters);
                if (status != 0) return null;

                string adapterName = display.SourceAdapterName.Substring(4, display.SourceAdapterName.LastIndexOf('#') - 4).Replace('#', '_');
                var displayIds = new List<DisplayId>();
                foreach (AdapterInfo adapterInfo in adapterInfos)
                {
                    if (adapterInfo.iVendorID != 1002) continue;
                    if (!adapterInfo.iPresent) continue;

                    if (adapterInfo.strUDID.IndexOf(adapterName, StringComparison.OrdinalIgnoreCase) == -1) continue;
                    if (adapterInfo.strDisplayName != display.SourceDeviceName) continue;

                    int adapterIndex = adapterInfo.iAdapterIndex;
                    status = ADL2_Adapter_Active_Get(context, adapterIndex, out bool active);
                    if (status != 0 || !active) continue;

                    status = ADL2_Display_DisplayInfo_Get(context, adapterIndex, out int numDisplays, out IntPtr displayInfosPtr, 0);
                    if (status != 0 || displayInfosPtr == IntPtr.Zero) continue;

                    try
                    {
                        int displayActive = ADL_DISPLAY_DISPLAYINFO_DISPLAYCONNECTED | ADL_DISPLAY_DISPLAYINFO_DISPLAYMAPPED;
                        int displayInfoSize = Marshal.SizeOf(typeof(DisplayInfo));

                        for (int i = 0; i < numDisplays; i++)
                        {
                            var displayInfo = Marshal.PtrToStructure<DisplayInfo>(displayInfosPtr + i * displayInfoSize);

                            if ((displayInfo.iDisplayInfoValue & displayActive) != displayActive) continue;

                            displayIds.Add(new DisplayId(adapterIndex, displayInfo.displayID.iDisplayLogicalIndex));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(displayInfosPtr);
                    }
                }

                if (displayIds.Count == 0) return null;
                if (displayIds.Count == 1) return displayIds[0];

                byte[] edid = display.GetEDID()?.RawData ?? new byte[0];

                foreach (var displayId in displayIds.ToList())
                {
                    EDIDData edidData = new EDIDData()
                    {
                        iSize = Marshal.SizeOf(typeof(EDIDData)),
                        cEDIDData = new byte[256],
                    };

                    status = ADL2_Display_EdidData_Get(context, displayId.AdapterIndex, displayId.DisplayIndex, ref edidData);

                    if (edid.Length == 0 && (edidData.iEDIDSize == 0 || edidData.cEDIDData.All((b) => b == 0))) continue;

                    int size = Math.Min(edid.Length, edidData.iEDIDSize);
                    if (size != 0 && edid.Take(size).SequenceEqual(edidData.cEDIDData.Take(size))) continue;

                    displayIds.Remove(displayId);
                }

                if (displayIds.Count == 1) return displayIds[0];
            }
            return null;
        }

        public static bool IsColorSpaceConversionActive(DisplayId displayId)
        {
            using (var context = new ADLContext())
            {
                if (context == IntPtr.Zero) return false;

                var destination = DisplayGamutGet(context, displayId, ADL_GAMUT_REFERENCE_DESTINATION);
                if (destination.iPredefinedGamut != ADL_GAMUT_SPACE_CIE_RGB) return true;
                if (destination.iPredefinedWhitePoint != ADL_WHITE_POINT_6500K) return true;

                var source = DisplayGamutGet(context, displayId, ADL_GAMUT_REFERENCE_SOURCE);
                if (source.iPredefinedGamut != ADL_GAMUT_SPACE_CIE_RGB) return true;
                if (source.iPredefinedWhitePoint != ADL_WHITE_POINT_6500K) return true;

                var regamma = DisplayRegammaGet(context, displayId);
                if (regamma.iFeature != ADL_EDID_REGAMMA_PREDEFINED_SRGB) return true;

                return false;
            }
        }

        public static void ClearColorSpaceConversion(DisplayId displayId)
        {
            using (var context = new ADLContext())
            {
                if (context == IntPtr.Zero) return;

                ClearColorSpaceConversionGamut(context, displayId);
                ClearColorSpaceConversionRegamma(context, displayId);
            }
        }

        private static void ClearColorSpaceConversionGamut(ADLContext context, DisplayId displayId)
        {
            if (context == IntPtr.Zero) return;

            var reset = new GamutData
            {
                iPredefinedGamut = ADL_GAMUT_SPACE_CIE_RGB,
                iPredefinedWhitePoint = ADL_WHITE_POINT_6500K,
            };

            DisplayGamutSet(context, displayId, ADL_GAMUT_REFERENCE_DESTINATION, reset);
            DisplayGamutSet(context, displayId, ADL_GAMUT_REFERENCE_SOURCE, reset);
        }

        private static void ClearColorSpaceConversionRegamma(ADLContext context, DisplayId displayId)
        {
            if (context == IntPtr.Zero) return;

            var reset = new RegammaEx
            {
                iFeature = ADL_EDID_REGAMMA_PREDEFINED_SRGB,
                gamma = new ushort[256 * 3],
                coefficients = new ushort[5 * 3],
            };

            DisplayRegammaSet(context, displayId, reset);
        }

        public static void SetColorspaceConversion(DisplayId displayId, Calibration calibration, bool optimizaMatrix)
        {
            using (var context = new ADLContext())
            {
                if (context == IntPtr.Zero) return;

                SetColorspaceConversionGamut(context, displayId, calibration, optimizaMatrix);

                if (calibration.DeGamma == null && calibration.RGBGains.DifferenceMax(Matrix.One3x1()) < 1E-10)
                {
                    ClearColorSpaceConversionRegamma(context, displayId);
                }
                else
                {
                    SetColorspaceConversionRegamma(context, displayId, calibration);
                }
            }
        }

        private static void SetColorspaceConversionGamut(ADLContext context, DisplayId displayId, Calibration calibration, bool optimizaMatrix)
        {
            var destinationColorSpace = calibration.NativeColorSpace;
            var sourceColorSpace = calibration.TargetColorSpace;

            if (optimizaMatrix)
            {
                var matrixOptimization = OptimizeMatrix(calibration.MatrixRGBToRGB, (i, x) => (new ScaledToneCurve(calibration.FinalGamma[i]).SampleAt(x)));
                sourceColorSpace = new Colorimetry.ColorSpace(Colorimetry.RGBToXYZ(sourceColorSpace) * matrixOptimization);
            }

            var destination = new GamutData(destinationColorSpace);
            var source = new GamutData(sourceColorSpace);

            DisplayGamutSet(context, displayId, ADL_GAMUT_REFERENCE_DESTINATION, destination);
            DisplayGamutSet(context, displayId, ADL_GAMUT_REFERENCE_SOURCE, source);
        }

        private static void SetColorspaceConversionRegamma(ADLContext context, DisplayId displayId, Calibration calibration)
        {
            if (context == IntPtr.Zero) return;

            var srgbCurve = new SrgbEOTF();
            Func<int, double, double> deGamma;
            Func<int, double, double> reGamma;
            if (calibration.DeGamma != null)
            {
                deGamma = (j, v) => calibration.DeGamma[j].SampleAt(v);
                reGamma = (j, v) => calibration.ReGamma[j].SampleAt(v);
            }
            else
            {
                deGamma = (j, v) => srgbCurve.SampleAt(v);
                reGamma = (j, v) => (srgbCurve.SampleInverseAt(v) * calibration.ReGamma[j].SampleAt(calibration.RGBGains[j]));
            }

            Func<int, double, double> reGammaAsLut = (j, v) => reGamma(j, deGamma(j, srgbCurve.SampleInverseAt(v)));

            var reGammaEx = new RegammaEx(reGammaAsLut);

            DisplayRegammaSet(context, displayId, reGammaEx);
        }

        private static readonly string[] DitheringStateNames = { "Default", "Enable", "Disable" };
        private static readonly string[] DitheringBitsNames = { "6 bit", "8 bit", "10 bit" };

        private static readonly int[][] DitheringBitModes = new int[][] {
            EnumExtensions.ToArray<Dithering6Bit>().Select((d) => (int) d).ToArray(),
            EnumExtensions.ToArray<Dithering8Bit>().Select((d) => (int) d).ToArray(),
            EnumExtensions.ToArray<Dithering10Bit>().Select((d) => (int) d).ToArray()
        };

        private static readonly string[][] DitheringBitModesNames = new string[][] {
            EnumExtensions.ToArray<Dithering6Bit>().Select((d) => d.GetDescription()).ToArray(),
            EnumExtensions.ToArray<Dithering8Bit>().Select((d) => d.GetDescription()).ToArray(),
            EnumExtensions.ToArray<Dithering10Bit>().Select((d) => d.GetDescription()).ToArray()
        };

        public static Dithering GetDithering(DisplayId displayId)
        {
            using (var context = new ADLContext())
            {
                if (context == IntPtr.Zero) return null;

                int status = ADL2_Display_DitherState_Get(context, displayId.AdapterIndex, displayId.DisplayIndex, out int ditherState);

                if (status != 0)
                {
                    return null;
                }

                int state = -1;
                int bits = -1;
                int mode = -1;

                if (ditherState == 0)
                {
                    state = 2;
                }
                else if (ditherState == 1)
                {
                    state = 0;
                }
                else if (ditherState > 1)
                {
                    state = 1;
                    for (int i = 0; i < DitheringBitModes.Length; i++)
                    {
                        int[] iBitModes = DitheringBitModes[i];

                        mode = Array.IndexOf(iBitModes, ditherState);
                        if (mode != -1)
                        {
                            bits = i;
                            break;
                        }
                    }
                }

                Dithering dithering = new Dithering(
                    state,
                    bits,
                    mode,
                    DitheringStateNames,
                    bits != -1 ? DitheringBitsNames : new string[0],
                    bits != -1 ? DitheringBitModesNames[bits] : new string[0]
                );

                return dithering;
            }
        }

        public static void SetDithering(DisplayId displayId, Dithering dithering)
        {
            using (var context = new ADLContext())
            {
                if (context == IntPtr.Zero) return;

                int ditherState = -1;

                switch (dithering.State)
                {
                    case 0:
                        ditherState = 1;
                        break;

                    case 1:
                        int bits = dithering.Bits;
                        bits = Math.Max(bits, 0);
                        bits = Math.Min(bits, DitheringBitModes.Length - 1);

                        int mode = dithering.Mode;
                        mode = Math.Max(mode, 0);
                        mode = Math.Min(mode, DitheringBitModes[bits].Length - 1);

                        ditherState = DitheringBitModes[bits][mode];
                        break;

                    case 2:
                        ditherState = 0;
                        break;
                }

                if (ditherState != -1)
                {
                    ADL2_Display_DitherState_Set(context, displayId.AdapterIndex, displayId.DisplayIndex, ditherState);
                }
            }
        }

        private static GamutData DisplayGamutGet(ADLContext context, DisplayId displayId, int gamutRef)
        {
            int status = ADL2_Display_Gamut_Get(context, displayId.AdapterIndex, displayId.DisplayIndex, gamutRef, out GamutData gamutData);

            if (status != 0)
            {
                throw new ExternalAPIException(nameof(ADL2_Display_Gamut_Get), status);
            }

            return gamutData;
        }

        private static void DisplayGamutSet(ADLContext context, DisplayId displayId, int gamutRef, GamutData gamutData)
        {
            int status = ADL2_Display_Gamut_Set(context, displayId.AdapterIndex, displayId.DisplayIndex, gamutRef, ref gamutData);

            if (status != 0)
            {
                throw new ExternalAPIException(nameof(ADL2_Display_Gamut_Set), status);
            }
        }

        private static RegammaEx DisplayRegammaGet(ADLContext context, DisplayId displayId)
        {
            int status = ADL2_Display_RegammaR1_Get(context, displayId.AdapterIndex, displayId.DisplayIndex, out RegammaEx regammaEx);

            if (status != 0)
            {
                throw new ExternalAPIException(nameof(ADL2_Display_RegammaR1_Get), status);
            }

            return regammaEx;
        }

        private static void DisplayRegammaSet(ADLContext context, DisplayId displayId, RegammaEx regammaEx)
        {
            int status = ADL2_Display_RegammaR1_Set(context, displayId.AdapterIndex, displayId.DisplayIndex, ref regammaEx);

            if (status != 0)
            {
                throw new ExternalAPIException(nameof(ADL2_Display_RegammaR1_Set), status);
            }
        }

        private static Matrix OptimizeMatrix(Matrix matrix, Func<int, double, double> sampleAt)
        {
            ToneCurve srgbCurve = new SrgbEOTF();

            Matrix white = Colorimetry.XYToXYZ(Colorimetry.D65);
            Matrix white3x3 = Matrix.FromDiagonal(white);

            Matrix target = matrix;
            target = target.Map(x => x > 0 ? x < 1 ? x : 1 : 0);
            target = Matrix.FromDiagonal(target * white).Inverse() * white3x3 * target;

            Matrix identityMatrix = Matrix.Identity();
            Matrix finalMatrixOptimization = identityMatrix;

            for (int i = 0; i < 10000; i++)
            {
                Matrix result = matrix * finalMatrixOptimization;

                result = result.Map(x => x > 0 ? x < 1 ? x : 1 : 0);

                result = result.Map((r, c, x) => sampleAt(r, srgbCurve.SampleInverseAt(x)));

                result = Matrix.FromDiagonal(result * white).Inverse() * white3x3 * result;

                Matrix matrixOptimization = result.Inverse() * target;

                if (identityMatrix.DifferenceMax(matrixOptimization) < 1E-10)
                {
                    break;
                }

                matrixOptimization = 0.9 * identityMatrix + 0.1 * matrixOptimization;
                finalMatrixOptimization = matrixOptimization * finalMatrixOptimization;
            }

            return finalMatrixOptimization;
        }

        public class DisplayId
        {
            public int AdapterIndex { get; }
            public int DisplayIndex { get; }

            public DisplayId(int adapterIndex, int displayIndex)
            {
                AdapterIndex = adapterIndex;
                DisplayIndex = displayIndex;
            }
        }

        private class ADLContext : IDisposable
        {
            private IntPtr _context;
            public static implicit operator IntPtr(ADLContext context) => context._context;

            public ADLContext()
            {
                ADL2_Main_Control_Create(Marshal.AllocHGlobal, 1, out _context);
            }

            public void Dispose()
            {
                if (_context != IntPtr.Zero)
                {
                    ADL2_Main_Control_Destroy(_context);
                    _context = IntPtr.Zero;
                }
            }
        }

        private enum Dithering6Bit
        {
            [Description("Temporal")]
            ADL_DL_DISPLAY_DITHER_FM6 = 2,

            [Description("SpatialDynamic")]
            ADL_DL_DISPLAY_DITHER_DITH6 = 5,

            [Description("SpatialStatic")]
            ADL_DL_DISPLAY_DITHER_DITH6_NO_FRAME_RAND = 8,

            [Description("Truncation")]
            ADL_DL_DISPLAY_DITHER_TRUN6 = 11,

            [Description("Truncation 10bit -> SpatialDynamic")]
            ADL_DL_DISPLAY_DITHER_TRUN10_DITH6 = 15,

            [Description("Truncation 10bit -> Temporal")]
            ADL_DL_DISPLAY_DITHER_TRUN10_FM6 = 17,

            [Description("Truncation 10bit -> SpatialDynamic 8bit -> Temporal")]
            ADL_DL_DISPLAY_DITHER_TRUN10_DITH8_FM6 = 18,

            [Description("SpatialDynamic 10bit -> Temporal")]
            ADL_DL_DISPLAY_DITHER_DITH10_FM6 = 20,

            [Description("Truncation 8bit -> SpatialDynamic")]
            ADL_DL_DISPLAY_DITHER_TRUN8_DITH6 = 21,

            [Description("Truncation 8bit -> Temporal")]
            ADL_DL_DISPLAY_DITHER_TRUN8_FM6 = 22,

            [Description("SpatialDynamic 8bit -> Temporal")]
            ADL_DL_DISPLAY_DITHER_DITH8_FM6 = 23
        }

        private enum Dithering8Bit
        {
            [Description("Temporal")]
            ADL_DL_DISPLAY_DITHER_FM8 = 3,

            [Description("SpatialDynamic")]
            ADL_DL_DISPLAY_DITHER_DITH8 = 6,

            [Description("SpatialStatic")]
            ADL_DL_DISPLAY_DITHER_DITH8_NO_FRAME_RAND = 9,

            [Description("Truncation")]
            ADL_DL_DISPLAY_DITHER_TRUN8 = 12,

            [Description("Truncation 10bit -> SpatialDynamic")]
            ADL_DL_DISPLAY_DITHER_TRUN10_DITH8 = 14,

            [Description("Truncation 10bit -> Temporal")]
            ADL_DL_DISPLAY_DITHER_TRUN10_FM8 = 16,

            [Description("SpatialDynamic 10bit -> Temporal")]
            ADL_DL_DISPLAY_DITHER_DITH10_FM8 = 19
        }

        private enum Dithering10Bit
        {
            [Description("Temporal")]
            ADL_DL_DISPLAY_DITHER_FM10 = 4,

            [Description("SpatialDynamic")]
            ADL_DL_DISPLAY_DITHER_DITH10 = 7,

            [Description("SpatialStatic")]
            ADL_DL_DISPLAY_DITHER_DITH10_NO_FRAME_RAND = 10,

            [Description("Truncation")]
            ADL_DL_DISPLAY_DITHER_TRUN10 = 13
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct AdapterInfo
    {
        public int iSize;
        public int iAdapterIndex;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strUDID;
        public int iBusNumber;
        public int iDeviceNumber;
        public int iFunctionNumber;
        public int iVendorID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strAdapterName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strDisplayName;
        [MarshalAs(UnmanagedType.Bool)]
        public bool iPresent;
        [MarshalAs(UnmanagedType.Bool)]
        public bool iExist;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strDriverPath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strDriverPathExt;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strPNPString;
        public int iOSDisplayIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DisplayID
    {
        public int iDisplayLogicalIndex;
        public int iDisplayPhysicalIndex;
        public int iDisplayLogicalAdapterIndex;
        public int iDisplayPhysicalAdapterIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DisplayInfo
    {
        public DisplayID displayID;
        public int iDisplayControllerIndex;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strDisplayManufacturerName;
        public int iDisplayType;
        public int iDisplayOutputType;
        public int iDisplayConnector;
        public int iDisplayInfoMask;
        public int iDisplayInfoValue;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EDIDData
    {
        public int iSize;
        public int iFlag;
        public int iEDIDSize;
        public int iBlockIndex;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] cEDIDData;
        public int iReserved1;
        public int iReserved2;
        public int iReserved3;
        public int iReserved4;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GamutData
    {
        public int iFeature;
        public int iPredefinedGamut;
        public int iPredefinedWhitePoint;
        public int iWhitePointX;
        public int iWhitePointY;
        public int iRedX;
        public int iRedY;
        public int iGreenX;
        public int iGreenY;
        public int iBlueX;
        public int iBlueY;

        public GamutData(Colorimetry.ColorSpace colorspace) : this()
        {
            iFeature = 0b11;
            iWhitePointX = (int)Math.Round(colorspace.White.X * 10000);
            iWhitePointY = (int)Math.Round(colorspace.White.Y * 10000);
            iRedX = (int)Math.Round(colorspace.Red.X * 10000);
            iRedY = (int)Math.Round(colorspace.Red.Y * 10000);
            iGreenX = (int)Math.Round(colorspace.Green.X * 10000);
            iGreenY = (int)Math.Round(colorspace.Green.Y * 10000);
            iBlueX = (int)Math.Round(colorspace.Blue.X * 10000);
            iBlueY = (int)Math.Round(colorspace.Blue.Y * 10000);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RegammaEx
    {
        public int iFeature;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256 * 3)]
        public ushort[] gamma;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5 * 3)]
        public ushort[] coefficients;

        public RegammaEx(Func<int, double, double> sampleAt) : this()
        {
            iFeature = (1 << 4);
            gamma = new ushort[256 * 3];
            for (var i = 1; i < 256; i++)
            {
                var value = i / 255d;
                for (var j = 0; j < 3; j++)
                {
                    gamma[j * 256 + i] = (ushort)Math.Round(sampleAt(j, value) * ushort.MaxValue);
                }
            }
            coefficients = new ushort[5 * 3];
        }
    }
}
