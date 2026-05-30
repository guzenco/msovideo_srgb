using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace msovideo_srgb
{
    public static class DisplayStateObserver
    {
        public static event EventHandler OnDisplayWake;

        public static void Init(HwndSource source)
        {
            source.AddHook(WndProc);
            RegisterPowerSettingNotification(source.Handle, ref GUID_CONSOLE_DISPLAY_STATE, 0);
        }

        internal static Guid GUID_CONSOLE_DISPLAY_STATE = new Guid("6FE69556-704A-47A0-8F24-C28D936FDA47");
        
        internal const int WM_POWERBROADCAST = 0x0218;
        internal const int PBT_POWERSETTINGCHANGE = 0x8013;  

        internal const int WM_WTSSESSION_CHANGE = 0x02B1;
        internal const int WTS_SESSION_LOGON = 0x5;
        internal const int WTS_SESSION_UNLOCK = 0x8;
        
        [StructLayout(LayoutKind.Sequential)]
        internal struct POWERBROADCAST_SETTING
        {
            public Guid PowerSetting;
            public int DataLength;
            public int Data;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid PowerSettingGuid, int Flags);

        private static int powerState = 1;

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_POWERBROADCAST && wParam.ToInt32() == PBT_POWERSETTINGCHANGE)
            {
                POWERBROADCAST_SETTING data = (POWERBROADCAST_SETTING)Marshal.PtrToStructure(lParam, typeof(POWERBROADCAST_SETTING));
                if (data.PowerSetting == GUID_CONSOLE_DISPLAY_STATE)
                {
                    int newPowerState = data.Data;
                    if (powerState == 0 && newPowerState != 0)
                    {
                        OnDisplayWake?.Invoke(null, null);
                    }
                    powerState = newPowerState;
                }
            }
            if (msg == WM_WTSSESSION_CHANGE)
            {
                int reason = wParam.ToInt32();
                if (reason == WTS_SESSION_LOGON || reason == WTS_SESSION_UNLOCK)
                {
                    RegisterPowerSettingNotification(hwnd, ref GUID_CONSOLE_DISPLAY_STATE, 0);
                }
            }
            return IntPtr.Zero;
        }
    }
}
