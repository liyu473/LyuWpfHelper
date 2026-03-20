using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shell;

namespace LyuWpfHelper.Controls;

/// <summary>
/// HandyControl-style custom title bar window with rich non-client area customization.
/// </summary>
public class CustomTitleBarWindow : Window
{
    private const int WmGetMinMaxInfo = 0x0024;
    private const int WmWindowPosChanged = 0x0047;
    private const int WmNcLButtonDown = 0x00A1;
    private const int HtCaption = 0x02;
    private const uint AbmGetState = 0x00000004;
    private const uint AbsAutoHide = 0x00000001;
    private const int MonitorDefaultToNearest = 0x00000002;
    private const double TitleBarCaptionFix = 7d;
    private const string PartTitleBar = "PART_TitleBar";

    private FrameworkElement? _titleBar;
    private HwndSource? _hwndSource;
    private Thickness _actualBorderThickness;
    private Thickness _commonPadding;
    private double _normalTitleBarHeight;
    private bool _isLoadedInitialized;

    public static readonly DependencyProperty TitleBarContentProperty =
        DependencyProperty.Register(
            nameof(TitleBarContent),
            typeof(object),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(null));

    public static readonly DependencyProperty TitleBarHeightProperty =
        DependencyProperty.Register(
            nameof(TitleBarHeight),
            typeof(double),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(29d, OnTitleBarHeightChanged));

    public static readonly DependencyProperty TitleBarBackgroundProperty =
        DependencyProperty.Register(
            nameof(TitleBarBackground),
            typeof(Brush),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(Brushes.WhiteSmoke));

    public static readonly DependencyProperty TitleBarForegroundProperty =
        DependencyProperty.Register(
            nameof(TitleBarForeground),
            typeof(Brush),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(Brushes.Black));

    public static readonly DependencyProperty ShowTitleProperty =
        DependencyProperty.Register(
            nameof(ShowTitle),
            typeof(bool),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowIconProperty =
        DependencyProperty.Register(
            nameof(ShowIcon),
            typeof(bool),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowMinButtonProperty =
        DependencyProperty.Register(
            nameof(ShowMinButton),
            typeof(bool),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowMaxButtonProperty =
        DependencyProperty.Register(
            nameof(ShowMaxButton),
            typeof(bool),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowCloseButtonProperty =
        DependencyProperty.Register(
            nameof(ShowCloseButton),
            typeof(bool),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(true));

    public static readonly DependencyProperty TitleAlignmentProperty =
        DependencyProperty.Register(
            nameof(TitleAlignment),
            typeof(HorizontalAlignment),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(HorizontalAlignment.Left));

    public static readonly DependencyProperty TitleMarginProperty =
        DependencyProperty.Register(
            nameof(TitleMargin),
            typeof(Thickness),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(new Thickness(10, 0, 0, 0)));

    public static readonly DependencyProperty CloseButtonBackgroundProperty =
        DependencyProperty.Register(
            nameof(CloseButtonBackground),
            typeof(Brush),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(Brushes.Transparent));

    public static readonly DependencyProperty CloseButtonForegroundProperty =
        DependencyProperty.Register(
            nameof(CloseButtonForeground),
            typeof(Brush),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(Brushes.Black));

    public static readonly DependencyProperty CloseButtonHoverBackgroundProperty =
        DependencyProperty.Register(
            nameof(CloseButtonHoverBackground),
            typeof(Brush),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E81123")!)));

    public static readonly DependencyProperty CloseButtonHoverForegroundProperty =
        DependencyProperty.Register(
            nameof(CloseButtonHoverForeground),
            typeof(Brush),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(Brushes.White));

    public static readonly DependencyProperty OtherButtonBackgroundProperty =
        DependencyProperty.Register(
            nameof(OtherButtonBackground),
            typeof(Brush),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(Brushes.Transparent));

    public static readonly DependencyProperty OtherButtonForegroundProperty =
        DependencyProperty.Register(
            nameof(OtherButtonForeground),
            typeof(Brush),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(Brushes.Black));

    public static readonly DependencyProperty OtherButtonHoverBackgroundProperty =
        DependencyProperty.Register(
            nameof(OtherButtonHoverBackground),
            typeof(Brush),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6E6E6")!)));

    public static readonly DependencyProperty OtherButtonHoverForegroundProperty =
        DependencyProperty.Register(
            nameof(OtherButtonHoverForeground),
            typeof(Brush),
            typeof(CustomTitleBarWindow),
            new PropertyMetadata(Brushes.Black));

    static CustomTitleBarWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CustomTitleBarWindow),
            new FrameworkPropertyMetadata(typeof(CustomTitleBarWindow)));
    }

    public CustomTitleBarWindow()
    {
        SetResourceReference(StyleProperty, typeof(CustomTitleBarWindow));
        Loaded += OnLoaded;

        CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, (_, _) => SystemCommands.MinimizeWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, (_, _) => SystemCommands.MaximizeWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, (_, _) => SystemCommands.RestoreWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (_, _) => SystemCommands.CloseWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.ShowSystemMenuCommand, (_, _) => ShowSystemMenu()));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_titleBar != null)
        {
            _titleBar.MouseLeftButtonDown -= OnTitleBarMouseLeftButtonDown;
        }

        _titleBar = GetTemplateChild(PartTitleBar) as FrameworkElement;
        if (_titleBar != null)
        {
            _titleBar.MouseLeftButtonDown += OnTitleBarMouseLeftButtonDown;
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        _hwndSource?.AddHook(WindowProc);
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        ApplyWindowChrome(WindowState);
        ApplyWindowStateLayout(WindowState);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(WindowProc);
            _hwndSource = null;
        }

        if (_titleBar != null)
        {
            _titleBar.MouseLeftButtonDown -= OnTitleBarMouseLeftButtonDown;
        }

        base.OnClosed(e);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoadedInitialized)
        {
            return;
        }

