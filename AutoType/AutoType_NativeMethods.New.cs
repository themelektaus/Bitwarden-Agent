using System.Runtime.InteropServices;
using System.Text;

using StringComparison = System.StringComparison;

using Environment = System.Environment;
using Application = System.Windows.Forms.Application;
using Process = System.Diagnostics.Process;

using MethodImpl = System.Runtime.CompilerServices.MethodImplAttribute;
using MIO = System.Runtime.CompilerServices.MethodImplOptions;

using Path = System.IO.Path;

namespace BitwardenAgent;

public static partial class AutoType_NativeMethods
{
    public static bool IsWindowEx(nint hWnd)
    {
        return IsWindow(hWnd);
    }

    public static int GetWindowStyle(nint hWnd)
    {
        return GetWindowLong(hWnd, -16);
    }

    public static bool SetForegroundWindowEx(nint hWnd)
    {
        return SetForegroundWindow(hWnd);
    }

    public static bool EnsureForegroundWindow(nint hWnd)
    {
        if (!IsWindowEx(hWnd))
            return false;

        var hWndInit = GetForegroundWindow();

        if (!SetForegroundWindowEx(hWnd))
            return false;

        int nStartMS = Environment.TickCount;
        while ((Environment.TickCount - nStartMS) < 1000)
        {
            var h = GetForegroundWindow();
            if (h == hWnd)
                return true;

            if ((h != nint.Zero) && (h != hWndInit))
                return true;

            Application.DoEvents();
        }

        return false;
    }

    public static bool LoseFocus(nint hWnd)
    {
        try
        {
            for (; ; )
            {
                var hWndPrev = hWnd;
                hWnd = GetWindow(hWnd, 2);

                if (hWnd == nint.Zero)
                    return false;

                if (hWnd == hWndPrev)
                    return false;

                int nStyle = GetWindowStyle(hWnd);
                if ((nStyle & 0x10000000) == 0)
                    continue;

                if (GetWindowTextLength(hWnd) == 0)
                    continue;

                if (IsTaskBar(hWnd))
                    continue;

                break;
            }

            return EnsureForegroundWindow(hWnd);
        }
        catch
        {
            return false;
        }
    }

    public static uint? GetLastInputTime()
    {
        var lastInputInfo = new LASTINPUTINFO
        {
            cbSize = (uint) Marshal.SizeOf(typeof(LASTINPUTINFO))
        };

        if (GetLastInputInfo(ref lastInputInfo))
            return lastInputInfo.dwTime;

        return null;
    }

    public static uint MapVirtualKey3(uint uCode, uint uMapType, nint hKL)
    {
        if (hKL == nint.Zero)
            return MapVirtualKey(uCode, uMapType);

        return MapVirtualKeyEx(uCode, uMapType, hKL);
    }

    public static ushort VkKeyScan3(char ch, nint hKL)
    {
        if (hKL == nint.Zero)
            return VkKeyScan(ch);

        return VkKeyScanEx(ch, hKL);
    }

    public static string ToUnicode3(int vKey, byte[] pbKeyState, nint hKL)
    {
        var pState = nint.Zero;

        try
        {
            uint uScanCode = MapVirtualKey3((uint) vKey, 0, hKL);

            pState = Marshal.AllocHGlobal(256);
            if (pState == nint.Zero)
                return null;

            if (pbKeyState is null)
                return null;

            if (pbKeyState.Length != 256)
                return null;

            Marshal.Copy(pbKeyState, 0, pState, 256);

            var sbUni = new StringBuilder(32);

            var r = hKL == nint.Zero
                ? ToUnicode((uint) vKey, uScanCode, pState, sbUni, 30, 0)
                : ToUnicodeEx((uint) vKey, uScanCode, pState, sbUni, 30, 0, hKL);

            if (r < 0)
                return string.Empty;

            if (r == 0)
                return null;

            var str = sbUni.ToString();

            if (string.IsNullOrEmpty(str))
                return null;

            if (r < str.Length)
                str = str[..r];

            return str;
        }
        catch
        {
            throw;
        }
        finally
        {
            if (pState != nint.Zero)
                Marshal.FreeHGlobal(pState);
        }
    }

    public static bool IsTaskBar(nint hWnd)
    {
        Process process = null;

        try
        {
            string strText = GetWindowText(hWnd, true);
            if (strText is null)
                return false;

            if (!strText.Equals("start", StringComparison.OrdinalIgnoreCase))
                return false;

            GetWindowThreadProcessId(hWnd, out var processId);

            process = Process.GetProcessById((int) processId);

            var exe = Path.GetFileName(process.MainModule.FileName);
            return exe.Contains("explorer.exe", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            throw;
        }
        finally
        {
            process?.Dispose();
        }
    }

    static string GetWindowText(nint hWnd, bool bTrim)
    {
        var windowTextLength = GetWindowTextLength(hWnd);
        if (windowTextLength <= 0)
            return string.Empty;

        string result;
        var p = nint.Zero;

        try
        {
            var charSize = Marshal.SystemDefaultCharSize;
            var cb = (windowTextLength + 2) * charSize;

            p = Marshal.AllocCoTaskMem(cb);

            if (p == nint.Zero)
                return string.Empty;

            ZeroMemory(p, cb);

            int windowText = GetWindowText(hWnd, p, windowTextLength + 1);
            if (windowText <= 0)
                return string.Empty;

            if (windowText <= windowTextLength)
            {
                var ibZero = windowText * charSize;

                for (int i = 0; i < charSize; ++i)
                    Marshal.WriteByte(p, ibZero + i, 0);
            }
            else
            {
                return string.Empty;
            }

            result = Marshal.PtrToStringAuto(p) ?? string.Empty;
        }
        finally
        {
            if (p != nint.Zero)
                Marshal.FreeCoTaskMem(p);
        }

        return bTrim ? result.Trim() : result;
    }

    static byte[] g_pbZero = null;

    [MethodImpl(MIO.NoOptimization | MIO.NoInlining)]
    static void ZeroMemory(nint pb, long cb)
    {
        if (pb == nint.Zero)
            return;

        if (cb < 0)
            return;

        byte[] pbZero = g_pbZero;

        if (pbZero is null)
        {
            pbZero = new byte[4096];
            g_pbZero = pbZero;
        }

        var cbZero = pbZero.Length;

        while (cb != 0)
        {
            var cbBlock = System.Math.Min(cb, cbZero);

            Marshal.Copy(pbZero, 0, pb, (int) cbBlock);

            pb = AddPtr(pb, cbBlock);
            cb -= cbBlock;
        }
    }
    static nint AddPtr(nint p, long cb)
    {
        if (nint.Size >= 8)
            return new nint(unchecked(p.ToInt64() + cb));

        return new nint(unchecked(p.ToInt32() + (int) cb));
    }
}
