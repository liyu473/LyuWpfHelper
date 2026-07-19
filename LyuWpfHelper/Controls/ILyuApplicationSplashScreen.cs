using System.Windows.Media;

namespace LyuWpfHelper.Controls;

/// <summary>
/// 定义 <see cref="LyuWindow"/> 首次显示时使用的启动屏幕。
/// </summary>
public interface ILyuApplicationSplashScreen
{
    /// <summary>
    /// 获取启动屏幕显示的应用名称。
    /// </summary>
    string? AppName { get; }

    /// <summary>
    /// 获取启动屏幕显示的应用图标。
    /// </summary>
    ImageSource? AppIcon { get; }

    /// <summary>
    /// 获取启动屏幕的自定义内容。设置后优先于应用图标和名称显示。
    /// </summary>
    object? SplashScreenContent { get; }

    /// <summary>
    /// 获取启动屏幕的最短显示时间（毫秒）。
    /// </summary>
    int MinimumShowTime { get; }

    /// <summary>
    /// 在后台线程执行应用初始化任务。
    /// </summary>
    Task RunTasks(CancellationToken cancellationToken);
}
