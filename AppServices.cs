using DeepSeekTokenMonitor.Services;

namespace DeepSeekTokenMonitor;

/// <summary>
/// 全局服务单例，方便各处访问
/// </summary>
public static class AppServices
{
    public static SettingsService Settings { get; private set; } = null!;
    public static DeepSeekApiService ApiService { get; private set; } = null!;
    public static UsageTracker Tracker { get; private set; } = null!;

    public static void Initialize()
    {
        Settings = new SettingsService();
        var s = Settings.Settings;

        ApiService = new DeepSeekApiService(
            s.ApiKey,
            string.IsNullOrWhiteSpace(s.ApiBaseUrl) ? "https://api.deepseek.com" : s.ApiBaseUrl
        );
        Tracker = new UsageTracker();
    }
}
