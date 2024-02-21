using System;
using System.Windows.Forms;

using Mutex = System.Threading.Mutex;

#if RELEASE
using System.Linq;

using Directory = System.IO.Directory;
using Assembly = System.Reflection.Assembly;
using DllImportSearchPath = System.Runtime.InteropServices.DllImportSearchPath;
using NativeLibrary = System.Runtime.InteropServices.NativeLibrary;
#endif

namespace BitwardenAgent;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
#if RELEASE
        if (AppInfo.currentProcessName.Contains(
                "update",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            Update.Apply();
            return;
        }

        if (args.FirstOrDefault() == "publish")
        {
            Update.Publish(
                publicFolder: args[1],
                version: args[2]
            );
            return;
        }

        Update.Post();
#endif

        var mutex = new Mutex(true, AppInfo.currentProcessName, out var createdNew);

        if (!createdNew)
            return;

#if RELEASE
        if (!Utils.IsAdmin())
        {
            Utils.StartAsAdmin(AppInfo.currentProcessExeName);
            return;
        }

        var variable = "NODE_TLS_REJECT_UNAUTHORIZED";
        var value = "0";
        var target = EnvironmentVariableTarget.Machine;

        if (Environment.GetEnvironmentVariable(variable, target) != value)
        {
            Environment.SetEnvironmentVariable(variable, value, target);

            Utils.StartAsAdmin(AppInfo.currentProcessExeName);
            return;
        }

        foreach (var dll in Directory.EnumerateFiles("lib", "*.dll"))
            LoadDll(dll);
#endif

        ApplicationConfiguration.Initialize();

        using (var app = new App())
        {
#if RELEASE
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Logger.Log(e.ExceptionObject);
                Environment.Exit(0);
            };
#endif

            app.mainForm.ShowDialog();

            var icon = new NotifyIcon
            {
                Icon = Properties.Resources.Icon,
                Visible = true
            };

            icon.MouseDown += (_, _) => app.mainForm.TryShowDialog();

            app.mainForm.TryShowDialog();

            Application.Run();

            icon.Visible = false;
        }

        GC.KeepAlive(mutex);
    }

#if RELEASE
    static void LoadDll(string dll)
    {
        NativeLibrary.TryLoad(
            dll,
            Assembly.GetExecutingAssembly(),
            DllImportSearchPath.SafeDirectories | DllImportSearchPath.UserDirectories,
            out _
        );

        Logger.Log($"Load DLL: {dll}"); ;
    }
#endif
}
