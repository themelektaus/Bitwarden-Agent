using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BitwardenAgent;

public class Client()
{
    string url;
    string username;
    string password;

    string session = string.Empty;

    public async Task Login(string url, string username, string password)
    {
        await Logout();

        if (string.IsNullOrEmpty(url))
            return;

        if (string.IsNullOrEmpty(username))
            return;

        if (string.IsNullOrEmpty(password))
            return;

        this.url = url;
        this.username = username;
        this.password = password;

        await ExecuteBw($"config server {url}");

        var result = await ExecuteBw($"login {username} {password}");

        var lines = result.Output.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries
        );

        foreach (var line in lines)
        {
            var session = line.Split("bw list items --session");
            if (session.Length != 2)
                continue;

            this.session = session[1].Trim();
            break;
        }
    }

    public bool CredentialsEquals(string password)
    {
        if (url != Config.Instance.url)
            return false;

        if (username != Config.Instance.username)
            return false;

        if (this.password != password)
            return false;

        return true;
    }

    public async Task Logout()
    {
        session = string.Empty;

        try { await ExecuteBw("logout"); } catch { }
    }

    public async Task<bool> HasConnection()
    {
        if (session == string.Empty)
            return false;

        var json = (await ExecuteBw("status")).Output;

        if (json is null)
            return false;

        if (!json.ValidateJson())
            return false;

        var status = json.FromJson<Bitwarden_Status>();
        if (status.status == "unauthenticated")
            return false;

        return true;
    }

    public async Task<List<Bitwarden_Item>> GetItems(
        string search = "",
        string folderId = "",
        string collectionId = ""
    )
    {
        var cmd = new StringBuilder();

        cmd.Append("list items");

        if (!string.IsNullOrEmpty(search))
            cmd.Append($" --search \"{search}\"");

        if (!string.IsNullOrEmpty(folderId))
            cmd.Append($" --folderid {folderId}");

        if (!string.IsNullOrEmpty(collectionId))
            cmd.Append($" --collectionid  {collectionId}");

        return await ExecuteBwWithSession<List<Bitwarden_Item>>(cmd);
    }

    public async Task<List<Bitwarden_Organisation>> GetOrganisations()
        => await ExecuteBwWithSession<List<Bitwarden_Organisation>>($"list organizations");

    public async Task<List<Bitwarden_Collection>> GetCollections()
        => await ExecuteBwWithSession<List<Bitwarden_Collection>>($"list collections");

    public async Task<bool> DeleteItem(string itemId, string organizationId = "")
    {
        var cmd = new StringBuilder();

        cmd.Append($"delete item {itemId}");

        if (!string.IsNullOrEmpty(organizationId))
            cmd.Append($" --organizationid {organizationId}");

        var json = await ExecuteBwWithSession_Internal(cmd);

        if (json is null)
            return false;

        if (!json.ValidateJson())
            return false;

        return true;
    }

    public async Task<Bitwarden_Item> CreateLogin(
        string organizationId,
        string collectionId,
        string name,
        string username,
        string password,
        string uri,
        string notes
    )
    {
        return await SendAndExecuteBwWithSession<Bitwarden_Item>("create item", new Bitwarden_NewItem
        {
            type = Bitwarden_Item.Type.Login,
            organizationId = organizationId,
            collectionId = collectionId,
            collectionIds = new() { collectionId },
            name = name,
            login = new()
            {
                username = username,
                password = password,
                uris = new() { new() { uri = uri } }
            },
            fields = new()
            {
                new()
                {
                    name = "Security Question",
                    value = "Bitwarden Rules",
                    type = Bitwarden_Field.Type.Text
                }
            },
            notes = notes
        });
    }

    public async Task<Bitwarden_SecureNote> CreateSecureNote(
        string organizationId,
        string collectionId,
        string name,
        string notes
    )
    {
        return await SendAndExecuteBwWithSession<Bitwarden_SecureNote>("create item", new Bitwarden_SecureNote
        {
            type = Bitwarden_Item.Type.SecureNote,
            organizationId = organizationId,
            collectionId = collectionId,
            collectionIds = new() { collectionId },
            name = name,
            notes = notes
        });
    }

    public async Task<(bool result, string message, Bitwarden_Item item)> GetItem(string itemId)
    {
        var json = await ExecuteBwWithSession_Internal($"get item {itemId}");

        if (json is not null && json.ValidateJson())
        {
            var item = json.FromJson<Bitwarden_Item>();
            return (true, "Success", item);
        }

        return (false, json, null);
    }

    public async Task<bool> EditItem(Bitwarden_Item item)
    {
        var json = await SendAndExecuteBwWithSession_Internal("edit item", item);

        if (json is null)
            return false;

        if (!json.ValidateJson())
            return false;

        return true;
    }

    public async Task<string> CreateAttachment(string itemId, string file)
    {
        return await ExecuteBwWithSession_Internal(
            $"create attachment --itemid {itemId} --file \"{file}\""
        );
    }

    public async Task<string> DownloadAttachment(
        string attachmentId,
        string itemId,
        string output = ""
    )
    {
        var cmd = new StringBuilder();
        cmd.Append($"get attachment \"{attachmentId}\"");
        cmd.Append($" --itemid {itemId}");

        if (!string.IsNullOrEmpty(output))
            cmd.Append($" --output \"{output}\"");

        return await ExecuteBwWithSession_Internal(cmd);
    }

    public async Task<string> DeleteAttachment(
        string attachmentId,
        string itemId,
        string organizationId = ""
    )
    {
        var builder = new StringBuilder();
        builder.Append($"delete attachment \"{attachmentId}\"");
        builder.Append($" --itemid {itemId}");

        if (!string.IsNullOrEmpty(organizationId))
            builder.Append($" --organizationid \"{organizationId}\"");

        return await ExecuteBwWithSession_Internal(builder);
    }

    async Task<T> ExecuteBwWithSession<T>(StringBuilder command)
        => await ExecuteBwWithSession<T>(command.ToString());

    async Task<T> ExecuteBwWithSession<T>(string command)
        => (await ExecuteBwWithSession_Internal(command)).FromJson<T>();

    async Task<string> ExecuteBwWithSession_Internal(StringBuilder command)
        => await ExecuteBwWithSession_Internal(command.ToString());

    async Task<string> ExecuteBwWithSession_Internal(string command)
    {
        var result = await Execute(
            GetExe(),
            command + $" --session {session}"
        );

        return result.Output;
    }

    async Task<T> SendAndExecuteBwWithSession<T>(string command, object data)
        => (await SendAndExecuteBwWithSession_Internal(command, data)).FromJson<T>();

    async Task<string> SendAndExecuteBwWithSession_Internal(string command, object data)
    {
        var result = await ExecuteCmd(
            $"echo {Base64EncodeJson(data)}" +
            $" | \"{GetExe()}\" {command} --session \"{session}\""
        );

        return result.Output;
    }

    static async Task<ExecutionResult> ExecuteBw(string command)
    {
        return await Execute(GetExe(), command);
    }

    static async Task<ExecutionResult> ExecuteCmd(string command)
    {
        return await Execute("cmd.exe", $"/c {command}");
    }

    record ExecutionResult(string Output, string Error);

    static async Task<ExecutionResult> Execute(string fileName, string arguments)
    {
        var p = new Process();

        var s = p.StartInfo;
        {
            s.FileName = fileName;
            s.Arguments = arguments;
            s.UseShellExecute = false;
            s.CreateNoWindow = true;
            s.ErrorDialog = false;
            s.RedirectStandardOutput = true;
            s.RedirectStandardError = true;
            s.StandardOutputEncoding = Encoding.UTF8;
            s.StandardErrorEncoding = Encoding.UTF8;
        }

        var output = new StringBuilder();
        p.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
                output.AppendLine(e.Data);
        };

        var error = new StringBuilder();
        p.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
                error.AppendLine(e.Data);
        };

        p.Start();
        p.BeginErrorReadLine();
        p.BeginOutputReadLine();
        await p.WaitForExitAsync();

        var result = new ExecutionResult(
            output.ToString().Trim(),
            error.ToString().Trim()
        );

        return result;
    }

    static string GetExe()
    {
        if (!File.Exists(AppInfo.bwExe))
        {
            throw new(
                $"{AppInfo.bwExe} not found. " +
                $"Before start, please download " +
                $"the last version of Bitwarden CLI (BW) " +
                $"from https://bitwarden.com/help/cli/"
            );
        }

        return AppInfo.bwExe;
    }

    static string Base64EncodeJson<T>(T data)
    {
        if (data is not string json)
            json = data.ToJson();
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }
}
