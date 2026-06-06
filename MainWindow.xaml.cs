using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using DeepSeekTokenMonitor.Services;
using DeepSeekTokenMonitor.ViewModels;

// 解决 System.Drawing (WinForms) 与 System.Windows.Media (WPF) 的命名冲突
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Brushes = System.Windows.Media.Brushes;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace DeepSeekTokenMonitor;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly double _expandedWidth;
    private readonly double _collapsedWidth;
    private bool _isExpanded;
    private bool _isOnRight = true;

    // 防抖计时器：鼠标离开后延迟收起
    private readonly DispatcherTimer _collapseTimer;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;

        var settings = AppServices.Settings;
        _expandedWidth = settings.Settings.ExpandedWidth;
        _collapsedWidth = settings.Settings.CollapsedWidth;
        _isOnRight = settings.Settings.SidebarPosition == "Right";

        // 绑定 ViewModel 事件
        _vm.PropertyChanged += (_, e) =>
        {
            Dispatcher.Invoke(() => UpdateUI(e.PropertyName));
        };
        _vm.DataRefreshed += () => Dispatcher.Invoke(UpdateChart);

        // 设置收起防抖
        _collapseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _collapseTimer.Tick += (_, _) =>
        {
            _collapseTimer.Stop();
            CollapsePanel();
        };

        // 监听主题切换
        ThemeManager.ThemeChanged += () => Dispatcher.Invoke(ApplyTheme);
    }

    // ─────────────────────────────────────────
    // 主题应用
    // ─────────────────────────────────────────
    public void ApplyTheme()
    {
        var tm = ThemeManager.Palette;
        var gc = ThemeManager.GetColor;
        var gw = ThemeManager.GetWpfColor;

        // ── 资源画刷 ──
        Resources["PrimaryText"]   = ThemeManager.GetBrush("PrimaryText");
        Resources["SecondaryText"] = ThemeManager.GetBrush("SecondaryText");
        Resources["AccentCyan"]    = ThemeManager.GetBrush("AccentCyan");
        Resources["AccentOrange"]  = ThemeManager.GetBrush("AccentOrange");
        Resources["DangerRed"]     = ThemeManager.GetBrush("DangerRed");
        Resources["SuccessGreen"]  = ThemeManager.GetBrush("SuccessGreen");

        // ── 收起条 ──
        CollapsedStrip.Background = new SolidColorBrush(gw("StripBg"));
        CollapsedBalance.Foreground = new SolidColorBrush(gw("AccentCyan"));

        // 收起条阴影
        CollapsedStrip.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = gw("Shadow"), BlurRadius = 8, ShadowDepth = 1,
            Opacity = double.Parse(gc("ShadowOpacity"))
        };

        // ── 展开面板 ──
        ExpandedPanel.Background = new SolidColorBrush(gw("PanelBg"));
        ExpandedPanel.BorderBrush = new SolidColorBrush(gw("PanelBorder"));
        ExpandedPanel.BorderThickness = new Thickness(
            ThemeManager.Current == "Dark" ? 0 : 1,
            ThemeManager.Current == "Dark" ? 0 : 1,
            0,
            ThemeManager.Current == "Dark" ? 0 : 1);
        ExpandedPanel.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = gw("Shadow"), BlurRadius = 20, ShadowDepth = 4,
            Opacity = double.Parse(gc("ShadowOpacity"))
        };

        // ── 分隔线 ──
        // (Grid.Row="3" 的 Border，通过 FindName 或直接查找)

        // ── 图表背景 ──
        if (TrendChart.Parent is Border chartBorder)
            chartBorder.Background = new SolidColorBrush(gw("ChartBg"));

        // ── 状态点 ──
        var statusColor = _vm.IsAvailable ? gw("StatusOk") : gw("StatusError");
        StatusDot.Fill = new SolidColorBrush(statusColor);
        CollapsedStatusDot.Fill = new SolidColorBrush(statusColor);

        // ── 重绘图表 ──
        UpdateChart();
    }

    // ─────────────────────────────────────────
    // 窗口加载：定位到屏幕边缘
    // ─────────────────────────────────────────
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyTheme();
        PositionAtScreenEdge();
        BindData();
        CollapsePanel();

        // 鼠标事件
        CollapsedStrip.MouseEnter += (_, _) => { _collapseTimer.Stop(); ExpandPanel(); };
        ExpandedPanel.MouseEnter += (_, _) => { _collapseTimer.Stop(); };
        ExpandedPanel.MouseLeave += (_, _) => { _collapseTimer.Stop(); _collapseTimer.Start(); };
        CollapsedStrip.MouseLeftButtonDown += (_, _) =>
        {
            if (!_isExpanded) ExpandPanel();
        };
    }

    private void PositionAtScreenEdge()
    {
        var screen = SystemParameters.WorkArea;
        Height = Math.Min(480, screen.Height - 40);

        if (_isOnRight)
        {
            Left = screen.Right - _collapsedWidth;
            Top = screen.Top + (screen.Height - Height) / 2;
        }
        else
        {
            Left = screen.Left;
            Top = screen.Top + (screen.Height - Height) / 2;
        }
    }

    // ─────────────────────────────────────────
    // 展开 / 收起 动画
    // ─────────────────────────────────────────
    private void ExpandPanel()
    {
        if (_isExpanded) return;
        _isExpanded = true;

        CollapsedStrip.Visibility = Visibility.Collapsed;
        ExpandedPanel.Visibility = Visibility.Visible;

        var screen = SystemParameters.WorkArea;
        double targetLeft = _isOnRight
            ? screen.Right - _expandedWidth
            : screen.Left;

        AnimateDouble(this, LeftProperty, Left, targetLeft, 250);
        AnimateDouble(this, WidthProperty, _collapsedWidth, _expandedWidth, 250);
        AnimateDouble(ExpandedPanel, OpacityProperty, 0, 1, 200);
    }

    private void CollapsePanel()
    {
        if (!_isExpanded) return;
        _isExpanded = false;

        var screen = SystemParameters.WorkArea;
        double targetLeft = _isOnRight
            ? screen.Right - _collapsedWidth
            : screen.Left;

        AnimateDouble(this, LeftProperty, Left, targetLeft, 200);
        AnimateDouble(this, WidthProperty, Width, _collapsedWidth, 200);
        AnimateDouble(ExpandedPanel, OpacityProperty, 1, 0, 150,
            completed: () =>
            {
                ExpandedPanel.Visibility = Visibility.Collapsed;
                CollapsedStrip.Visibility = Visibility.Visible;
            });
    }

    private static void AnimateDouble(UIElement target, DependencyProperty prop,
        double from, double to, int durationMs, Action? completed = null)
    {
        var anim = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        if (completed != null)
            anim.Completed += (_, _) => completed();
        target.BeginAnimation(prop, anim);
    }

    // ─────────────────────────────────────────
    // 数据绑定 & UI 刷新
    // ─────────────────────────────────────────
    private void BindData()
    {
        BalanceValue.Text = _vm.BalanceText;
        TotalSpentValue.Text = _vm.TotalSpentText;
        TodayValue.Text = _vm.TodaySpentText;
        WeekValue.Text = _vm.WeekSpentText;
        MonthValue.Text = _vm.MonthSpentText;
        UpdateTimeText.Text = _vm.LastUpdated;
        EstDaysValue.Text = _vm.EstDaysLeftText;
        CollapsedBalance.Text = $"¥{(int)_vm.CurrentBalance}";
    }

    private void UpdateUI(string? propertyName)
    {
        switch (propertyName)
        {
            case nameof(_vm.BalanceText):
                BalanceValue.Text = _vm.BalanceText;
                CollapsedBalance.Text = $"¥{(int)_vm.CurrentBalance}";
                break;
            case nameof(_vm.TotalSpentText):
                TotalSpentValue.Text = _vm.TotalSpentText;
                break;
            case nameof(_vm.StatusColor):
                var statusColor = _vm.IsAvailable
                    ? ThemeManager.GetWpfColor("StatusOk")
                    : ThemeManager.GetWpfColor("StatusError");
                var brush = new SolidColorBrush(statusColor);
                StatusDot.Fill = brush;
                CollapsedStatusDot.Fill = brush;
                break;
            case nameof(_vm.TodaySpentText):
                TodayValue.Text = _vm.TodaySpentText;
                break;
            case nameof(_vm.WeekSpentText):
                WeekValue.Text = _vm.WeekSpentText;
                break;
            case nameof(_vm.MonthSpentText):
                MonthValue.Text = _vm.MonthSpentText;
                break;
            case nameof(_vm.LastUpdated):
                UpdateTimeText.Text = _vm.LastUpdated;
                break;
            case nameof(_vm.EstDaysLeftText):
                EstDaysValue.Text = _vm.EstDaysLeftText;
                break;
            case nameof(_vm.IsLowBalance):
                LowBalanceWarning.Visibility = _vm.IsLowBalance ? Visibility.Visible : Visibility.Collapsed;
                break;
            case nameof(_vm.IsLoading):
                LoadingIndicator.Visibility = _vm.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                RefreshButton.IsEnabled = !_vm.IsLoading;
                break;
            case nameof(_vm.HasError):
                if (_vm.HasError)
                {
                    ErrorText.Text = _vm.ErrorMessage;
                    ErrorText.Visibility = Visibility.Visible;
                }
                else
                {
                    ErrorText.Visibility = Visibility.Collapsed;
                }
                break;
        }
    }

    // ─────────────────────────────────────────
    // 迷你折线图绘制（颜色跟随主题）
    // ─────────────────────────────────────────
    private void UpdateChart()
    {
        TrendChart.Children.Clear();

        var data = _vm.DailyUsageData;
        if (data.Count < 2) return;

        double chartW = TrendChart.ActualWidth > 0 ? TrendChart.ActualWidth - 8 : 160;
        double chartH = TrendChart.ActualHeight > 0 ? TrendChart.ActualHeight - 8 : 50;
        if (chartW <= 0 || chartH <= 0) return;

        var maxSpent = data.Max(d => d.Spent);
        if (maxSpent <= 0) maxSpent = 1;

        var points = new PointCollection();
        for (int i = 0; i < data.Count; i++)
        {
            double x = (double)i / (data.Count - 1) * chartW;
            double y = chartH - (double)data[i].Spent / (double)maxSpent * chartH;
            points.Add(new Point(x + 4, y + 4));
        }

        var chartLine = ThemeManager.GetWpfColor("ChartLine");
        var fillStart = ThemeManager.GetWpfColor("ChartFillStart");
        var fillEnd = ThemeManager.GetWpfColor("ChartFillEnd");
        var dotStroke = ThemeManager.GetWpfColor("ChartDotStroke");

        // 填充区域
        var fillPoints = new PointCollection(points);
        fillPoints.Add(new Point(points.Last().X, chartH + 4));
        fillPoints.Add(new Point(points.First().X, chartH + 4));

        var fillPolygon = new Polygon
        {
            Points = fillPoints,
            Fill = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(fillStart, 0),
                    new GradientStop(fillEnd, 1)
                }
            },
            Stroke = Brushes.Transparent
        };
        TrendChart.Children.Add(fillPolygon);

        // 折线
        var line = new Polyline
        {
            Points = points,
            Stroke = new SolidColorBrush(chartLine),
            StrokeThickness = 2,
            StrokeLineJoin = PenLineJoin.Round
        };
        TrendChart.Children.Add(line);

        // 数据点
        foreach (var pt in points)
        {
            var dot = new Ellipse
            {
                Width = 4,
                Height = 4,
                Fill = new SolidColorBrush(chartLine),
                Stroke = new SolidColorBrush(dotStroke),
                StrokeThickness = 1
            };
            Canvas.SetLeft(dot, pt.X - 2);
            Canvas.SetTop(dot, pt.Y - 2);
            TrendChart.Children.Add(dot);
        }
    }

    // ─────────────────────────────────────────
    // 按钮事件
    // ─────────────────────────────────────────
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await _vm.RefreshAsync();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.RequestSettings();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    // ─────────────────────────────────────────
    // 公开方法
    // ─────────────────────────────────────────
    public void ShowAndExpand()
    {
        Show();
        PositionAtScreenEdge();
        ExpandPanel();
    }
}
