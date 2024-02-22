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

    public string password = string.Empty;

    public readonly List<Bitwarden_Item> items = new();

    public Update Update { get; private set; }

    Task updateCheckTask;

    int nextUpdateCheckCountdown;
    public int NextUpdateCheckCountdown
    {
        get => nextUpdateCheckCountdown;
        set
        {
            nextUpdateCheckCountdown = value;
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
        mainForm = new(this) { TopMost = Config.Instance.topMost };

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

            var config = Config.Instance;

            if (mainForm.WindowState == FormWindowState.Maximized)
            {
                config.maximized = true;
            }
            else
            {
                config.bounds = mainForm.Bounds;
                config.maximized = false;
            }

            config.Save();

            task.Wait();

            ;
        }
    }

    public bool IsReadyToLogin(bool includePassword)
    {
        if (string.IsNullOrWhiteSpace(Config.Instance.url))
            return false;

        if (string.IsNullOrEmpty(Config.Instance.username))
            return false;

        if (includePassword)
            if (string.IsNullOrEmpty(password))
                return false;

        return true;
    }

    public async Task Sync()
    {
        if (password == string.Empty)
            return;

        using var status = AddStatus("Getting Items");

        var client = await GetClient();

        if (client is not null)
        {
            var _items = await client.GetItems();

            if (_items is null)
            {
                await TempStatus("Getting Items failed :(");
            }
            else
            {
                items.Clear();

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

                    items.Add(item);
                }
            }
        }
    }

    public async Task Logout()
    {
        password = string.Empty;

        items.Clear();

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



    Client client;

    async Task<Client> GetClient()
    {
        using var status = AddStatus("Login");

        var hasConnection = false;

        if (client is not null)
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

            await client.Login(password);

            hasConnection = await client.HasConnection();
        }

        if (!hasConnection)
        {
            await client.Logout();
            client = null;

            await TempStatus("Login failed :(");
        }

        return client;
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
        using var status = AddStatusInternal(message);
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
}
