using System;
using System.IO;

namespace BitwardenAgent;

public static class Logger
{
    static string Now => DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss fff");

    static readonly string file = Path.Combine("logs", $"{Now}.log");

    public static void Log(object @object)
    {
        Directory.CreateDirectory("logs");

        File.AppendAllText(
            file,
            $"[{Now}] {@object ?? "null"}{Environment.NewLine}"
        );
    }
}
