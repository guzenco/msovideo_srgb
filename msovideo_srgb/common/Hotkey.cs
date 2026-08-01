using System;
using System.ComponentModel;

namespace msovideo_srgb
{
    public class Hotkey
    {
        public KeyModifier KeyModifier { get; set; }
        public VirtualKey VirtualKey { get; set; }

        public Hotkey()
        {
            KeyModifier = KeyModifier.None;
            VirtualKey = VirtualKey.None;
        }

        public Hotkey(uint keyModifier, uint virtualKey)
        {
            KeyModifier = (KeyModifier)keyModifier;
            VirtualKey = (VirtualKey)virtualKey;
        }

        public bool IsBindable => KeyModifier.IsDefined() && KeyModifier != KeyModifier.None && VirtualKey.IsDefined() && VirtualKey != VirtualKey.None;

        public override bool Equals(object obj)
        {
            if (obj is Hotkey other)
            {
                return KeyModifier == other.KeyModifier && VirtualKey == other.VirtualKey;
            }
            return false;       
        }

        public override int GetHashCode()
        {
            int hashCode = -120903952;
            hashCode = hashCode * -1521134295 + KeyModifier.GetHashCode();
            hashCode = hashCode * -1521134295 + VirtualKey.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return IsBindable ? KeyModifier.GetDescription() + " + " + VirtualKey.GetDescription() : "";
        }
    }

    public enum KeyModifier : uint
    {
        [Description("")]
        None = 0,

        [Description("Alt")]
        Alt = KeyModifierBase.Alt,
        [Description("Ctrl")]
        Ctrl = KeyModifierBase.Ctrl,
        [Description("Ctrl + Alt")]
        CtrlAlt = KeyModifierBase.Ctrl | KeyModifierBase.Alt,
        [Description("Shift")]
        Shift = KeyModifierBase.Shift,
        [Description("Shift + Alt")]
        ShiftAlt = KeyModifierBase.Shift | KeyModifierBase.Alt,
        [Description("Shift + Ctrl")]
        ShiftCtrl = KeyModifierBase.Shift |KeyModifierBase.Ctrl,
        [Description("Shift + Ctrl + Alt")]
        ShiftCtrlAlt = KeyModifierBase.Shift | KeyModifierBase.Ctrl | KeyModifierBase.Alt,
        [Description("Win")]
        Win = KeyModifierBase.Win,
        [Description("Win + Alt")]
        WinAlt = KeyModifierBase.Win | KeyModifierBase.Alt,
        [Description("Win + Ctrl")]
        WinCtrl = KeyModifierBase.Win | KeyModifierBase.Ctrl,
        [Description("Win + Ctrl + Alt")]
        WinCtrlAlt = KeyModifierBase.Win | KeyModifierBase.Ctrl | KeyModifierBase.Alt,
        [Description("Win + Shift")]
        WinShift = KeyModifierBase.Win | KeyModifierBase.Shift,
        [Description("Win + Shift + Alt")]
        WinShiftAlt = KeyModifierBase.Win | KeyModifierBase.Shift | KeyModifierBase.Alt,
        [Description("Win + Shift + Ctrl")]
        WinCtrlShift = KeyModifierBase.Win | KeyModifierBase.Shift | KeyModifierBase.Ctrl
    }

    public enum VirtualKey : uint
    {
        [Description("")]
        None = 0,

        [Description("Backspace")]
        VK_BACK = 0x08,
        [Description("Tab")]
        VK_TAB = 0x09,
        [Description("Clear")]
        VK_CLEAR = 0x0C,

