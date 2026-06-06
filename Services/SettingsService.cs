using System.IO;
using System.Text.Json;

namespace DeepSeekTokenMonitor.Services;

/// <summary>
/// 应用设置管理（API Key、轮询间隔、侧边栏位置等）
/// 配置文件保存在 %AppData%/DeepSeekTokenMonitor/settings.json
/// </summary>
public class SettingsService
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "DeepSeekTokenMonitor");

    private static readonly string SettingsPath =
        Path.Combine(SettingsDir, "settings.json");

    private AppSettings _settings = new();

    public AppSettings Settings => _settings;

    public SettingsService()
    {
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            _settings = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // 静默处理写入错误
        }
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(_settings.ApiKey);
}

public class AppSettings
{
    /// <summary>DeepSeek API Key（sk-xxx）</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>API 基地址，默认官方地址</summary>
    public string ApiBaseUrl { get; set; } = "https://api.deepseek.com";

    /// <summary>轮询间隔（分钟）</summary>
    public int PollIntervalMinutes { get; set; } = 5;

    /// <summary>侧边栏位置：Right / Left</summary>
    public string SidebarPosition { get; set; } = "Right";

    /// <summary>余额低于此值时显示警告</summary>
    public decimal LowBalanceThreshold { get; set; } = 10.0m;

    /// <summary>窗口展开宽度</summary>
    public double ExpandedWidth { get; set; } = 220;

    /// <summary>窗口收起宽度</summary>
    public double CollapsedWidth { get; set; } = 40;

    /// <summary>是否开机自启动</summary>
    public bool AutoStart { get; set; } = false;

    /// <summary>主题：Light / Dark</summary>
    public string Theme { get; set; } = "Light";
}
