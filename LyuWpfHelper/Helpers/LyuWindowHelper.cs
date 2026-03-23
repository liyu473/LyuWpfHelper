using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace LyuWpfHelper.Helpers;

/// <summary>
/// Window 辅助类，提供窗口全屏等功能
/// </summary>
public static class LyuWindowHelper
{
    #region IsFullScreen 附加属性

    /// <summary>
    /// IsFullScreen 附加属性
    /// 设置为 true 时，窗口将进入全屏模式（隐藏标题栏、任务栏等）
    /// 设置为 false 时，窗口将恢复到之前的状态
    /// </summary>
    public static readonly DependencyProperty IsFullScreenProperty =
        DependencyProperty.RegisterAttached(
            "IsFullScreen",
            typeof(bool),
            typeof(LyuWindowHelper),
            new PropertyMetadata(false, OnIsFullScreenChanged));

    public static bool GetIsFullScreen(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsFullScreenProperty);
    }

    public static void SetIsFullScreen(DependencyObject obj, bool value)
    {
        obj.SetValue(IsFullScreenProperty, value);
    }

    private static void OnIsFullScreenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window)
            return;

        bool isFullScreen = (bool)e.NewValue;

        if (isFullScreen)
        {
            EnterFullScreen(window);
        }
        else
        {
            ExitFullScreen(window);
        }
    }

    #endregion

    #region FullScreenKey 附加属性

    /// <summary>
    /// FullScreenKey 附加属性
    /// 设置一个快捷键来切换全屏模式
    /// 示例: "F11" 或 "Alt+Enter"
    /// </summary>
    public static readonly DependencyProperty FullScreenKeyProperty =
        DependencyProperty.RegisterAttached(
            "FullScreenKey",
            typeof(Key),
            typeof(LyuWindowHelper),
            new PropertyMetadata(Key.None, OnFullScreenKeyChanged));

    public static Key GetFullScreenKey(DependencyObject obj)
    {
        return (Key)obj.GetValue(FullScreenKeyProperty);
    }

    public static void SetFullScreenKey(DependencyObject obj, Key value)
    {
        obj.SetValue(FullScreenKeyProperty, value);
    }

    private static void OnFullScreenKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window)
            return;

        // 移除旧的事件处理
        window.KeyDown -= Window_KeyDown;

        // 如果设置了快捷键，添加新的事件处理
        if ((Key)e.NewValue != Key.None)
        {
            window.KeyDown += Window_KeyDown;
        }
    }

    private static void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not Window window)
            return;

        Key fullScreenKey = GetFullScreenKey(window);
        if (e.Key == fullScreenKey)
        {
            bool isFullScreen = GetIsFullScreen(window);
            SetIsFullScreen(window, !isFullScreen);
            e.Handled = true;
        }
    }

    #endregion

    #region 私有字段存储

    // 用于存储窗口进入全屏前的状态
    private static readonly DependencyProperty OriginalWindowStateProperty =
        DependencyProperty.RegisterAttached(
            "OriginalWindowState",
            typeof(WindowState),
            typeof(LyuWindowHelper),
            new PropertyMetadata(WindowState.Normal));

    private static readonly DependencyProperty OriginalWindowStyleProperty =
        DependencyProperty.RegisterAttached(
            "OriginalWindowStyle",
            typeof(WindowStyle),
            typeof(LyuWindowHelper),
            new PropertyMetadata(WindowStyle.SingleBorderWindow));

    private static readonly DependencyProperty OriginalResizeModeProperty =
        DependencyProperty.RegisterAttached(
            "OriginalResizeMode",
            typeof(ResizeMode),
            typeof(LyuWindowHelper),
            new PropertyMetadata(ResizeMode.CanResize));

    private static readonly DependencyProperty OriginalTopProperty =
        DependencyProperty.RegisterAttached(
            "OriginalTop",
            typeof(double),
            typeof(LyuWindowHelper),
            new PropertyMetadata(0.0));

    private static readonly DependencyProperty OriginalLeftProperty =
        DependencyProperty.RegisterAttached(
            "OriginalLeft",
            typeof(double),
            typeof(LyuWindowHelper),
            new PropertyMetadata(0.0));

    private static readonly DependencyProperty OriginalWidthProperty =
        DependencyProperty.RegisterAttached(
            "OriginalWidth",
            typeof(double),
            typeof(LyuWindowHelper),
            new PropertyMetadata(800.0));

    private static readonly DependencyProperty OriginalHeightProperty =
        DependencyProperty.RegisterAttached(
            "OriginalHeight",
            typeof(double),
            typeof(LyuWindowHelper),
            new PropertyMetadata(600.0));

    private static readonly DependencyProperty OriginalTopmostProperty =
        DependencyProperty.RegisterAttached(
            "OriginalTopmost",
            typeof(bool),
            typeof(LyuWindowHelper),
            new PropertyMetadata(false));

    #endregion

    #region Win32 API

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    #endregion

    #region 全屏逻辑

    /// <summary>
    /// 进入全屏模式
    /// </summary>
    private static void EnterFullScreen(Window window)
    {
        // 保存当前窗口状态
        window.SetValue(OriginalWindowStateProperty, window.WindowState);
        window.SetValue(OriginalWindowStyleProperty, window.WindowStyle);
        window.SetValue(OriginalResizeModeProperty, window.ResizeMode);
        window.SetValue(OriginalTopProperty, window.Top);
        window.SetValue(OriginalLeftProperty, window.Left);
        window.SetValue(OriginalWidthProperty, window.Width);
        window.SetValue(OriginalHeightProperty, window.Height);
        window.SetValue(OriginalTopmostProperty, window.Topmost);

        // 设置全屏属性
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.NoResize;
        window.Topmost = true;
        window.WindowState = WindowState.Normal; // 先设置为 Normal，避免最大化状态的边距

        // 获取当前屏幕的工作区域
        var hwnd = new WindowInteropHelper(window).Handle;
        var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

        if (monitor != IntPtr.Zero)
        {
            var monitorInfo = new MONITORINFO
            {
                cbSize = (uint)Marshal.SizeOf<MONITORINFO>()
            };

            if (GetMonitorInfo(monitor, ref monitorInfo))
            {
                var rect = monitorInfo.rcMonitor;
                
                // 获取 DPI 缩放因子，将物理像素转换为 WPF 逻辑像素
                var source = PresentationSource.FromVisual(window);
                double dpiScaleX = 1.0;
                double dpiScaleY = 1.0;
                
                if (source?.CompositionTarget != null)
                {
                    var transform = source.CompositionTarget.TransformFromDevice;
                    dpiScaleX = transform.M11;
                    dpiScaleY = transform.M22;
                }
                
                // 设置窗口位置和大小为整个屏幕（转换为逻辑像素）
                window.Left = rect.Left * dpiScaleX;
                window.Top = rect.Top * dpiScaleY;
                window.Width = (rect.Right - rect.Left) * dpiScaleX;
                window.Height = (rect.Bottom - rect.Top) * dpiScaleY;
            }
        }
        else
        {
            // 如果无法获取屏幕信息，使用系统参数（已经是逻辑像素）
            window.Left = 0;
            window.Top = 0;
            window.Width = SystemParameters.PrimaryScreenWidth;
            window.Height = SystemParameters.PrimaryScreenHeight;
        }
    }

    /// <summary>
    /// 退出全屏模式
    /// </summary>
    private static void ExitFullScreen(Window window)
    {
        // 恢复窗口状态
        window.WindowStyle = (WindowStyle)window.GetValue(OriginalWindowStyleProperty);
        window.ResizeMode = (ResizeMode)window.GetValue(OriginalResizeModeProperty);
        window.Topmost = (bool)window.GetValue(OriginalTopmostProperty);

        // 恢复窗口位置和大小
        window.Left = (double)window.GetValue(OriginalLeftProperty);
        window.Top = (double)window.GetValue(OriginalTopProperty);
        window.Width = (double)window.GetValue(OriginalWidthProperty);
        window.Height = (double)window.GetValue(OriginalHeightProperty);

        // 最后恢复窗口状态
        window.WindowState = (WindowState)window.GetValue(OriginalWindowStateProperty);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 切换窗口的全屏状态
    /// </summary>
    /// <param name="window">要切换的窗口</param>
    public static void ToggleFullScreen(Window window)
    {
        if (window == null)
            return;

        bool isFullScreen = GetIsFullScreen(window);
        SetIsFullScreen(window, !isFullScreen);
    }

    #endregion
}
