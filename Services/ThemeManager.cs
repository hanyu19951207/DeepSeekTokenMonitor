using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace DeepSeekTokenMonitor.Services;

/// <summary>
/// 主题管理器：定义深/浅两套色板，运行时切换
/// </summary>
public static class ThemeManager
{
    public static string Current { get; private set; } = "Light";

    // ── 深色色板 ──────────────────────────────
    private static readonly Dictionary<string, string> DarkPalette = new()
    {
        ["PanelBg"]          = "#EE1A1A2E",
        ["PanelBgEnd"]       = "#EE16213E",
        ["PanelBorder"]      = "#00000000",
        ["StripBg"]          = "#DD1A1A2E",
        ["PrimaryText"]      = "#E0E0E0",
        ["SecondaryText"]    = "#78909C",
        ["AccentCyan"]       = "#00E5FF",
        ["AccentOrange"]     = "#FFAB40",
        ["DangerRed"]        = "#FF5252",
        ["SuccessGreen"]     = "#00E676",
        ["Separator"]        = "#22FFFFFF",
        ["ChartBg"]          = "#11FFFFFF",
        ["ButtonHover"]      = "#22FFFFFF",
        ["ButtonDefault"]    = "#78909C",
        ["Shadow"]           = "#000000",
        ["ShadowOpacity"]    = "0.6",
        // 图表
        ["ChartLine"]        = "#00E5FF",
        ["ChartFillStart"]   = "#3C00E5FF",
        ["ChartFillEnd"]     = "#0500E5FF",
        ["ChartDotStroke"]   = "#1A1A2E",
        // 设置窗口
        ["SettingsBg"]       = "#1A1A2E",
        ["InputBg"]          = "#16213E",
        ["InputBorder"]      = "#333366",
        ["BtnSecondaryBg"]   = "#16213E",
        ["BtnPrimaryBg"]     = "#00E5FF",
        ["BtnPrimaryFg"]     = "#1A1A2E",
        ["TrayBg"]           = "#1A1A2E",
        ["TrayFg"]           = "#00E5FF",
        ["TrayCenter"]       = "#1A1A2E",
        // ViewModel 状态色
        ["StatusOk"]         = "#00E676",
        ["StatusError"]      = "#FF5252",
    };

    // ── 浅色色板（极光白）─────────────────────
    private static readonly Dictionary<string, string> LightPalette = new()
    {
        ["PanelBg"]          = "#F8FAFC",
        ["PanelBgEnd"]       = "#F8FAFC",
        ["PanelBorder"]      = "#E2E8F0",
        ["StripBg"]          = "#EEF2F7",
        ["PrimaryText"]      = "#1E293B",
        ["SecondaryText"]    = "#64748B",
        ["AccentCyan"]       = "#0891B2",
        ["AccentOrange"]     = "#D97706",
        ["DangerRed"]        = "#DC2626",
        ["SuccessGreen"]     = "#16A34A",
        ["Separator"]        = "#E2E8F0",
        ["ChartBg"]          = "#F1F5F9",
        ["ButtonHover"]      = "#E2E8F0",
        ["ButtonDefault"]    = "#94A3B8",
        ["Shadow"]           = "#94A3B8",
        ["ShadowOpacity"]    = "0.25",
        // 图表
        ["ChartLine"]        = "#0891B2",
        ["ChartFillStart"]   = "#320891B2",
        ["ChartFillEnd"]     = "#050891B2",
        ["ChartDotStroke"]   = "#F8FAFC",
        // 设置窗口
        ["SettingsBg"]       = "#F8FAFC",
        ["InputBg"]          = "#FFFFFF",
        ["InputBorder"]      = "#CBD5E1",
        ["BtnSecondaryBg"]   = "#FFFFFF",
        ["BtnPrimaryBg"]     = "#0891B2",
        ["BtnPrimaryFg"]     = "#FFFFFF",
        ["TrayBg"]           = "#F8FAFC",
        ["TrayFg"]           = "#0891B2",
        ["TrayCenter"]       = "#F8FAFC",
        // ViewModel 状态色
        ["StatusOk"]         = "#16A34A",
        ["StatusError"]      = "#DC2626",
    };

    /// <summary>获取当前色板</summary>
    public static Dictionary<string, string> Palette =>
        Current == "Dark" ? DarkPalette : LightPalette;

    /// <summary>获取颜色值（hex string）</summary>
    public static string GetColor(string key) => Palette[key];

    /// <summary>获取 WPF Color</summary>
    public static Color GetWpfColor(string key) =>
        (Color)ColorConverter.ConvertFromString(Palette[key]);

    /// <summary>获取 WPF SolidColorBrush</summary>
    public static SolidColorBrush GetBrush(string key) =>
        new(GetWpfColor(key));

    /// <summary>切换主题，返回是否真的变了</summary>
    public static bool SetTheme(string theme)
    {
        if (theme == Current) return false;
        Current = theme;
        ThemeChanged?.Invoke();
        return true;
    }

    public static event Action? ThemeChanged;
}
