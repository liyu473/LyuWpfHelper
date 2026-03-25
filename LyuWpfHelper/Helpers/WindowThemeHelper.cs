using LyuWpfHelper.Controls;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LyuWpfHelper.Helpers;

/// <summary>
/// Window theme modes supported by <see cref="WindowThemeHelper"/>.
/// </summary>
public enum WindowThemeMode
{
    [Description("跟随系统")]
    FollowSystem,
    [Description("明亮")]
    Light,
    [Description("深色")]
    Dark,
}

/// <summary>
/// Applies theme colors for both <see cref="LyuWindow"/> and normal <see cref="Window"/>.
/// </summary>
public static class WindowThemeHelper
{
    private static readonly Duration DefaultThemeTransitionDuration = new(TimeSpan.FromMilliseconds(220));

    private const string LightWindowBackgroundBrushKey = "LyuWindowTheme.Light.WindowBackgroundBrush";
    private const string DarkWindowBackgroundBrushKey = "LyuWindowTheme.Dark.WindowBackgroundBrush";
    private const string LightBorderBrushKey = "LyuWindowTheme.Light.BorderBrush";
    private const string DarkBorderBrushKey = "LyuWindowTheme.Dark.BorderBrush";
    private const string LightTitleBarBackgroundBrushKey =
        "LyuWindowTheme.Light.TitleBarBackgroundBrush";
    private const string DarkTitleBarBackgroundBrushKey =
        "LyuWindowTheme.Dark.TitleBarBackgroundBrush";

    private static readonly object SyncLock = new();
    private static readonly List<WeakReference<Window>> FollowSystemWindows = [];
    public static readonly DependencyProperty CurrentThemeProperty =
        DependencyProperty.RegisterAttached(
            "CurrentTheme",
            typeof(WindowThemeMode),
            typeof(WindowThemeHelper),
            new PropertyMetadata(WindowThemeMode.Light)
        );

    public static readonly DependencyProperty ThemeProperty = DependencyProperty.RegisterAttached(
        "Theme",
        typeof(WindowThemeMode),
        typeof(WindowThemeHelper),
        new PropertyMetadata(WindowThemeMode.Light, OnThemeChanged)
    );

    private static readonly Brush LightWindowBackground = CreateBrush("#FFFFFFFF");
    private static readonly Brush LightWindowForeground = CreateBrush("#FF1F1F1F");
    private static readonly Brush LightBorderBrush = CreateBrush("#FFD6D6D6");
    private static readonly Brush LightTitleBarBackground = CreateBrush("#FFFFFFFF");
    private static readonly Brush LightTitleBarForeground = CreateBrush("#FF000000");
    private static readonly Brush LightOtherButtonHoverBackground = CreateBrush("#FFE6E6E6");

    private static readonly Brush DarkWindowBackground = CreateBrush("#FF202020");
    private static readonly Brush DarkWindowForeground = CreateBrush("#FFF3F3F3");
    private static readonly Brush DarkBorderBrush = CreateBrush("#FF404040");
    private static readonly Brush DarkTitleBarBackground = CreateBrush("#FF202020");
    private static readonly Brush DarkTitleBarForeground = CreateBrush("#FFF3F3F3");
    private static readonly Brush DarkOtherButtonHoverBackground = CreateBrush("#FF3A3A3A");

    private static readonly Brush TitleBarCloseHoverBackground = CreateBrush("#FFE81123");
    private static readonly Brush TitleBarCloseHoverForeground = CreateBrush("#FFFFFFFF");
    private static readonly Brush TransparentBrush = Brushes.Transparent;

    static WindowThemeHelper()
    {
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public static WindowThemeMode GetTheme(DependencyObject obj)
    {
        return (WindowThemeMode)obj.GetValue(ThemeProperty);
    }

    public static void SetTheme(DependencyObject obj, WindowThemeMode value)
    {
        obj.SetValue(ThemeProperty, value);
    }

    public static void ApplyTheme(Window window, WindowThemeMode theme)
    {
        if (window is null)
        {
            return;
        }

        if (theme == WindowThemeMode.FollowSystem)
        {
            RegisterFollowSystemWindow(window);
        }
        else
        {
            UnregisterFollowSystemWindow(window);
        }

        window.SetValue(CurrentThemeProperty, theme);

        WindowThemeMode effectiveTheme = ResolveThemeMode(theme);
        WindowBackdropHelper.SetImmersiveDarkMode(window, effectiveTheme == WindowThemeMode.Dark);

        if (window is LyuWindow lyuWindow)
        {
            ApplyLyuWindowTheme(lyuWindow, effectiveTheme);
            lyuWindow.NotifyThemeChanged(theme, effectiveTheme);
            return;
        }

        ApplyNormalWindowTheme(window, effectiveTheme);
    }

    public static WindowThemeMode GetCurrentTheme(DependencyObject obj)
    {
        return (WindowThemeMode)obj.GetValue(CurrentThemeProperty);
    }

    public static void SetCurrentTheme(DependencyObject obj, WindowThemeMode value)
    {
        obj.SetValue(CurrentThemeProperty, value);
    }

    private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window)
        {
            return;
        }

