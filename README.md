# DeepSeek Token Monitor

DeepSeek API 用量监控桌面小组件 —— 一个轻量级的 Windows 侧边栏应用，实时展示你的 DeepSeek 账户余额、消耗趋势和用量预估。

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![License](https://img.shields.io/badge/license-MIT-green)

## 功能特性

- **实时余额监控** — 定时轮询 DeepSeek `/user/balance` API，显示可用余额（支持充值余额与赠送余额）
- **消耗统计** — 自动记录并计算今日、本周、本月及累计消耗金额
- **趋势图表** — 7 日消耗迷你折线图，直观展示用量走势
- **预估可用天数** — 基于日均消耗自动估算余额可用天数
- **桌面侧边栏** — 透明无边框窗口，鼠标悬停展开/收起，不遮挡日常工作
- **深浅主题** — 支持极光白与暗夜黑两种主题，设置中一键切换
- **系统托盘** — 最小化后驻留托盘，双击恢复显示
- **开机自启** — 可选开机自动运行（写入注册表 `Run` 项）
- **单文件发布** — 可打包为独立 exe，无需安装 .NET 运行时

## 运行要求

- Windows 10 / 11
- DeepSeek API Key（在 [DeepSeek 开放平台](https://platform.deepseek.com/) 获取）

## 快速开始

### 直接使用

下载 Release 中的 `DeepSeekTokenMonitor.exe`，双击运行即可。首次启动会弹出设置窗口，填入你的 API Key 后保存。

### 从源码构建

需要安装 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

```bash
# 克隆仓库
git clone https://github.com/hanyu19951207/DeepSeekTokenMonitor.git
cd DeepSeekTokenMonitor

# 调试运行
dotnet run

# 打包为独立 exe
dotnet publish -c Release --self-contained -r win-x64 -p:PublishSingleFile=true -o publish
```

打包完成后，`publish/` 目录中的 `DeepSeekTokenMonitor.exe` 即为可独立运行的程序。

## 使用说明

1. **首次运行** — 程序启动后自动打开设置窗口，填入 DeepSeek API Key 并保存
2. **侧边栏交互** — 鼠标移至屏幕右侧边缘的触发条，面板自动展开；移开后自动收起
3. **刷新数据** — 默认每 5 分钟自动刷新一次，也可点击面板底部的刷新按钮立即更新
4. **设置选项** — 点击齿轮图标进入设置，可调整 API Key、刷新间隔、侧边栏位置、主题和开机自启
5. **最小化** — 点击 ✕ 按钮不会退出程序，而是最小化到系统托盘；双击托盘图标可重新显示

## 项目结构

```
DeepSeekTokenMonitor/
├── Models/
│   ├── BalanceResponse.cs      # API 响应模型
│   └── UsageSnapshot.cs        # 用量快照模型
├── Services/
│   ├── DeepSeekApiService.cs   # DeepSeek API 调用
│   ├── SettingsService.cs      # 设置持久化
│   ├── ThemeManager.cs         # 深浅主题管理
│   └── UsageTracker.cs         # 用量追踪与统计
├── ViewModels/
│   └── MainViewModel.cs        # 主视图模型（MVVM）
├── Windows/
│   ├── SettingsWindow.xaml     # 设置窗口 UI
│   └── SettingsWindow.xaml.cs  # 设置窗口逻辑
├── App.xaml / App.xaml.cs      # 应用入口与托盘管理
├── AppServices.cs              # 全局服务容器
├── MainWindow.xaml             # 主窗口 UI
├── MainWindow.xaml.cs          # 主窗口逻辑
└── DeepSeekTokenMonitor.csproj # 项目文件
```

## 数据存储

设置和用量快照保存在 `%AppData%/DeepSeekTokenMonitor/` 目录下：

- `settings.json` — API Key、刷新间隔、主题等用户设置
- `usage_snapshots.json` — 余额快照历史记录（保留最近 30 天）

## 技术栈

- **C# / WPF** (.NET 8) — 桌面 UI 框架
- **System.Text.Json** — JSON 序列化（支持 API 返回的字符串数值自动转换）
- **System.Windows.Forms.NotifyIcon** — 系统托盘图标
- **MVVM 模式** — View / ViewModel 分离

## License

[MIT](LICENSE)
