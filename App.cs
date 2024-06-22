using BitwardenAgent.Web;
using BitwardenAgent.Web.Components;
using BitwardenAgent.Web.Pages;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BitwardenAgent;

public class App : IDisposable
{
    public static App Instance { get; private set; }

    public MainForm mainForm;

    public Bitwarden_Data data = new();

    public Update Update { get; private set; }

    Task updateCheckTask;

    int nextUpdateCheckCountdown;
    public int NextUpdateCheckCountdown
    {
        get => nextUpdateCheckCountdown;
        set
        {
            nextUpdateCheckCountdown = value;

            if (mainForm.Visible)
                pages.debug?.RenderLater();
        }
    }

    public class Components
    {
        public Root root;
        public Menu menu;
    }
    public readonly Components components = new();

    public class Pages
    {
        public Page_Database database;
        public Page_Debug debug;
    }
    public readonly Pages pages = new();

    public App()
    {
        Instance = this;
        mainForm = new() { TopMost = Config.Instance.topMost };

        updateCheckTask = Task.Run(async () =>
        {
            while (components.root is null)
            {
                await Task.Delay(1);
            }

        Loop:
            await Task.Delay(1000);
            await CheckForUpdates();

            NextUpdateCheckCountdown = 900;
            while (NextUpdateCheckCountdown > 0)
            {
                if (updateCheckTask is null)
                    return;

                NextUpdateCheckCountdown--;
                await Task.Delay(1000);
            }
            goto Loop;
        });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            var task = updateCheckTask;

            updateCheckTask = null;

            Config.Instance.Save();

            task.Wait();
        }
    }

    public bool IsConnected() => client is not null;

    public async Task Login(string password)
    {
        var url = Config.Instance.url;
        var username = Config.Instance.username;

        await Login(url, username, password);
    }

    public async Task Sync()
    {
        data = new() { collections = [], items = [] };

        using var status1 = AddStatus("Load Collections...");
        
        var _collections = await client.GetCollections();

        if (_collections is null)
        {
            await TempStatus("Getting Collections failed :(");
        }
        else
        {
            foreach (var collection in _collections)
            {
                data.collections.Add(collection);
            }
        }

        using var status2 = AddStatus("Load Items...");
        
        var _items = await client.GetItems();

        if (_items is null)
        {
            await TempStatus("Getting Items failed :(");
        }
        else
        {
            foreach (var item in _items)
            {
                if (item.type != Bitwarden_Item.Type.Login)
                    continue;

                var login = item.login;
                if (login is null)
                    continue;

                if (login.username == string.Empty)
                    if (login.password == string.Empty)
                        continue;

                item.LoadCollections(data.collections);

                data.items.Add(item);
            }
        }
    }

    public async Task Logout()
    {
        if (client is null)
            return;

        using var status = AddStatus("Logout");

        await client.Logout();
        client = null;
    }

    public async Task CheckForUpdates()
    {
        bool IsUpdateAvailable()
            => Update?.available ?? false;

        var updateAvailable = IsUpdateAvailable();

        Update = components.root is null
            ? null
            : await Update.Check();

        if (components.root is null)
            return;

        if (updateAvailable != IsUpdateAvailable())
            components.root.RenderLater();
    }

    public async Task PerformUpdate()
    {
        using var status = AddStatus($"Downloading v{Update.remoteVersion}");

        await Update.Prepare();

        await Exit();
    }

    public async Task Exit()
    {
        using var status = AddStatus("Exiting");

        await Logout();

        mainForm.Close();

        Application.Exit();
    }

    public async Task PerformAutoType(string keyString)
    {
        using var status = AddStatus("Performing Auto Type");

        keyString = AutoType.Escape(keyString);
        keyString = AutoType.Encode($"{keyString}\n");
        AutoType.PerformIntoPreviousWindow(mainForm.Handle, keyString);

        await Task.CompletedTask;
    }

    public async Task PerformAutoType(string username, string password)
    {
        using var status = AddStatus("Performing Auto Type");

        username = AutoType.Escape(username);
        password = AutoType.Escape(password);
        var keyString = AutoType.Encode($"{username}\t{password}\n");
        AutoType.PerformIntoPreviousWindow(mainForm.Handle, keyString);

        await Task.CompletedTask;
    }

    public async Task<bool> DownloadFile(string file)
    {
        using var status = AddStatus($"Downloading {file}");

        if (await Config.Instance.DownloadFile(file))
        {
            await TempStatus($"Download of {file} successful");
            return true;
        }

        await TempStatus($"Error: Download of {file} failed");
        return false;
    }



    Client client;

    async Task Login(string url, string username, string password)
    {
        using var status = AddStatus("Connecting...");

        var hasConnection = false;

        if (IsConnected())
        {
            if (client.CredentialsEquals(password))
                hasConnection = await client.HasConnection();

            if (!hasConnection)
            {
                await client.Logout();
                client = null;
            }
        }

        if (client is null)
        {
            client = new();

            await client.Login(url, username, password);

            hasConnection = await client.HasConnection();
        }

        if (hasConnection)
            return;
        
        await client.Logout();
        client = null;

        await TempStatus("Connection could not be established. :(");
    }



    readonly List<Status> statusList = new();

    public class Status : IDisposable
    {
        public string message;
        public event Action OnDispose;
        public void Dispose() => OnDispose?.Invoke();
        public override string ToString() => message;
    }

    public Status GetCurrentStatus()
    {
        return statusList.LastOrDefault();
    }

    public bool IsBusy()
    {
        return GetCurrentStatus() is not null;
    }

    public Status AddStatus(string message)
    {
        return AddStatusInternal(message);
    }

    public async Task TempStatus(string message)
    {
        using var _ = AddStatusInternal(message);
        await Task.Delay(3000);
    }

    Status AddStatusInternal(string message)
    {
        var status = new Status { message = message };

        status.OnDispose += () =>
        {
            statusList.Remove(status);
            components.root.RenderLater();
        };

        statusList.Add(status);
        components.root.RenderLater();

        return status;
    }

    public async Task<string> ShowOpenFileDialog(string fileName)
    {
        using var status = AddStatus("Opening File...");

        using var dialog = Utils.CreateOpenFileDialog(fileName);

        var dialogResult = await dialog.ShowDialogAsync();
        if (dialogResult == DialogResult.OK)
            return dialog.FileName;

        return null;
    }
}
