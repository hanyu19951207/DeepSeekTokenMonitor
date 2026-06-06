using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DeepSeekTokenMonitor.Services;
using Microsoft.Win32;

// 解决 WinForms/WPF MessageBox 歧义
using MessageBox = System.Windows.MessageBox;

namespace DeepSeekTokenMonitor.Windows;

public partial class SettingsWindow : Window
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "DeepSeekTokenMonitor";

    private readonly SettingsService _settingsService;
    private readonly DeepSeekApiService _apiService;
    private readonly Action _onSettingsSaved;

    public SettingsWindow(SettingsService settingsService, DeepSeekApiService apiService, Action onSettingsSaved)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _apiService = apiService;
        _onSettingsSaved = onSettingsSaved;
        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        var s = _settingsService.Settings;
        ApiKeyBox.Text = s.ApiKey;
        BaseUrlBox.Text = s.ApiBaseUrl;
        IntervalSlider.Value = s.PollIntervalMinutes;
        IntervalLabel.Text = s.PollIntervalMinutes.ToString();
        RightRadio.IsChecked = s.SidebarPosition == "Right";
        LeftRadio.IsChecked = s.SidebarPosition == "Left";
        AutoStartCheck.IsChecked = IsAutoStartEnabled();

        // 主题选择
        ThemeCombo.SelectedIndex = s.Theme == "Dark" ? 1 : 0;
        ThemeCombo.SelectionChanged += ThemeCombo_SelectionChanged;
    }

    private void IntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IntervalLabel != null)
            IntervalLabel.Text = ((int)e.NewValue).ToString();
    }

    /// <summary>主题下拉切换时实时预览</summary>
    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeCombo == null) return;
        var theme = ThemeCombo.SelectedIndex == 1 ? "Dark" : "Light";
        ThemeManager.SetTheme(theme);
    }

    private async void TestButton_Click(object sender, RoutedEventArgs e)
    {
        var key = ApiKeyBox.Text.Trim();
        var baseUrl = BaseUrlBox.Text.Trim();
        if (string.IsNullOrEmpty(key))
        {
            ShowMessage("请输入 API Key", "提示");
            return;
        }

        TestButton.Content = "测试中...";
        TestButton.IsEnabled = false;

        _apiService.UpdateApiKey(key, baseUrl);
        var (ok, error) = await _apiService.TestConnectionAsync();

        TestButton.IsEnabled = true;
        TestButton.Content = "测试连接";

        if (ok)
            ShowMessage("✓ 连接成功！API Key 有效。", "测试");
        else
            ShowMessage($"✕ 连接失败:\n{error}", "测试");
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var s = _settingsService.Settings;
        s.ApiKey = ApiKeyBox.Text.Trim();
        s.ApiBaseUrl = string.IsNullOrWhiteSpace(BaseUrlBox.Text)
            ? "https://api.deepseek.com"
            : BaseUrlBox.Text.Trim();
        s.PollIntervalMinutes = (int)IntervalSlider.Value;
        s.SidebarPosition = RightRadio.IsChecked == true ? "Right" : "Left";
        s.AutoStart = AutoStartCheck.IsChecked == true;
        s.Theme = ThemeCombo.SelectedIndex == 1 ? "Dark" : "Light";

        // 写入/移除注册表
        SetAutoStart(s.AutoStart);

        // 应用主题
        ThemeManager.SetTheme(s.Theme);

        _settingsService.Save();
        _apiService.UpdateApiKey(s.ApiKey, s.ApiBaseUrl);
        _onSettingsSaved();

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowMessage(string text, string title)
    {
        MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ── 注册表开机自启 ──────────────────────────────

    /// <summary>
    /// 检查注册表中是否已设置开机自启
    /// </summary>
    private static bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 设置/取消注册表开机自启（HKCU，无需管理员权限）
    /// </summary>
    private static void SetAutoStart(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null) return;

            if (enabled)
            {
                var exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                    key.SetValue(AppName, $"\"{exePath}\"", RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch
        {
            // 静默处理
        }
    }
}
