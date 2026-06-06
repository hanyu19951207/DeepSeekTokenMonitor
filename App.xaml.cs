using System.Drawing;
using System.Windows;
using DeepSeekTokenMonitor.Services;
using DeepSeekTokenMonitor.ViewModels;
using DeepSeekTokenMonitor.Windows;
using Forms = System.Windows.Forms;

namespace DeepSeekTokenMonitor;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private MainViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // 初始化全局服务
        AppServices.Initialize();

        // 应用保存的主题
        ThemeManager.SetTheme(AppServices.Settings.Settings.Theme);

        // 首次运行检查 API Key
        if (!AppServices.Settings.HasApiKey)
        {
            ShowSettingsDialog(firstRun: true);
            if (!AppServices.Settings.HasApiKey)
            {
                // 用户关闭了设置窗口且没填 Key，退出
                Shutdown();
                return;
            }
        }

        CreateViewModel();
        CreateMainWindow();
        CreateTrayIcon();

        _viewModel!.Start();
    }

    private void CreateViewModel()
    {
        _viewModel = new MainViewModel(
            AppServices.ApiService,
            AppServices.Tracker,
            AppServices.Settings
        );
        _viewModel.SettingsRequested += OnSettingsRequested;
    }

    private void CreateMainWindow()
    {
        _mainWindow = new MainWindow(_viewModel!);
        _mainWindow.Show();
    }

    private void CreateTrayIcon()
    {
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = GenerateTrayIcon(),
            Text = "DeepSeek Token Monitor",
            Visible = true
        };

        // 双击托盘图标 → 显示/隐藏
        _trayIcon.DoubleClick += (_, _) => ToggleMainWindow();

        // 右键菜单
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("显示/隐藏", null, (_, _) => ToggleMainWindow());
        menu.Items.Add("立即刷新", null, async (_, _) => await _viewModel!.RefreshAsync());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("设置...", null, (_, _) => ShowSettingsDialog());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => Shutdown());
        _trayIcon.ContextMenuStrip = menu;
    }

    private void ToggleMainWindow()
    {
        if (_mainWindow == null) return;

        if (_mainWindow.IsVisible)
            _mainWindow.Hide();
        else
            _mainWindow.ShowAndExpand();
    }

    private void OnSettingsRequested()
    {
        ShowSettingsDialog();
    }

    private void ShowSettingsDialog(bool firstRun = false)
    {
        var window = new SettingsWindow(
            AppServices.Settings,
            AppServices.ApiService,
            onSettingsSaved: () =>
            {
                // 设置保存后更新轮询间隔
                _viewModel?.UpdatePollInterval(AppServices.Settings.Settings.PollIntervalMinutes);
                if (!firstRun)
                    _ = _viewModel?.RefreshAsync();
            }
        );

        if (firstRun)
        {
            window.Title = "欢迎 - 请配置 API Key";
        }

        window.ShowDialog();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _viewModel?.Dispose();
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        base.OnExit(e);
    }

    /// <summary>
    /// 程序化生成托盘图标（白底 + 青色圆点）
    /// 无需外部 .ico 文件
    /// </summary>
    private static Icon GenerateTrayIcon()
    {
        var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // 背景圆（白底）
            using var bgBrush = new SolidBrush(Color.FromArgb(248, 250, 252));
            g.FillEllipse(bgBrush, 1, 1, 30, 30);

            // 外圈描边
            using var borderPen = new Pen(Color.FromArgb(203, 213, 225), 1);
            g.DrawEllipse(borderPen, 1, 1, 30, 30);

            // 内圆（青色）
            using var fgBrush = new SolidBrush(Color.FromArgb(8, 145, 178));
            g.FillEllipse(fgBrush, 8, 8, 16, 16);

            // 中心点
            using var centerBrush = new SolidBrush(Color.FromArgb(248, 250, 252));
            g.FillEllipse(centerBrush, 13, 13, 6, 6);
        }

        IntPtr hIcon = bmp.GetHicon();
        var icon = Icon.FromHandle(hIcon);
        // 返回克隆以确保图标在 bitmap 释放后仍可用
        var result = (Icon)icon.Clone();
        DestroyIcon(hIcon);
        bmp.Dispose();
        return result;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