        [Description("Pause")]
        VK_PAUSE = 0x13,
        [Description("Caps lock")]
        VK_CAPITAL = 0x14,
        [Description("IME Kana")]
        VK_KANA = 0x15,
        [Description("IME Hangul")]
        VK_HANGUL = 0x15,
        [Description("IME On")]
        VK_IME_ON = 0x16,
        [Description("IME Junja")]
        VK_JUNJA = 0x17,
        [Description("IME final")]
        VK_FINAL = 0x18,
        [Description("IME Hanja")]
        VK_HANJA = 0x19,
        [Description("IME Kanji")]
        VK_KANJI = 0x19,
        [Description("IME Off")]
        VK_IME_OFF = 0x1A,
        [Description("Esc")]
        VK_ESCAPE = 0x1B,
        [Description("IME convert")]
        VK_CONVERT = 0x1C,
        [Description("IME nonconvert")]
        VK_NONCONVERT = 0x1D,
        [Description("IME accept")]
        VK_ACCEPT = 0x1E,
        [Description("IME modechange")]
        VK_MODECHANGE = 0x1F,
        [Description("Spacebar")]
        VK_SPACE = 0x20,
        [Description("Page up")]
        VK_PRIOR = 0x21,
        [Description("Page down")]
        VK_NEXT = 0x22,
        [Description("End")]
        VK_END = 0x23,
        [Description("Home")]
        VK_HOME = 0x24,
        [Description("Left")]
        VK_LEFT = 0x25,
        [Description("Up")]
        VK_UP = 0x26,
        [Description("Right")]
        VK_RIGHT = 0x27,
        [Description("Down")]
        VK_DOWN = 0x28,
        [Description("Select")]
        VK_SELECT = 0x29,
        [Description("Print")]
        VK_PRINT = 0x2A,
        [Description("Execute")]
        VK_EXECUTE = 0x2B,
        [Description("Print screen")]
        VK_SNAPSHOT = 0x2C,
        [Description("Insert")]
        VK_INSERT = 0x2D,
        [Description("Delete")]
        VK_DELETE = 0x2E,
        [Description("Help")]
        VK_HELP = 0x2F,

        [Description("0")]
        VK_0 = 0x30,
        [Description("1")]
        VK_1 = 0x31,
        [Description("2")]
        VK_2 = 0x32,
        [Description("3")]
        VK_3 = 0x33,
        [Description("4")]
        VK_4 = 0x34,
        [Description("5")]
        VK_5 = 0x35,
        [Description("6")]
        VK_6 = 0x36,
        [Description("7")]
        VK_7 = 0x37,
        [Description("8")]
        VK_8 = 0x38,
        [Description("9")]
        VK_9 = 0x39,

        A = 0x41,
        B = 0x42,
        C = 0x43,
        D = 0x44,
        E = 0x45,
        F = 0x46,
        G = 0x47,
        H = 0x48,
        I = 0x49,
        J = 0x4A,
        K = 0x4B,
        L = 0x4C,
        M = 0x4D,
        N = 0x4E,
        O = 0x4F,
        P = 0x50,
        Q = 0x51,
        R = 0x52,
        S = 0x53,
        T = 0x54,
        U = 0x55,
        V = 0x56,
        W = 0x57,
        X = 0x58,
        Y = 0x59,
        Z = 0x5A,

        [Description("Application")]
        VK_APPS = 0x5D,
        [Description("Computer Sleep")]
        VK_SLEEP = 0x5F,

        [Description("Num 0")]
        VK_NUMPAD0 = 0x60,
        [Description("Num 1")]
        VK_NUMPAD1 = 0x61,
        [Description("Num 2")]
        VK_NUMPAD2 = 0x62,
        [Description("Num 3")]
        VK_NUMPAD3 = 0x63,
        [Description("Num 4")]
        VK_NUMPAD4 = 0x64,
        [Description("Num 5")]
        VK_NUMPAD5 = 0x65,
        [Description("Num 6")]
        VK_NUMPAD6 = 0x66,
        [Description("Num 7")]
        VK_NUMPAD7 = 0x67,
        [Description("Num 8")]
        VK_NUMPAD8 = 0x68,
        [Description("Num 9")]
        VK_NUMPAD9 = 0x69,

