using System;
using System.Diagnostics;
using System.IO;

namespace BitwardenAgent;

public static class AppInfo
{
    static string name;
    public static string Name => name ??= AppDomain.CurrentDomain.FriendlyName;

    static string updateName;
    public static string UpdateName => updateName ??= $"{Name} Update";

    static string version;
    public static string Version => version ??= typeof(AppInfo).Assembly
        .GetName()?.Version?.ToString() ?? "0.0.0.0";

    static Process process;
    public static Process Process => process ??= Process.GetCurrentProcess();

    static string processName;
    public static string ProcessName => processName ??= Process.ProcessName;

    static ProcessModule mainModule;
    static ProcessModule MainModule => mainModule ??= Process.MainModule;

    static FileInfo exeInfo;
    public static FileInfo ExeInfo => exeInfo ??= new(MainModule.FileName);
}
