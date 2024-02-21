using Process = System.Diagnostics.Process;

namespace BitwardenAgent;

public sealed class AutoType_WindowInfo
{
    public enum SendMethod
    {
        Default,
        KeyEvent,
        UnicodePacket
    }

    static readonly string[] processNames_Unicode = [
        "PuTTY",
        "KiTTY",
        "KiTTY_Portable",
        "KiTTY_NoTrans",
        "KiTTY_NoHyperlink",
        "KiTTY_NoCompress",
        "PuTTYjp",
        "MinTTY"
    ];

    static readonly string[] processNames_VM = [
        "MSTSC",
        "VirtualBox",
        "VirtualBoxVM",
        "VpxClient",
        "VMware-VMX",
        "VMware-AuthD",
        "VMPlayer",
        "VMware-Unity-Helper",
        "VMware",
        "VMRC",
        "VMware-View",
        "DWRCC",
        "KaseyaLiveConnect"
    ];

    public readonly nint keyboardLayout = nint.Zero;
    public readonly SendMethod sendMethod;
    public readonly bool charsRAltAsCtrlAlt;
    public readonly int sleepAroundKeyMod = 1;

    public AutoType_WindowInfo(nint windowHandle)
    {
        if (windowHandle == nint.Zero)
            return;

        keyboardLayout = AutoType_NativeMethods.GetKeyboardLayout(
            AutoType_NativeMethods.GetWindowThreadProcessId(windowHandle, out var processId)
        );

        var process = Process.GetProcessById((int) processId);
        if (process is not null)
        {
            InitByProcessName(
                GetProcessName(process),
                ref sendMethod,
                ref charsRAltAsCtrlAlt,
                ref sleepAroundKeyMod
            );
            process.Dispose();
        }
    }

    static void InitByProcessName(
        string processName,
        ref SendMethod sendMethod,
        ref bool charsRAltAsCtrlAlt,
        ref int sleepAroundKeyMod
    )
    {
        foreach (string name in processNames_Unicode)
        {
            if (ProcessNameMatches(processName, name))
            {
                sendMethod = SendMethod.UnicodePacket;
                return;
            }
        }

        foreach (string name in processNames_VM)
        {
            if (ProcessNameMatches(processName, name))
            {
                sendMethod = SendMethod.KeyEvent;
                charsRAltAsCtrlAlt = true;
                sleepAroundKeyMod = 50;
                return;
            }
        }
    }

    public static string GetProcessName(Process process)
    {
        return (process?.ProcessName ?? string.Empty).Trim();
    }

    public static bool ProcessNameMatches(string a, string b)
    {
        if (a is null || b is null)
            return false;

        if (a.Equals(b))
            return true;

        if (a.Equals($"{b}.exe"))
            return true;

        return false;
    }
}