        [Description("Multiply")]
        VK_MULTIPLY = 0x6A,
        [Description("Add")]
        VK_ADD = 0x6B,
        [Description("Separator")]
        VK_SEPARATOR = 0x6C,
        [Description("Subtract")]
        VK_SUBTRACT = 0x6D,
        [Description("Decimal")]
        VK_DECIMAL = 0x6E,
        [Description("Divide")]
        VK_DIVIDE = 0x6F,

        [Description("F1")]
        VK_F1 = 0x70,
        [Description("F2")]
        VK_F2 = 0x71,
        [Description("F3")]
        VK_F3 = 0x72,
        [Description("F4")]
        VK_F4 = 0x73,
        [Description("F5")]
        VK_F5 = 0x74,
        [Description("F6")]
        VK_F6 = 0x75,
        [Description("F7")]
        VK_F7 = 0x76,
        [Description("F8")]
        VK_F8 = 0x77,
        [Description("F9")]
        VK_F9 = 0x78,
        [Description("F10")]
        VK_F10 = 0x79,
        [Description("F11")]
        VK_F11 = 0x7A,
        [Description("F12")]
        VK_F12 = 0x7B,
        [Description("F13")]
        VK_F13 = 0x7C,
        [Description("F14")]
        VK_F14 = 0x7D,
        [Description("F15")]
        VK_F15 = 0x7E,
        [Description("F16")]
        VK_F16 = 0x7F,
        [Description("F17")]
        VK_F17 = 0x80,
        [Description("F18")]
        VK_F18 = 0x81,
        [Description("F19")]
        VK_F19 = 0x82,
        [Description("F20")]
        VK_F20 = 0x83,
        [Description("F21")]
        VK_F21 = 0x84,
        [Description("F22")]
        VK_F22 = 0x85,
        [Description("F23")]
        VK_F23 = 0x86,
        [Description("F24")]
        VK_F24 = 0x87,

        [Description("Num lock")]
        VK_NUMLOCK = 0x90,
        [Description("Scroll lock")]
        VK_SCROLL = 0x91,

        [Description("Browser Back")]
        VK_BROWSER_BACK = 0xA6,
        [Description("Browser Forward")]
        VK_BROWSER_FORWARD = 0xA7,
        [Description("Browser Refresh")]
        VK_BROWSER_REFRESH = 0xA8,
        [Description("Browser Stop")]
        VK_BROWSER_STOP = 0xA9,
        [Description("Browser Search")]
        VK_BROWSER_SEARCH = 0xAA,
        [Description("Browser Favorites")]
        VK_BROWSER_FAVORITES = 0xAB,
        [Description("Browser Home")]
        VK_BROWSER_HOME = 0xAC,
        [Description("Volume Mute")]
        VK_VOLUME_MUTE = 0xAD,
        [Description("Volume Down")]
        VK_VOLUME_DOWN = 0xAE,
        [Description("Volume Up")]
        VK_VOLUME_UP = 0xAF,
        [Description("Next Track")]
        VK_MEDIA_NEXT_TRACK = 0xB0,
        [Description("Previous Track")]
        VK_MEDIA_PREV_TRACK = 0xB1,
        [Description("Stop Media")]
        VK_MEDIA_STOP = 0xB2,
        [Description("Play/Pause Media")]
        VK_MEDIA_PLAY_PAUSE = 0xB3,
        [Description("Start Mail")]
        VK_LAUNCH_MAIL = 0xB4,
        [Description("Select Media")]
        VK_LAUNCH_MEDIA_SELECT = 0xB5,
        [Description("Start App 1")]
        VK_LAUNCH_APP1 = 0xB6,
        [Description("Start App 2")]
        VK_LAUNCH_APP2 = 0xB7,

