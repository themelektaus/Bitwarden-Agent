using System.IO;
using System.Linq;
using System.Threading.Tasks;


#if RELEASE
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;

using Env = System.Environment;
using Ex = System.Exception;
#endif

namespace BitwardenAgent;

public static class Update
{
    public static async Task<(bool shouldUpdate, string message)> Check()
    {
#if RELEASE
        if (!int.TryParse(AppInfo.Version.Replace(".", string.Empty), out var localVersion))
            return (false, "Error: Unknown local version");

        using var httpClient = new HttpClient();
        
        var version = await httpClient.GetStringAsync(
            $"{Config.Instance.updateUrl.TrimEnd('/')}/version.txt"
        );

        if (version is null)
            return (false, "Error: Remote version is null");

        if (version == string.Empty)
            return (false, "Error: Remote version is empty");

        if (!int.TryParse(version.Replace(".", string.Empty), out var _version))
            return (false, $"Error: Can not parse remote version \"{version}\"");
        
        if (_version == 0)
            return (false, $"Error: Parsed remote version is 0");
        
        if (_version == localVersion)
            return (false, $"No Update available");
        
        return (true, $"Downloading v{version}");
#else
        return await Task.FromResult((false, "Not possible in debug mode"));
#endif
    }

    public static async Task Prepare()
    {
        RecreateDirectory("temp");

#if RELEASE
        using (var httpClient = new HttpClient())
        {
            using var stream = await httpClient.GetStreamAsync(
                $"{Config.Instance.updateUrl.TrimEnd('/')}/data.zip"
            );

            await Task.Run(() =>
            {
                using var zip = new ZipArchive(stream);
                zip.ExtractToDirectory("temp");
            });
        }
#else
        await Task.CompletedTask;
#endif

        RecreateDirectory("backup");

        await Task.Run(() =>
        {
            var s = Path.DirectorySeparatorChar;

            Copy(
                sourceDirectory: ".",
                destinationDirectory: "backup",
                exclusions: [
                    $"{s}backup",
                    $"{s}lib{s}bw.exe",
                    $"{s}logs",
                    $"{s}temp"
                ]
            );

            CopyFile(AppInfo.ExeInfo.Name, $"{AppInfo.UpdateName}.exe");
        });

        Utils.StartAsAdmin($"{AppInfo.UpdateName}.exe");
    }

#if RELEASE
    public static void Apply()
    {
        try
        {
            while (Process.GetProcessesByName(AppInfo.Name).Length > 0)
                Thread.Sleep(1000);

            if (File.Exists($"{AppInfo.Name}.pdb"))
                DeleteFile($"{AppInfo.Name}.pdb");

            Copy(
                sourceDirectory: "temp",
                destinationDirectory: ".",
                exclusions: []
            );

            DeleteDirectory("temp");

            Utils.StartAsAdmin($"{AppInfo.Name}.exe");
        }
        catch (Ex ex)
        {
            Logger.Log(ex);
        }
    }

    public static void Post()
    {
        try
        {
            if (File.Exists($"{AppInfo.UpdateName}.exe"))
                DeleteFile($"{AppInfo.UpdateName}.exe");

            if (File.Exists($"{AppInfo.Name}.pdb"))
                File.SetAttributes($"{AppInfo.Name}.pdb", FileAttributes.Hidden);
        }
        catch (Ex ex)
        {
            Logger.Log(ex);
        }
    }

    public static void Publish(string publicFolder, string version)
    {
        var buildPath = Path.Combine(Env.CurrentDirectory, "Build");
        if (!Directory.Exists(buildPath))
            return;

        Env.CurrentDirectory = buildPath;

        if (Directory.Exists("wwwroot"))
        {
            if (Directory.Exists("web"))
                DeleteDirectory("web");

            MoveDirectory("wwwroot", "web");
        }

        RecreateDirectory("lib");

        MoveFile("bw.exe", Path.Combine("lib", "bw.exe"));

        foreach (var dll in Directory.EnumerateFiles(".", "*.dll"))
            MoveFile(dll, Path.Combine("lib", dll));

        var x = AppInfo.Name;

        if (File.Exists("data.zip"))
            DeleteFile("data.zip");

        using (var dataZip = ZipFile.Open("data.zip", ZipArchiveMode.Create))
        {
            dataZip.CreateEntryFromFile($"{x}.exe", $"{x}.exe");
            dataZip.CreateEntryFromFile($"{x}.pdb", $"{x}.pdb");

            foreach (var file in Directory.EnumerateFiles("lib"))
                dataZip.CreateEntryFromFile(file, file);

            foreach (var file in Directory.EnumerateFiles("web"))
                dataZip.CreateEntryFromFile(file, file);
        }

        MoveFile("data.zip", Path.Combine(publicFolder, "data.zip"));
        File.WriteAllText(Path.Combine(publicFolder, "version.txt"), version);
    }
#endif

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
#if RELEASE
        Directory.CreateDirectory(name);
#endif
    }

    static void MoveDirectory(string sourceDirectory, string destinationDirectory)
    {
        Logger.Log($"Move Directory: {sourceDirectory} => {destinationDirectory}");
#if RELEASE
        Directory.Move("wwwroot", "web");
#endif
    }

    static void DeleteDirectory(string name)
    {
        Logger.Log($"Delete Directory: {name}");
#if RELEASE
        Directory.Delete(name, true);
#endif
    }

    static void CopyFile(string sourceFile, string destinationFile)
    {
        Logger.Log($"Copy File: {sourceFile} => {destinationFile}");
#if RELEASE
        File.Copy(sourceFile, destinationFile, true);
#endif
    }

    static void MoveFile(string sourceFile, string destinationFile)
    {
        Logger.Log($"Move File: {sourceFile} => {destinationFile}");
#if RELEASE
        File.Move(sourceFile, destinationFile, true);
#endif
    }

    static void DeleteFile(string name)
    {
        Logger.Log($"Delete File: {name}");
#if RELEASE
        File.Delete(name);
#endif
    }
}
