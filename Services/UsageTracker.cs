using System.IO;
using System.Text.Json;
using DeepSeekTokenMonitor.Models;

namespace DeepSeekTokenMonitor.Services;

/// <summary>
/// 用量追踪器：记录余额快照，计算消耗量
/// 数据持久化到 %AppData%/DeepSeekTokenMonitor/snapshots.json
/// </summary>
public class UsageTracker
{
    private static readonly string DataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "DeepSeekTokenMonitor");

    private static readonly string SnapshotsPath =
        Path.Combine(DataDir, "snapshots.json");

    private readonly List<UsageSnapshot> _snapshots = new();
    private readonly object _lock = new();

    public IReadOnlyList<UsageSnapshot> Snapshots
    {
        get { lock (_lock) return _snapshots.ToList(); }
    }

    public UsageTracker()
    {
        Load();
        PurgeInvalidSnapshots();
    }

    /// <summary>
    /// 清理 Balance=0 的无效快照（来自 API 字段修复前的历史脏数据）
    /// </summary>
    private void PurgeInvalidSnapshots()
    {
        lock (_lock)
        {
            var before = _snapshots.Count;
            _snapshots.RemoveAll(s => s.Balance == 0);
            if (_snapshots.Count < before)
            {
                Save();
            }
        }
    }

    /// <summary>
    /// 记录一次余额快照
    /// </summary>
    public void RecordSnapshot(decimal balance)
    {
        lock (_lock)
        {
            _snapshots.Add(new UsageSnapshot
            {
                Timestamp = DateTime.Now,
                Balance = balance
            });
            PruneOldSnapshots();
            Save();
        }
    }

    /// <summary>
    /// 获取最近 N 天的日消耗量
    /// </summary>
    public List<DailyUsage> GetDailyUsage(int days = 7)
    {
        lock (_lock)
        {
            if (_snapshots.Count < 2)
                return new List<DailyUsage>();

            var cutoff = DateTime.Now.AddDays(-days);
            var relevant = _snapshots
                .Where(s => s.Timestamp >= cutoff && s.Balance > 0)
                .OrderBy(s => s.Timestamp)
                .ToList();

            if (relevant.Count < 2)
                return new List<DailyUsage>();

            // 按日期分组，取每组首尾快照计算消耗
            var result = new List<DailyUsage>();
            var groups = relevant.GroupBy(s => s.Timestamp.Date).ToList();

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                var dayStart = group.First().Balance;
                var dayEnd = group.Last().Balance;
                var spent = Math.Max(0, dayStart - dayEnd);

                // 如果是第一天且有前一天数据，用前一天最后余额计算
                if (i == 0 && groups.Count > 1)
                {
                    // 使用组内差值即可
                }

                result.Add(new DailyUsage
                {
                    Date = group.Key,
                    Spent = spent
                });
            }

            return result;
        }
    }

    /// <summary>
    /// 获取指定时间范围内的总消耗量
    /// </summary>
    public decimal GetTotalSpent(TimeSpan period)
    {
        lock (_lock)
        {
            var cutoff = DateTime.Now - period;
            var relevant = _snapshots
                .Where(s => s.Timestamp >= cutoff && s.Balance > 0)
                .OrderBy(s => s.Timestamp)
                .ToList();

            if (relevant.Count < 2)
                return 0;

            var first = relevant.First().Balance;
            var last = relevant.Last().Balance;
            return Math.Max(0, first - last);
        }
    }

    public decimal TodaySpent => GetTotalSpent(TimeSpan.FromDays(1));
    public decimal WeekSpent => GetTotalSpent(TimeSpan.FromDays(7));
    public decimal MonthSpent => GetTotalSpent(TimeSpan.FromDays(30));

    /// <summary>
    /// 清理 30 天前的旧快照（每小时保留一条）
    /// </summary>
    private void PruneOldSnapshots()
    {
        var cutoff = DateTime.Now.AddDays(-30);
        var old = _snapshots.Where(s => s.Timestamp < cutoff).ToList();

        // 保留每小时最后一条
        var toRemove = old
            .GroupBy(s => new { s.Timestamp.Date, s.Timestamp.Hour })
            .SelectMany(g => g.Take(g.Count() - 1))
            .ToList();

        foreach (var s in toRemove)
            _snapshots.Remove(s);
    }

    private void Load()
    {
        try
        {
            if (File.Exists(SnapshotsPath))
            {
                var json = File.ReadAllText(SnapshotsPath);
                var data = JsonSerializer.Deserialize<List<UsageSnapshot>>(json);
                if (data != null)
                    _snapshots.AddRange(data);
            }
        }
        catch
        {
            // 数据损坏时重新开始
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            var json = JsonSerializer.Serialize(_snapshots, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            File.WriteAllText(SnapshotsPath, json);
        }
        catch
        {
            // 静默处理
        }
    }
}
