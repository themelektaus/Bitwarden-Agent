using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows.Forms;

namespace BitwardenAgent;

public static class Utils
{
#if DEBUG
    public const bool DEBUG = true;
#else
    public const bool DEBUG = false;
#endif

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

    public static bool IsReadyToLogin(string password)
    {
        if (string.IsNullOrWhiteSpace(Config.Instance.url))
            return false;

        if (string.IsNullOrEmpty(Config.Instance.username))
            return false;

        if (password is not null && password == string.Empty)
            return false;

        return true;
    }

    public static List<string> GetDataFileNames()
        => Directory.Exists("data")
            ? Directory
                .EnumerateFiles("data", "*.dat")
                .Select(Path.GetFileNameWithoutExtension)
                .ToList()
            : new();

    public static OpenFileDialog CreateOpenFileDialog(string fileName)
    {
        var dialog = new OpenFileDialog();

        if (!string.IsNullOrEmpty(fileName))
        {
            var file = new FileInfo(fileName);

            if (file.Exists)
            {
                dialog.InitialDirectory = file.DirectoryName;
                dialog.FileName = file.Name;
            }
            else if (Directory.Exists(fileName))
            {
                dialog.InitialDirectory = fileName;
            }
        }

        return dialog;
    }

}
