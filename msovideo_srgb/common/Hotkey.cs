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

        public bool IsBindable => KeyModifier != KeyModifier.None && VirtualKey != VirtualKey.None;

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

        [Description("Ctrl")]
        Ctrl = KeyModifierBase.Ctrl,
        [Description("Ctrl + Alt")]
        CtrlAlt = KeyModifierBase.Ctrl | KeyModifierBase.Alt,
        [Description("Ctrl + Shift")]
        CtrlShift = KeyModifierBase.Ctrl | KeyModifierBase.Shift,
        [Description("Ctrl + Shift + Alt")]
        CtrlShiftAlt = KeyModifierBase.Ctrl | KeyModifierBase.Shift | KeyModifierBase.Alt,
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

        F1 = 0x70,
        F2 = 0x71,
        F3 = 0x72,
        F4 = 0x73,
        F5 = 0x74,
        F6 = 0x75,
        F7 = 0x76,
        F8 = 0x77,
        F9 = 0x78,
        F10 = 0x79,
        F11 = 0x7A,
        F12 = 0x7B
    }

    internal enum KeyModifierBase : uint
    {
        Alt = 0x0001,
        Ctrl = 0x0002,
        Shift = 0x0004,
        Win = 0x0008
    }
}
