using System;
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
    /// Default system backdrop
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

    private static void OnBackdropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window)
        {
            return;
        }

        var backdropType = (WindowBackdropType)e.NewValue;

        // Use SourceInitialized to ensure window handle is ready
        if (PresentationSource.FromVisual(window) != null)
        {
            ApplyBackdrop(window, backdropType);
        }
        else
        {
            window.SourceInitialized += (s, args) =>
            {
                ApplyBackdrop(window, backdropType);
            };
        }
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
        // 1 = None
        // 2 = Mica
        // 3 = Acrylic
        // 4 = Tabbed
        int dwmBackdropType = backdropType switch
        {
            WindowBackdropType.Acrylic => 3,
            WindowBackdropType.Mica => 2,
            WindowBackdropType.Tabbed => 4,
            _ => 0 // Default
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
}
