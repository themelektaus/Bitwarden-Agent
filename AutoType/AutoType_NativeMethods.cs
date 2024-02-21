using System.Runtime.InteropServices;
using System.Text;

namespace BitwardenAgent;

public static partial class AutoType_NativeMethods
{
    public const int VK_SHIFT = 0x10;
    public const int VK_CONTROL = 0x11;
    public const int VK_MENU = 0x12;
    public const int VK_CAPITAL = 0x14;
    public const int VK_NUMLOCK = 0x90;
    public const int VK_LSHIFT = 0xA0;
    public const int VK_RSHIFT = 0xA1;
    public const int VK_LCONTROL = 0xA2;
    public const int VK_RCONTROL = 0xA3;
    public const int VK_LMENU = 0xA4;
    public const int VK_RMENU = 0xA5;
    public const int VK_LWIN = 0x5B;
    public const int VK_RWIN = 0x5C;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MOUSEINPUT32_WithSkip
    {
        public uint __Unused0;
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct KEYBDINPUT32_WithSkip
    {
        public uint __Unused0;
        public ushort VirtualKeyCode;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct INPUT32
    {
        [FieldOffset(0)] public uint Type;
        [FieldOffset(0)] public MOUSEINPUT32_WithSkip MouseInput;
        [FieldOffset(0)] public KEYBDINPUT32_WithSkip KeyboardInput;
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public struct SpecializedKeyboardINPUT64
    {
        [FieldOffset(0)] public uint Type;
        [FieldOffset(8)] public ushort VirtualKeyCode;
        [FieldOffset(10)] public ushort ScanCode;
        [FieldOffset(12)] public uint Flags;
        [FieldOffset(16)] public uint Time;
        [FieldOffset(24)] public nint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHFILEINFO
    {
        public nint hIcon;
        public int iIcon;
        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(nint hWnd);

    [DllImport("User32.dll")]
    public static extern nint GetWindow(nint hWnd, uint uCmd);

    [DllImport("User32.dll", CharSet = CharSet.Auto, ExactSpelling = false, SetLastError = true)]
    public static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("User32.dll", CharSet = CharSet.Auto, ExactSpelling = false, SetLastError = true)]
    static extern int GetWindowTextLength(nint hWnd);

    [DllImport("User32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("User32.dll", EntryPoint = "SendInput", SetLastError = true)]
    public static extern uint SendInput32(uint nInputs, INPUT32[] pInputs, int cbSize);

    [DllImport("User32.dll", EntryPoint = "SendInput", SetLastError = true)]
    public static extern uint SendInput64Special(uint nInputs, SpecializedKeyboardINPUT64[] pInputs, int cbSize);

    [DllImport("User32.dll")]
    public static extern nint GetMessageExtraInfo();

    [DllImport("User32.dll")]
    public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("User32.dll")]
    public static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, nint hKL);

    [DllImport("User32.dll", CharSet = CharSet.Auto)]
    public static extern ushort VkKeyScan(char ch);

    [DllImport("User32.dll", CharSet = CharSet.Auto)]
    public static extern ushort VkKeyScanEx(char ch, nint hKL);

    [DllImport("User32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int ToUnicode(uint wVirtKey, uint wScanCode, nint lpKeyState, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder sbBuff, int cchBuff, uint wFlags);

    [DllImport("User32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, nint lpKeyState, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder sbBuff, int cchBuff, uint wFlags, nint hKL);

    [DllImport("User32.dll")]
    public static extern ushort GetKeyState(int vKey);

    [DllImport("User32.dll")]
    public static extern ushort GetAsyncKeyState(int vKey);

    [DllImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BlockInput([MarshalAs(UnmanagedType.Bool)] bool fBlockIt);

    [DllImport("User32.dll")]
    public static extern nint GetKeyboardLayout(uint idThread);

    [DllImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("User32.dll")]
    public static extern uint GetWindowThreadProcessId(nint hWnd, [Out] out uint lpdwProcessId);

    [DllImport("User32.dll", CharSet = CharSet.Auto, ExactSpelling = false, SetLastError = true)]
    static extern int GetWindowText(nint hWnd, nint lpString, int nMaxCount);
}
