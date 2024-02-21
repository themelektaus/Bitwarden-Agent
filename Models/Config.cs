using System.Drawing;
using System.IO;

namespace BitwardenAgent.Models;

public class Config
{
 #if RELEASE
    static readonly string PATH = Path.Combine("data", "config.json");
#else
    const string PATH = "Config.json";
#endif

    static Config instance;
    public static Config Instance => instance ??= Load();

    public string url;
    public string username;

    public bool topMost;
    public bool developerMode;

    public Rectangle? bounds;
    public bool maximized;

    public string updateUrl = "https://steinalt.online/download/bitwarden-agent";

    Config() { }

    static Config Load()
    {
        if (!File.Exists(PATH))
            return new();

        var json = File.ReadAllText(PATH);
        return json.FromJson<Config>();
    }

    public void Save()
    {
        var json = this.ToJson();
#if RELEASE
        Directory.CreateDirectory("data");
#endif
        File.WriteAllText(PATH, json);
    }
}
