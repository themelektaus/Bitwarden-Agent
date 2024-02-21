using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Security.Principal;

namespace BitwardenAgent;

public static class Utils
{
    public static bool IsAdmin()
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void StartAsAdmin(string exe)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = true,
            Verb = "runas"
        });
    }
}