        _actualBorderThickness = BorderThickness;
        _commonPadding = Padding;
        _normalTitleBarHeight = TitleBarHeight;
        _isLoadedInitialized = true;

        ApplyWindowChrome(WindowState);
        ApplyWindowStateLayout(WindowState);
    }

    private static void OnTitleBarHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CustomTitleBarWindow window)
        {
            return;
        }

        if (window.WindowState != WindowState.Maximized)
        {
            window._normalTitleBarHeight = (double)e.NewValue;
        }

        window.ApplyWindowChrome(window.WindowState);
    }

    private void ApplyWindowChrome(WindowState state)
    {
        var chrome = WindowChrome.GetWindowChrome(this) ?? new WindowChrome();
        chrome.CornerRadius = new CornerRadius(8);
        chrome.GlassFrameThickness = new Thickness(-1);
        chrome.UseAeroCaptionButtons = false;
        chrome.ResizeBorderThickness = state == WindowState.Maximized ? new Thickness(0) : new Thickness(6);
        chrome.CaptionHeight = Math.Max(0, TitleBarHeight - TitleBarCaptionFix);
        BindingOperations.SetBinding(
            chrome,
            WindowChrome.CaptionHeightProperty,
            new Binding(nameof(TitleBarHeight)) { Source = this, Mode = BindingMode.OneWay });
        WindowChrome.SetWindowChrome(this, chrome);
    }

    private void ApplyWindowStateLayout(WindowState state)
    {
        if (!_isLoadedInitialized)
        {
            return;
        }

        if (state == WindowState.Maximized)
        {
            BorderThickness = new Thickness(0);
            Padding = GetWindowMaximizedPadding();
            if (!AreClose(TitleBarHeight, _normalTitleBarHeight))
            {
                TitleBarHeight = _normalTitleBarHeight;
            }
        }
        else
        {
            BorderThickness = _actualBorderThickness;
            Padding = _commonPadding;

            if (!AreClose(TitleBarHeight, _normalTitleBarHeight))
            {
                TitleBarHeight = _normalTitleBarHeight;
            }
        }
    }

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        if (ShouldIgnoreDrag(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (e.ClickCount == 2 && CanToggleMaxRestore())
        {
            if (WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
            }
            else
            {
                SystemCommands.MaximizeWindow(this);
            }

            e.Handled = true;
            return;
        }

        BeginNativeCaptionDrag();
        e.Handled = true;
    }

    private bool CanToggleMaxRestore() =>
        ResizeMode is not ResizeMode.NoResize and not ResizeMode.CanMinimize && ShowMaxButton;

    private void BeginNativeCaptionDrag()
    {
        IntPtr handle = new WindowInteropHelper(this).EnsureHandle();
        ReleaseCapture();
        SendMessage(handle, WmNcLButtonDown, new IntPtr(HtCaption), IntPtr.Zero);
    }

    private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WmWindowPosChanged:
                Padding = WindowState == WindowState.Maximized ? GetWindowMaximizedPadding() : _commonPadding;
                break;
            case WmGetMinMaxInfo:
                UpdateMinMaxInfo(hwnd, lParam);
                Padding = WindowState == WindowState.Maximized ? GetWindowMaximizedPadding() : _commonPadding;
                break;
        }

        return IntPtr.Zero;
    }

    private static void UpdateMinMaxInfo(IntPtr hwnd, IntPtr lParam)
    {
        MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

        if (IsTaskbarAutoHide())
        {
            IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new()
                {
                    cbSize = Marshal.SizeOf<MONITORINFO>()
                };

                if (GetMonitorInfo(monitor, ref monitorInfo))
                {
                    RECT workArea = monitorInfo.rcWork;
                    RECT monitorArea = monitorInfo.rcMonitor;
                    mmi.ptMaxPosition.x = Math.Abs(workArea.left - monitorArea.left);
                    mmi.ptMaxPosition.y = Math.Abs(workArea.top - monitorArea.top);
                    mmi.ptMaxSize.x = Math.Abs(workArea.right - workArea.left);
                    mmi.ptMaxSize.y = Math.Abs(workArea.bottom - workArea.top - 1);
                }
            }
        }

        Marshal.StructureToPtr(mmi, lParam, true);
    }

    private static Thickness GetWindowMaximizedPadding()
    {
        Thickness resize = SystemParameters.WindowResizeBorderThickness;
        double top = Math.Max(0, resize.Top + (IsTaskbarAutoHide() ? 0d : 4d));

        return new Thickness(
            0d,
            top,
            0d,
            0d);
    }

    private static bool IsTaskbarAutoHide()
    {
        APPBARDATA appBarData = new()
        {
            cbSize = Marshal.SizeOf<APPBARDATA>()
        };

        uint state = SHAppBarMessage(AbmGetState, ref appBarData);
        return (state & AbsAutoHide) == AbsAutoHide;
    }

    private void ShowSystemMenu()
    {
        Point pt = PointToScreen(new Point(0, TitleBarHeight));
        SystemCommands.ShowSystemMenu(this, pt);
    }

    private bool ShouldIgnoreDrag(DependencyObject? originalSource)
    {
        DependencyObject? current = originalSource;
        while (current is not null)
        {
            if (current == _titleBar)
            {
                return false;
            }

            if (current is ButtonBase ||
                current is TextBoxBase ||
                current is PasswordBox ||
                current is ComboBox ||
                current is ListBoxItem ||
                current is Slider ||
                current is ScrollBar ||
                current is Menu ||
                current is MenuItem)
            {
                return true;
            }

            current = GetParent(current);
        }

        return false;
    }

    private static DependencyObject? GetParent(DependencyObject current)
    {
        if (current is Visual or Visual3D)
        {
            return VisualTreeHelper.GetParent(current);
        }

        return LogicalTreeHelper.GetParent(current);
    }

    private static bool AreClose(double a, double b) =>
        Math.Abs(a - b) < 0.01d;

    public object? TitleBarContent
    {
        get => GetValue(TitleBarContentProperty);
        set => SetValue(TitleBarContentProperty, value);
    }

    public double TitleBarHeight
    {
        get => (double)GetValue(TitleBarHeightProperty);
        set => SetValue(TitleBarHeightProperty, value);
    }

    public Brush? TitleBarBackground
    {
        get => (Brush?)GetValue(TitleBarBackgroundProperty);
        set => SetValue(TitleBarBackgroundProperty, value);
    }

    public Brush? TitleBarForeground
    {
        get => (Brush?)GetValue(TitleBarForegroundProperty);
        set => SetValue(TitleBarForegroundProperty, value);
    }

    public bool ShowTitle
    {
        get => (bool)GetValue(ShowTitleProperty);
        set => SetValue(ShowTitleProperty, value);
    }

    public bool ShowIcon
    {
        get => (bool)GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }

    public bool ShowMinButton
    {
        get => (bool)GetValue(ShowMinButtonProperty);
        set => SetValue(ShowMinButtonProperty, value);
    }

    public bool ShowMaxButton
    {
        get => (bool)GetValue(ShowMaxButtonProperty);
        set => SetValue(ShowMaxButtonProperty, value);
    }

    public bool ShowCloseButton
    {
        get => (bool)GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, value);
    }

    public HorizontalAlignment TitleAlignment
    {
        get => (HorizontalAlignment)GetValue(TitleAlignmentProperty);
        set => SetValue(TitleAlignmentProperty, value);
    }

    public Thickness TitleMargin
    {
        get => (Thickness)GetValue(TitleMarginProperty);
        set => SetValue(TitleMarginProperty, value);
    }

    public Brush? CloseButtonBackground
    {
        get => (Brush?)GetValue(CloseButtonBackgroundProperty);
        set => SetValue(CloseButtonBackgroundProperty, value);
    }

    public Brush? CloseButtonForeground
    {
        get => (Brush?)GetValue(CloseButtonForegroundProperty);
        set => SetValue(CloseButtonForegroundProperty, value);
    }

    public Brush? CloseButtonHoverBackground
    {
        get => (Brush?)GetValue(CloseButtonHoverBackgroundProperty);
        set => SetValue(CloseButtonHoverBackgroundProperty, value);
    }

    public Brush? CloseButtonHoverForeground
    {
        get => (Brush?)GetValue(CloseButtonHoverForegroundProperty);
        set => SetValue(CloseButtonHoverForegroundProperty, value);
    }

    public Brush? OtherButtonBackground
    {
        get => (Brush?)GetValue(OtherButtonBackgroundProperty);
        set => SetValue(OtherButtonBackgroundProperty, value);
    }

    public Brush? OtherButtonForeground
    {
        get => (Brush?)GetValue(OtherButtonForegroundProperty);
        set => SetValue(OtherButtonForegroundProperty, value);
    }

    public Brush? OtherButtonHoverBackground
    {
        get => (Brush?)GetValue(OtherButtonHoverBackgroundProperty);
        set => SetValue(OtherButtonHoverBackgroundProperty, value);
    }

    public Brush? OtherButtonHoverForeground
    {
        get => (Brush?)GetValue(OtherButtonHoverForegroundProperty);
        set => SetValue(OtherButtonHoverForegroundProperty, value);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public RECT rc;
        public int lParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

    [DllImport("shell32.dll")]
    private static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
