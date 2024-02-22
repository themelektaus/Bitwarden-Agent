#if RELEASE
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BitwardenAgent;

public partial class Update
{
    public static async Task Prepare()
    {
        RecreateDirectory("temp");

        using (var httpClient = new HttpClient())
        {
            using var stream = await Config.Instance.DownloadFileStream("data.zip");

            await Task.Run(() =>
            {
                using var zip = new ZipArchive(stream);
                zip.ExtractToDirectory("temp");
            });
        }

        RecreateDirectory("backup");

        await Task.Run(() =>
        {
            var s = Path.DirectorySeparatorChar;

            Copy(
                sourceDirectory: ".",
                destinationDirectory: "backup",
                exclusions: [
                    $"{s}backup",
                    $"{s}files",
                    $"{s}logs",
                    $"{s}temp"
                ]
            );

            CopyFile(AppInfo.mainExeName, AppInfo.updateExeName);
        });

        Utils.StartAsAdmin(AppInfo.updateExeName);
    }

    public static void Apply()
    {
        try
        {
            while (Process.GetProcessesByName(AppInfo.name).Length > 0)
                Thread.Sleep(1000);

            if (File.Exists(AppInfo.mainPdbName))
                DeleteFile(AppInfo.mainPdbName);

            Copy(
                sourceDirectory: "temp",
                destinationDirectory: ".",
                exclusions: []
            );

            DeleteDirectory("temp");

            Utils.StartAsAdmin(AppInfo.mainExeName);
        }
        catch (Exception ex)
        {
            Logger.Log(ex);
        }
    }

    public static void Post()
    {
        try
        {
            if (File.Exists(AppInfo.updateExeName))
                DeleteFile(AppInfo.updateExeName);

            if (File.Exists(AppInfo.mainPdbName))
                File.SetAttributes(AppInfo.mainPdbName, FileAttributes.Hidden);
        }
        catch (Exception ex)
        {
            Logger.Log(ex);
        }
    }

    public static void Publish(string publicFolder, string version)
    {
        var buildPath = Path.Combine(Environment.CurrentDirectory, "Build");
        if (!Directory.Exists(buildPath))
            return;

        Environment.CurrentDirectory = buildPath;

        if (Directory.Exists("wwwroot"))
        {
            if (Directory.Exists("web"))
                DeleteDirectory("web");

            MoveDirectory("wwwroot", "web");
        }

        foreach (var file in Directory.EnumerateFiles("web", "*.scss"))
            DeleteFile(file);

        RecreateDirectory("lib");

        foreach (var dll in Directory.EnumerateFiles(".", "*.dll"))
            MoveFile(dll, Path.Combine("lib", dll));

        if (File.Exists("data.zip"))
            DeleteFile("data.zip");

        using (var dataZip = ZipFile.Open("data.zip", ZipArchiveMode.Create))
        {
            dataZip.CreateEntryFromFile(AppInfo.mainExeName, AppInfo.mainExeName);
            dataZip.CreateEntryFromFile(AppInfo.mainPdbName, AppInfo.mainPdbName);

            foreach (var file in Directory.EnumerateFiles("lib"))
                dataZip.CreateEntryFromFile(file, file);

            foreach (var file in Directory.EnumerateFiles("web"))
                dataZip.CreateEntryFromFile(file, file);
        }

        MoveFile("data.zip", Path.Combine(publicFolder, "data.zip"));
        File.WriteAllText(Path.Combine(publicFolder, "version.txt"), version);
    }

    static void RecreateDirectory(string name)
    {
        if (Directory.Exists(name))
            DeleteDirectory(name);

        CreateDirectory(name);
    }

    static void Copy(string sourceDirectory, string destinationDirectory, string[] exclusions)
    {
        var searchOption = SearchOption.AllDirectories;

        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", searchOption))
        {
            var _directory = directory[sourceDirectory.Length..];
            if (exclusions.Any(_directory.StartsWith))
            {
                Logger.Log($"Skipping: {directory}");
                continue;
            }

            _directory = destinationDirectory + _directory;
            if (!Directory.Exists(_directory))
                CreateDirectory(_directory);
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*.*", searchOption))
        {
            var _file = file[sourceDirectory.Length..];
            if (exclusions.Contains(_file))
            {
                Logger.Log($"Skipping: {file}");
                continue;
            }

            var _directory = Path.GetDirectoryName(_file);
            if (exclusions.Any(_directory.StartsWith))
            {
                Logger.Log($"Skipping: {file}");
                continue;
            }

            CopyFile(file, destinationDirectory + _file);
        }
    }

    static void CreateDirectory(string name)
    {
        Logger.Log($"Create Directory: {name}");
        Directory.CreateDirectory(name);
    }

    static void MoveDirectory(string sourceDirectory, string destinationDirectory)
    {
        Logger.Log($"Move Directory: {sourceDirectory} => {destinationDirectory}");
        Directory.Move("wwwroot", "web");
    }

    static void DeleteDirectory(string name)
    {
        Logger.Log($"Delete Directory: {name}");
        Directory.Delete(name, true);
    }

    static void CopyFile(string sourceFile, string destinationFile)
    {
        Logger.Log($"Copy File: {sourceFile} => {destinationFile}");
        File.Copy(sourceFile, destinationFile, true);
    }

    static void MoveFile(string sourceFile, string destinationFile)
    {
        Logger.Log($"Move File: {sourceFile} => {destinationFile}");
        File.Move(sourceFile, destinationFile, true);
    }

    static void DeleteFile(string name)
    {
        Logger.Log($"Delete File: {name}");
        File.Delete(name);
    }
}
#endif
