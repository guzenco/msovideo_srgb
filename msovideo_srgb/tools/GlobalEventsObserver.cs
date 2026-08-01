using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace msovideo_srgb
{
    public static class GlobalEventsObserver
    {
        public static event EventHandler OnDisplayWake;
        public static event Action<int> OnHotKey;

        private static IntPtr _hPowerNotify;
        private static HashSet<int> _hotKeyIds;

        private static HwndSource _hwndSource = InitHwndSource();

        private static HwndSource InitHwndSource()
        {
            HwndSource hwndSource;
            HwndSourceParameters parameters = new HwndSourceParameters() { WindowStyle = 0, ParentWindow = new IntPtr(-3) };
            hwndSource = new HwndSource(parameters);    
            hwndSource.AddHook(WndProc);
            _hPowerNotify = RegisterPowerSettingNotification(hwndSource.Handle, ref GUID_CONSOLE_DISPLAY_STATE, 0);
            _hotKeyIds = new HashSet<int>();
            App.OnAppExit += OnExit;
            return hwndSource;
        }       

        private static void OnExit(object sender, EventArgs e)
        {
            if (_hPowerNotify != IntPtr.Zero)
            {
                UnregisterPowerSettingNotification(_hPowerNotify);
            }

            ClearHotKeys();
        }

        public static void ClearHotKeys()
        {
            foreach (int id in _hotKeyIds)
            {
                UnregisterHotKey(_hwndSource.Handle, id);
            }
            _hotKeyIds.Clear();
        }

        public static void AddHotKey(int id, Hotkey hotkey)
        {
            if (_hotKeyIds.Contains(id))
            {
                UnregisterHotKey(_hwndSource.Handle, id);
            }
            RegisterHotKey(_hwndSource.Handle, id, (uint)hotkey.KeyModifier, (uint)hotkey.VirtualKey);
            _hotKeyIds.Add(id);
        }

        internal static Guid GUID_CONSOLE_DISPLAY_STATE = new Guid("6FE69556-704A-47A0-8F24-C28D936FDA47");
        
        internal const int WM_POWERBROADCAST = 0x0218;
        internal const int PBT_POWERSETTINGCHANGE = 0x8013;  

        internal const int WM_WTSSESSION_CHANGE = 0x02B1;
        internal const int WTS_SESSION_LOGON = 0x5;
        internal const int WTS_SESSION_UNLOCK = 0x8;

        internal const int WM_HOTKEY = 0x0312;

        [StructLayout(LayoutKind.Sequential)]
        internal struct POWERBROADCAST_SETTING
        {
            public Guid PowerSetting;
            public int DataLength;
            public int Data;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid PowerSettingGuid, int Flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr UnregisterPowerSettingNotification(IntPtr handle);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr UnregisterHotKey(IntPtr hWnd, int id);

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
            else if (msg == WM_WTSSESSION_CHANGE)
            {
                int reason = wParam.ToInt32();
                if (reason == WTS_SESSION_LOGON || reason == WTS_SESSION_UNLOCK)
                {
                    RegisterPowerSettingNotification(hwnd, ref GUID_CONSOLE_DISPLAY_STATE, 0);
                }
            }
            else if (msg == WM_HOTKEY)
            {
                OnHotKey?.Invoke((int)wParam);
            }
            return IntPtr.Zero;
        }
    }
}
