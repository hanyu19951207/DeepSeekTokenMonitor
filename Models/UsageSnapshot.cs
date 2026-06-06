namespace DeepSeekTokenMonitor.Models;

/// <summary>
/// 一次余额快照，用于追踪消耗趋势
/// </summary>
public class UsageSnapshot
{
    public DateTime Timestamp { get; set; }
    public decimal Balance { get; set; }
}

/// <summary>
/// 聚合后的日消耗统计
/// </summary>
public class DailyUsage
{
    public DateTime Date { get; set; }
    public decimal Spent { get; set; }
}
