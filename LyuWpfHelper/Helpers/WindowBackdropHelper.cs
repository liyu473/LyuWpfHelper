using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LyuWpfHelper.Helpers;

/// <summary>
/// Window backdrop type enumeration
/// </summary>
public enum WindowBackdropType
{
    /// <summary>
    /// Default white background (no backdrop effect)
    /// </summary>
    Default = 0,

    /// <summary>
    /// Acrylic backdrop effect (Windows 11+)
    /// </summary>
    Acrylic = 1,

    /// <summary>
    /// Mica backdrop effect (Windows 11+)
    /// </summary>
    Mica = 2,

    /// <summary>
    /// Tabbed backdrop effect - optimized for tabbed windows (Windows 11 22H2+)
    /// </summary>
    Tabbed = 3
}

/// <summary>
/// Helper class for setting window backdrop effects (Acrylic, Mica) on any WPF Window.
/// Works on Windows 11 and later versions.
/// </summary>
public static class WindowBackdropHelper
{
    private const int DwmwaSystemBackdropType = 38;
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    /// <summary>
    /// Backdrop attached property
    /// </summary>
    public static readonly DependencyProperty BackdropProperty =
        DependencyProperty.RegisterAttached(
            "Backdrop",
            typeof(WindowBackdropType),
            typeof(WindowBackdropHelper),
            new PropertyMetadata(WindowBackdropType.Default, OnBackdropChanged));

    /// <summary>
    /// Gets the backdrop type for the specified window
    /// </summary>
    public static WindowBackdropType GetBackdrop(DependencyObject obj)
    {
        return (WindowBackdropType)obj.GetValue(BackdropProperty);
    }

    /// <summary>
    /// Sets the backdrop type for the specified window
    /// </summary>
    public static void SetBackdrop(DependencyObject obj, WindowBackdropType value)
    {
        obj.SetValue(BackdropProperty, value);
    }

    /// <summary>
    /// Sets immersive dark mode for a window so DWM backdrop/title can render dark variant.
    /// </summary>
    public static void SetImmersiveDarkMode(Window window, bool enabled)
    {
        if (window is null)
        {
            return;
        }

        if (PresentationSource.FromVisual(window) != null)
        {
            ApplyImmersiveDarkMode(window, enabled);
        }
        else
        {
            void ApplyWhenReady(object? sender, EventArgs args)
            {
                window.SourceInitialized -= ApplyWhenReady;
                ApplyImmersiveDarkMode(window, enabled);
            }

            window.SourceInitialized += ApplyWhenReady;
        }
    }

    private static void OnBackdropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window)
        {
            return;
        }

        var backdropType = (WindowBackdropType)e.NewValue;

        // Set window background based on backdrop type
        // For backdrop effects (Acrylic, Mica, Tabbed), use Transparent to let the effect show through
        // For Default, use White for solid background
        window.Background = backdropType == WindowBackdropType.Default
            ? System.Windows.Media.Brushes.White
            : System.Windows.Media.Brushes.Transparent;

        // Use SourceInitialized to ensure window handle is ready
        if (PresentationSource.FromVisual(window) != null)
        {
            ApplyBackdropAndRefreshTheme(window, backdropType);
        }
        else
        {
            window.SourceInitialized += (s, args) =>
            {
                ApplyBackdropAndRefreshTheme(window, backdropType);
            };
        }
    }

    private static void ApplyBackdropAndRefreshTheme(Window window, WindowBackdropType backdropType)
    {
        ApplyBackdrop(window, backdropType);

        // Keep visual consistency: backdrop changes may overwrite background,
        // so re-apply current window theme immediately.
        WindowThemeHelper.ApplyTheme(window, WindowThemeHelper.GetCurrentTheme(window));
    }

    private static void ApplyBackdrop(Window window, WindowBackdropType backdropType)
    {
        var helper = new WindowInteropHelper(window);
        IntPtr hwnd = helper.Handle;

        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        // Map enum to DWM backdrop type values
        // 0 = Auto/Default
        // 1 = None (white background)
        // 2 = Mica
        // 3 = Acrylic
        // 4 = Tabbed
        int dwmBackdropType = backdropType switch
        {
            WindowBackdropType.Acrylic => 3,
            WindowBackdropType.Mica => 2,
            WindowBackdropType.Tabbed => 4,
            _ => 1 // Default to None (white background)
        };

        try
        {
            _ = DwmSetWindowAttribute(hwnd, DwmwaSystemBackdropType, ref dwmBackdropType, sizeof(int));
        }
        catch
        {
            // Silently fail on older Windows versions that don't support this API
        }
    }

    private static void ApplyImmersiveDarkMode(Window window, bool enabled)
    {
        var helper = new WindowInteropHelper(window);
        IntPtr hwnd = helper.Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        int value = enabled ? 1 : 0;

        try
        {
            int result = DwmSetWindowAttribute(
                hwnd,
                DwmwaUseImmersiveDarkMode,
                ref value,
                sizeof(int)
            );

            if (result != 0)
            {
                _ = DwmSetWindowAttribute(
                    hwnd,
                    DwmwaUseImmersiveDarkModeBefore20H1,
                    ref value,
                    sizeof(int)
                );
            }
        }
        catch
        {
            // Silently fail on unsupported systems.
        }
    }
}
