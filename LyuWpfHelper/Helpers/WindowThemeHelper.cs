using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using LyuWpfHelper.Controls;
using Microsoft.Win32;

namespace LyuWpfHelper.Helpers;

/// <summary>
/// Window theme modes supported by <see cref="WindowThemeHelper"/>.
/// </summary>
public enum WindowThemeMode
{
    FollowSystem,
    Light,
    Dark,
}

/// <summary>
/// Applies theme colors for both <see cref="LyuWindow"/> and normal <see cref="Window"/>.
/// </summary>
public static class WindowThemeHelper
{
    private static readonly object SyncLock = new();
    private static readonly List<WeakReference<Window>> FollowSystemWindows = [];
    private static readonly DependencyProperty CurrentThemeProperty =
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
    private static readonly Brush LightTitleBarBackground = CreateBrush("#FFF5F5F5");
    private static readonly Brush LightTitleBarForeground = CreateBrush("#FF000000");
    private static readonly Brush LightOtherButtonHoverBackground = CreateBrush("#FFE6E6E6");

    private static readonly Brush DarkWindowBackground = CreateBrush("#FF202020");
    private static readonly Brush DarkWindowForeground = CreateBrush("#FFF3F3F3");
    private static readonly Brush DarkBorderBrush = CreateBrush("#FF404040");
    private static readonly Brush DarkTitleBarBackground = CreateBrush("#FF2B2B2B");
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

    private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window)
        {
            return;
        }

        WindowThemeMode themeMode = (WindowThemeMode)e.NewValue;
        if (themeMode == WindowThemeMode.FollowSystem)
        {
            RegisterFollowSystemWindow(window);
        }
        else
        {
            UnregisterFollowSystemWindow(window);
        }

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

        if (theme == WindowThemeMode.Dark)
        {
            window.Background = hasBackdrop ? TransparentBrush : DarkWindowBackground;
            window.Foreground = DarkWindowForeground;
            window.BorderBrush = DarkBorderBrush;
            window.TitleBarBackground = hasBackdrop ? TransparentBrush : DarkWindowBackground;
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

        window.Background = hasBackdrop ? TransparentBrush : LightWindowBackground;
        window.Foreground = LightWindowForeground;
        window.BorderBrush = LightBorderBrush;
        window.TitleBarBackground = hasBackdrop ? TransparentBrush : LightWindowBackground;
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

        if (theme == WindowThemeMode.Dark)
        {
            window.Background = hasBackdrop ? TransparentBrush : DarkWindowBackground;
            window.Foreground = DarkWindowForeground;
            window.BorderBrush = DarkBorderBrush;
            return;
        }

        window.Background = hasBackdrop ? TransparentBrush : LightWindowBackground;
        window.Foreground = LightWindowForeground;
        window.BorderBrush = LightBorderBrush;
    }

    private static Brush CreateBrush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
        brush.Freeze();
        return brush;
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