        WindowThemeMode themeMode = (WindowThemeMode)e.NewValue;
        ApplyTheme(window, themeMode);
    }

    private static WindowThemeMode ResolveThemeMode(WindowThemeMode theme)
    {
        if (theme != WindowThemeMode.FollowSystem)
        {
            return theme;
        }

        return IsSystemDarkTheme() ? WindowThemeMode.Dark : WindowThemeMode.Light;
    }

    public static WindowThemeMode GetEffectiveTheme(WindowThemeMode theme)
    {
        return ResolveThemeMode(theme);
    }

    private static void ApplyLyuWindowTheme(LyuWindow window, WindowThemeMode theme)
    {
        bool hasBackdrop = WindowBackdropHelper.GetBackdrop(window) != WindowBackdropType.Default;
        bool animate = !hasBackdrop;
        Brush lightBackground = ResolveThemeBrush(
            window,
            LightWindowBackgroundBrushKey,
            LightWindowBackground
        );
        Brush darkBackground = ResolveThemeBrush(
            window,
            DarkWindowBackgroundBrushKey,
            DarkWindowBackground
        );
        Brush lightBorder = ResolveThemeBrush(window, LightBorderBrushKey, LightBorderBrush);
        Brush darkBorder = ResolveThemeBrush(window, DarkBorderBrushKey, DarkBorderBrush);
        Brush lightTitleBarBackground = ResolveThemeBrush(
            window,
            LightTitleBarBackgroundBrushKey,
            LightTitleBarBackground
        );
        Brush darkTitleBarBackground = ResolveThemeBrush(
            window,
            DarkTitleBarBackgroundBrushKey,
            DarkTitleBarBackground
        );

        if (theme == WindowThemeMode.Dark)
        {
            ApplyBrushWithTransition(
                window,
                Window.BackgroundProperty,
                hasBackdrop ? TransparentBrush : darkBackground,
                animate
            );
            window.Foreground = DarkWindowForeground;
            ApplyBrushWithTransition(window, Window.BorderBrushProperty, darkBorder, animate);
            ApplyBrushWithTransition(
                window,
                LyuWindow.TitleBarBackgroundProperty,
                hasBackdrop ? TransparentBrush : darkTitleBarBackground,
                animate
            );
            window.TitleBarForeground = DarkTitleBarForeground;
            window.OtherButtonBackground = TransparentBrush;
            window.OtherButtonForeground = DarkTitleBarForeground;
            window.OtherButtonHoverBackground = DarkOtherButtonHoverBackground;
            window.OtherButtonHoverForeground = DarkTitleBarForeground;
            window.CloseButtonBackground = TransparentBrush;
            window.CloseButtonForeground = DarkTitleBarForeground;
            window.CloseButtonHoverBackground = TitleBarCloseHoverBackground;
            window.CloseButtonHoverForeground = TitleBarCloseHoverForeground;
            return;
        }

        ApplyBrushWithTransition(
            window,
            Window.BackgroundProperty,
            hasBackdrop ? TransparentBrush : lightBackground,
            animate
        );
        window.Foreground = LightWindowForeground;
        ApplyBrushWithTransition(window, Window.BorderBrushProperty, lightBorder, animate);
        ApplyBrushWithTransition(
            window,
            LyuWindow.TitleBarBackgroundProperty,
            hasBackdrop ? TransparentBrush : lightTitleBarBackground,
            animate
        );
        window.TitleBarForeground = LightTitleBarForeground;
        window.OtherButtonBackground = TransparentBrush;
        window.OtherButtonForeground = LightTitleBarForeground;
        window.OtherButtonHoverBackground = LightOtherButtonHoverBackground;
        window.OtherButtonHoverForeground = LightTitleBarForeground;
        window.CloseButtonBackground = TransparentBrush;
        window.CloseButtonForeground = LightTitleBarForeground;
        window.CloseButtonHoverBackground = TitleBarCloseHoverBackground;
        window.CloseButtonHoverForeground = TitleBarCloseHoverForeground;
    }

    private static void ApplyNormalWindowTheme(Window window, WindowThemeMode theme)
    {
        bool hasBackdrop = WindowBackdropHelper.GetBackdrop(window) != WindowBackdropType.Default;
        bool animate = !hasBackdrop;
        Brush lightBackground = ResolveThemeBrush(
            window,
            LightWindowBackgroundBrushKey,
            LightWindowBackground
        );
        Brush darkBackground = ResolveThemeBrush(
            window,
            DarkWindowBackgroundBrushKey,
            DarkWindowBackground
        );
        Brush lightBorder = ResolveThemeBrush(window, LightBorderBrushKey, LightBorderBrush);
        Brush darkBorder = ResolveThemeBrush(window, DarkBorderBrushKey, DarkBorderBrush);

        if (theme == WindowThemeMode.Dark)
        {
            ApplyBrushWithTransition(
                window,
                Window.BackgroundProperty,
                hasBackdrop ? TransparentBrush : darkBackground,
                animate
            );
            window.Foreground = DarkWindowForeground;
            ApplyBrushWithTransition(window, Window.BorderBrushProperty, darkBorder, animate);
            return;
        }

        ApplyBrushWithTransition(
            window,
            Window.BackgroundProperty,
            hasBackdrop ? TransparentBrush : lightBackground,
            animate
        );
        window.Foreground = LightWindowForeground;
        ApplyBrushWithTransition(window, Window.BorderBrushProperty, lightBorder, animate);
    }

    private static Brush CreateBrush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
        brush.Freeze();
        return brush;
    }

    private static Brush ResolveThemeBrush(Window window, string key, Brush fallback)
    {
        if (window.TryFindResource(key) is Brush windowBrush)
        {
            return windowBrush;
        }

        if (Application.Current?.TryFindResource(key) is Brush appBrush)
        {
            return appBrush;
        }

        return fallback;
    }

    private static void ApplyBrushWithTransition(
        Window window,
        DependencyProperty property,
        Brush targetBrush,
        bool animate
    )
    {
        if (!animate)
        {
            window.SetValue(property, targetBrush);
            return;
        }

        if (targetBrush is not SolidColorBrush targetSolid)
        {
            window.SetValue(property, targetBrush);
            return;
        }

        Brush? currentBrush = window.GetValue(property) as Brush;
        Color startColor =
            currentBrush is SolidColorBrush currentSolid
                ? currentSolid.Color
                : Color.FromArgb(0, targetSolid.Color.R, targetSolid.Color.G, targetSolid.Color.B);

        if (startColor == targetSolid.Color)
        {
            window.SetValue(property, targetBrush);
            return;
        }

        var animatedBrush = new SolidColorBrush(startColor);
        window.SetValue(property, animatedBrush);

        var animation = new ColorAnimation
        {
            To = targetSolid.Color,
            Duration = DefaultThemeTransitionDuration,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            FillBehavior = FillBehavior.HoldEnd,
        };

        animation.Completed += (_, _) => window.SetValue(property, targetBrush);
        animatedBrush.BeginAnimation(
            SolidColorBrush.ColorProperty,
            animation,
            HandoffBehavior.SnapshotAndReplace
        );
    }

    private static bool IsSystemDarkTheme()
    {
        try
        {
            using RegistryKey? personalizeKey = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"
            );

            object? value = personalizeKey?.GetValue("AppsUseLightTheme");
            if (value is int intValue)
            {
                return intValue == 0;
            }

            if (value is byte byteValue)
            {
                return byteValue == 0;
            }
        }
        catch
        {
            // Fallback to light theme when system value cannot be read.
        }

        return false;
    }

    private static void RegisterFollowSystemWindow(Window window)
    {
        lock (SyncLock)
        {
            CleanupDeadWindows();
            if (FollowSystemWindows.Any(wr => wr.TryGetTarget(out Window? w) && ReferenceEquals(w, window)))
            {
                return;
            }

            FollowSystemWindows.Add(new WeakReference<Window>(window));
        }

        window.Closed -= Window_Closed;
        window.Closed += Window_Closed;
    }

    private static void UnregisterFollowSystemWindow(Window window)
    {
        lock (SyncLock)
        {
            _ = FollowSystemWindows.RemoveAll(
                wr => !wr.TryGetTarget(out Window? current) || ReferenceEquals(current, window)
            );
        }

        window.Closed -= Window_Closed;
    }

    private static void Window_Closed(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            UnregisterFollowSystemWindow(window);
        }
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (
            e.Category
            is not UserPreferenceCategory.Color
            and not UserPreferenceCategory.General
            and not UserPreferenceCategory.VisualStyle
        )
        {
            return;
        }

        List<Window> windowsToRefresh = [];
        lock (SyncLock)
        {
            CleanupDeadWindows();
            foreach (WeakReference<Window> weakWindow in FollowSystemWindows)
            {
                if (weakWindow.TryGetTarget(out Window? window))
                {
                    windowsToRefresh.Add(window);
                }
            }
        }

        foreach (Window window in windowsToRefresh)
        {
            if (!window.Dispatcher.CheckAccess())
            {
                _ = window.Dispatcher.InvokeAsync(() => ApplyTheme(window, WindowThemeMode.FollowSystem));
            }
            else
            {
                ApplyTheme(window, WindowThemeMode.FollowSystem);
            }
        }
    }

    private static void CleanupDeadWindows()
    {
        _ = FollowSystemWindows.RemoveAll(wr => !wr.TryGetTarget(out _));
    }
}