        [Description("Plus")]
        VK_OEM_PLUS = 0xBB,
        [Description("Comma")]
        VK_OEM_COMMA = 0xBC,
        [Description("Minus")]
        VK_OEM_MINUS = 0xBD,
        [Description("Period")]
        VK_OEM_PERIOD = 0xBE,

        [Description("Gamepad A")]
        VK_GAMEPAD_A = 0xC3,
        [Description("Gamepad B")]
        VK_GAMEPAD_B = 0xC4,
        [Description("Gamepad X")]
        VK_GAMEPAD_X = 0xC5,
        [Description("Gamepad Y")]
        VK_GAMEPAD_Y = 0xC6,
        [Description("R-Shoulder")]
        VK_GAMEPAD_RIGHT_SHOULDER = 0xC7,
        [Description("L-Shoulder")]
        VK_GAMEPAD_LEFT_SHOULDER = 0xC8,
        [Description("L-Trigger")]
        VK_GAMEPAD_LEFT_TRIGGER = 0xC9,
        [Description("R-Trigger")]
        VK_GAMEPAD_RIGHT_TRIGGER = 0xCA,
        [Description("D-pad Up")]
        VK_GAMEPAD_DPAD_UP = 0xCB,
        [Description("D-pad Down")]
        VK_GAMEPAD_DPAD_DOWN = 0xCC,
        [Description("D-pad L")]
        VK_GAMEPAD_DPAD_LEFT = 0xCD,
        [Description("D-pad R")]
        VK_GAMEPAD_DPAD_RIGHT = 0xCE,
        [Description("Gamepad Menu")]
        VK_GAMEPAD_MENU = 0xCF,
        [Description("Gamepad View")]
        VK_GAMEPAD_VIEW = 0xD0,
        [Description("L-Stick")]
        VK_GAMEPAD_LEFT_THUMBSTICK_BUTTON = 0xD1,
        [Description("R-Stick")]
        VK_GAMEPAD_RIGHT_THUMBSTICK_BUTTON = 0xD2,
        [Description("L-Stick up")]
        VK_GAMEPAD_LEFT_THUMBSTICK_UP = 0xD3,
        [Description("L-Stick down")]
        VK_GAMEPAD_LEFT_THUMBSTICK_DOWN = 0xD4,
        [Description("L-Stick right")]
        VK_GAMEPAD_LEFT_THUMBSTICK_RIGHT = 0xD5,
        [Description("L-Stick left")]
        VK_GAMEPAD_LEFT_THUMBSTICK_LEFT = 0xD6,
        [Description("R-Stick up")]
        VK_GAMEPAD_RIGHT_THUMBSTICK_UP = 0xD7,
        [Description("R-Stick down")]
        VK_GAMEPAD_RIGHT_THUMBSTICK_DOWN = 0xD8,
        [Description("R-Stick right")]
        VK_GAMEPAD_RIGHT_THUMBSTICK_RIGHT = 0xD9,
        [Description("R-Stick left")]
        VK_GAMEPAD_RIGHT_THUMBSTICK_LEFT = 0xDA,

        [Description("IME PROCESS")]
        VK_PROCESSKEY = 0xE5,

        [Description("Attn")]
        VK_ATTN = 0xF6,
        [Description("CrSel")]
        VK_CRSEL = 0xF7,
        [Description("ExSel")]
        VK_EXSEL = 0xF8,
        [Description("Erase EOF")]
        VK_EREOF = 0xF9,
        [Description("Play")]
        VK_PLAY = 0xFA,
        [Description("Zoom")]
        VK_ZOOM = 0xFB,
        [Description("PA1")]
        VK_PA1 = 0xFD,
        [Description("Clear")]
        VK_OEM_CLEAR = 0xFE,
    }

    public enum KeyModifierBase : uint
    {
        None = 0,

        Alt = 0x0001,
        Ctrl = 0x0002,
        Shift = 0x0004,
        Win = 0x0008,

        NoRepeat = 0x4000
    }
}
