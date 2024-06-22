using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;

using System.Drawing;
using System.Windows.Forms;

using Task = System.Threading.Tasks.Task;

namespace BitwardenAgent;

public partial class MainForm : Form
{
    public bool Ready { get; private set; }

    BlazorWebView blazorWebView;

    public MainForm()
    {
        SuspendLayout();

        Text = $"{AppInfo.name} - v{AppInfo.version}";
        Icon = Properties.Resources.Icon;
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.Manual;
        Margin = Padding.Empty;

        SetupBlazorView();

        Opacity = 0;
        ShowInTaskbar = false;

        ResumeLayout(true);
    }

    void SetupBlazorView()
    {
        blazorWebView = new BlazorWebView
        {
            HostPage = AppInfo.hostPage,
            Location = Point.Empty,
            Margin = Padding.Empty,
            Dock = DockStyle.Fill
        };

        blazorWebView.WebView.DefaultBackgroundColor = Color.Transparent;

        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif

        blazorWebView.Services = services.BuildServiceProvider();

        blazorWebView.RootComponents.Add<Web.Root>("#root");

        Controls.Add(blazorWebView);
    }

    Task focusTask;

    public void TryShowDialog()
    {
        if (TryFocus() && !Visible)
        {
            ShowDialog();
        }
    }

    bool TryFocus()
    {
        if (!Ready)
            return false;

        if (focusTask is not null)
            return false;

        focusTask = Task.Run(async () =>
        {
            while (!Visible)
                await Task.Delay(1);

            Invoke(() =>
            {
                Activate();
                focusTask = null;
            });
        });

        return true;
    }

    public void OnAfterFirstRender(Rectangle? bounds, bool maximized)
    {
        RefreshWindow(zoomFactor: 1, bounds, maximized);

        Close();

        Opacity = 1;
        ShowInTaskbar = true;

        Ready = true;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing)
            return;

        e.Cancel = true;

        var config = Config.Instance;

        if (WindowState == FormWindowState.Maximized)
        {
            config.maximized = true;
        }
        else
        {
            config.bounds = Bounds;
            config.maximized = false;
        }

        Hide();
    }

    public void RefreshWindow(float zoomFactor, Rectangle? bounds, bool maximized)
    {
        WindowState = maximized ? FormWindowState.Maximized : FormWindowState.Normal;

        blazorWebView.WebView.ZoomFactor = zoomFactor;

        Point location;
        Size size;

        if (bounds.HasValue)
        {
            location = bounds.Value.Location;
            size = bounds.Value.Size;
        }
        else
        {
            var dpiFactor = blazorWebView.WebView.DeviceDpi / 96f;
            size = (new Size(480, 640) * zoomFactor * dpiFactor).ToSize();

            var screenSize = Screen.PrimaryScreen.Bounds.Size;
            location = (Point) ((screenSize - size) / 2);
        }

        if (Size != size || Location != location)
        {
            Size = size;
            Location = location;
        }
    }
}
