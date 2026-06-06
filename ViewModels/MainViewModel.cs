using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using DeepSeekTokenMonitor.Models;
using DeepSeekTokenMonitor.Services;

namespace DeepSeekTokenMonitor.ViewModels;

/// <summary>
/// 主视图模型，驱动侧边栏 UI 数据绑定
/// </summary>
public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly DeepSeekApiService _apiService;
    private readonly UsageTracker _usageTracker;
    private readonly SettingsService _settingsService;
    private readonly DispatcherTimer _pollTimer;

    // ── 可绑定属性 ──────────────────────────────
    private decimal _currentBalance;
    public decimal CurrentBalance
    {
        get => _currentBalance;
        set { _currentBalance = value; OnPropertyChanged(); OnPropertyChanged(nameof(BalanceText)); }
    }
    public string BalanceText => $"¥{_currentBalance:F2}";

    /// <summary>累计消耗 = 已赠送 + 已充值 - 当前余额（首次即有数据）</summary>
    private decimal _totalSpent;
    public decimal TotalSpent
    {
        get => _totalSpent;
        set { _totalSpent = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalSpentText)); }
    }
    public string TotalSpentText => $"¥{_totalSpent:F2}";

    private bool _isAvailable;
    public bool IsAvailable
    {
        get => _isAvailable;
        set { _isAvailable = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusColor)); }
    }
    public string StatusColor => _isAvailable
        ? Services.ThemeManager.GetColor("StatusOk")
        : Services.ThemeManager.GetColor("StatusError");

    private decimal _todaySpent;
    public decimal TodaySpent
    {
        get => _todaySpent;
        set { _todaySpent = value; OnPropertyChanged(); OnPropertyChanged(nameof(TodaySpentText)); }
    }
    public string TodaySpentText => $"¥{_todaySpent:F2}";

    private decimal _weekSpent;
    public decimal WeekSpent
    {
        get => _weekSpent;
        set { _weekSpent = value; OnPropertyChanged(); OnPropertyChanged(nameof(WeekSpentText)); }
    }
    public string WeekSpentText => $"¥{_weekSpent:F2}";

    private decimal _monthSpent;
    public decimal MonthSpent
    {
        get => _monthSpent;
        set { _monthSpent = value; OnPropertyChanged(); OnPropertyChanged(nameof(MonthSpentText)); }
    }
    public string MonthSpentText => $"¥{_monthSpent:F2}";

    private string _lastUpdated = "--:--";
    public string LastUpdated
    {
        get => _lastUpdated;
        set { _lastUpdated = value; OnPropertyChanged(); }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    private string _errorMessage = "";
    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
    }
    public bool HasError => !string.IsNullOrEmpty(_errorMessage);

    private bool _isLowBalance;
    public bool IsLowBalance
    {
        get => _isLowBalance;
        set { _isLowBalance = value; OnPropertyChanged(); }
    }

    private decimal _dailyAvgSpent;
    public decimal DailyAvgSpent
    {
        get => _dailyAvgSpent;
        set { _dailyAvgSpent = value; OnPropertyChanged(); OnPropertyChanged(nameof(EstDaysLeft)); OnPropertyChanged(nameof(EstDaysLeftText)); }
    }

    public int EstDaysLeft
    {
        get
        {
            if (_dailyAvgSpent <= 0) return -1; // -1 表示"无法估算"
            return (int)(_currentBalance / _dailyAvgSpent);
        }
    }
    public string EstDaysLeftText =>
        EstDaysLeft < 0 ? "∞" : $"{EstDaysLeft} 天";

    /// <summary>7 天日消耗数据（供图表使用）</summary>
    public List<DailyUsage> DailyUsageData { get; private set; } = new();

    // ── 事件 ──────────────────────────────────
    public event Action? DataRefreshed;
    public event Action? SettingsRequested;

    // ── 构造函数 ────────────────────────────────
    public MainViewModel(DeepSeekApiService apiService, UsageTracker usageTracker, SettingsService settingsService)
    {
        _apiService = apiService;
        _usageTracker = usageTracker;
        _settingsService = settingsService;

        _pollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(settingsService.Settings.PollIntervalMinutes)
        };
        _pollTimer.Tick += async (_, _) => await RefreshAsync();
    }

    public void Start()
    {
        _pollTimer.Start();
        // 启动时立即刷新一次
        _ = RefreshAsync();
    }

    public void Stop()
    {
        _pollTimer.Stop();
    }

    public void UpdatePollInterval(int minutes)
    {
        _pollTimer.Interval = TimeSpan.FromMinutes(minutes);
    }

    /// <summary>
    /// 从 DeepSeek API 拉取最新余额
    /// </summary>
    public async Task RefreshAsync()
    {
        IsLoading = true;
        ErrorMessage = "";

        try
        {
            var (balance, error) = await _apiService.GetBalanceAsync();
            if (balance == null)
            {
                ErrorMessage = $"获取失败: {error ?? "未知错误"}";
                IsAvailable = false;
            }
            else
            {
                CurrentBalance = balance.TotalBalance;
                IsAvailable = balance.IsAvailable;
                IsLowBalance = CurrentBalance < _settingsService.Settings.LowBalanceThreshold;

                // 累计消耗 = 总获得额度 - 当前余额（首次即有数据）
                TotalSpent = Math.Max(0, balance.TotalGranted + balance.TotalToppedUp - CurrentBalance);

                // 记录快照
                _usageTracker.RecordSnapshot(CurrentBalance);

                // 更新时段消耗统计（需要多次快照后才有数据）
                TodaySpent = _usageTracker.TodaySpent;
                WeekSpent = _usageTracker.WeekSpent;
                MonthSpent = _usageTracker.MonthSpent;

                // 计算日均消耗：优先用快照数据，不够则用累计消耗粗略估算
                var dailyData = _usageTracker.GetDailyUsage(7);
                DailyUsageData = dailyData;
                if (dailyData.Count > 0)
                {
                    DailyAvgSpent = dailyData.Average(d => d.Spent);
                }
                else if (_usageTracker.Snapshots.Count >= 2)
                {
                    // 用快照时间跨度估算日均
                    var snaps = _usageTracker.Snapshots;
                    var daysSpan = Math.Max(1, (snaps.Last().Timestamp - snaps.First().Timestamp).TotalDays);
                    var totalDiff = Math.Max(0, snaps.First().Balance - snaps.Last().Balance);
                    DailyAvgSpent = (decimal)((double)totalDiff / daysSpan);
                }
                else if (TotalSpent > 0 && _usageTracker.Snapshots.Count >= 1)
                {
                    // 首次运行：假设账户已使用至少 1 天，粗略估算
                    DailyAvgSpent = TotalSpent / (decimal)Math.Max(1, _usageTracker.Snapshots.Count);
                }

                LastUpdated = DateTime.Now.ToString("HH:mm");

                DataRefreshed?.Invoke();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"网络错误: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void RequestSettings()
    {
        SettingsRequested?.Invoke();
    }

    // ── INotifyPropertyChanged ───────────────────
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public void Dispose()
    {
        Stop();
        _apiService.Dispose();
    }
}
