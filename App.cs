using BitwardenAgent.Web;
using BitwardenAgent.Web.Components;
using BitwardenAgent.Web.Pages;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BitwardenAgent;

public class App : IDisposable
{
    public static App Instance { get; private set; }

    public MainForm mainForm;

    public string password = string.Empty;

    public readonly List<Bitwarden_Item> items = new();

    public event EventHandler StatusChanged;

    public class Components
    {
        public Root root;
        public Menu menu;
    }
    public readonly Components components = new();

    public class Pages
    {
        public Page_Welcome welcome;
        public Page_Database database;
        public Page_Settings settings;
    }
    public readonly Pages pages = new();

    public App()
    {
        Instance = this;
        mainForm = new(this) { TopMost = Config.Instance.topMost };
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
            if (mainForm.WindowState == FormWindowState.Maximized)
            {
                Config.Instance.maximized = true;
            }
            else
            {
                Config.Instance.bounds = mainForm.Bounds;
                Config.Instance.maximized = false;
            }

            Config.Instance.Save();
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

        await SetStatus("Getting Items");

        var client = await GetClient();

        if (client is not null)
        {
            var _items = await client.GetItems();

            if (_items is null)
            {
                await SetTempStatus("Getting Items failed :(");
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

        await SetStatus(null);
    }

    public async Task Logout()
    {
        password = string.Empty;

        items.Clear();

        if (client is null)
            return;

        await SetStatus("Logout");

        await client.Logout();
        client = null;

        await SetStatus(null);
    }

    public async Task Update()
    {
        await SetStatus("Checking");

        var (shouldUpdate, message) = await BitwardenAgent.Update.Check();

        if (shouldUpdate)
        {
            await SetStatus(message);
            await BitwardenAgent.Update.Prepare();
            await Exit();
            return;
        }

        await SetTempStatus(message);
        await SetStatus(null);
    }

    public async Task Exit()
    {
        await SetStatus("Exiting");

        await Logout();

        mainForm.Close();

        Application.Exit();
    }

    public async Task PerformAutoType(string keyString)
    {
        await SetStatus("Performing Auto Type");

        keyString = AutoType.Escape(keyString);
        keyString = AutoType.Encode($"{keyString}\n");
        AutoType.PerformIntoPreviousWindow(mainForm.Handle, keyString);

        await SetStatus(null);
    }

    public async Task PerformAutoType(string username, string password)
    {
        await SetStatus("Performing Auto Type");

        username = AutoType.Escape(username);
        password = AutoType.Escape(password);
        var keyString = AutoType.Encode($"{username}\t{password}\n");
        AutoType.PerformIntoPreviousWindow(mainForm.Handle, keyString);

        await SetStatus(null);
    }



    Client client;

    async Task<Client> GetClient()
    {
        await SetStatus("Login");

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

            await SetTempStatus("Login failed :(");
        }

        await SetStatus(null);

        return client;
    }



    readonly Stack<string> statusStack = new();

    public string GetStatus()
    {
        return statusStack.TryPeek(out var x) ? x : null;
    }

    public bool IsBusy()
    {
        return GetStatus() is not null;
    }

    public async Task SetStatus(string value)
    {
        if (value is null)
            statusStack.Pop();
        else
            statusStack.Push(value);

        StatusChanged?.Invoke(this, EventArgs.Empty);
        await Task.Delay(1);
    }

    public async Task SetTempStatus(string value)
    {
        await SetStatus(value);
        await Task.Delay(3000);
        await SetStatus(null);
    }

    public async Task ClearStatus()
    {
        if (statusStack.Count == 0)
            return;

        statusStack.Clear();
        StatusChanged?.Invoke(this, EventArgs.Empty);
        await Task.Delay(1);
    }
}
