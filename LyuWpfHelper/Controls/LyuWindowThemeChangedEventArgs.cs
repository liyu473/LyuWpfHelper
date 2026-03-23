using LyuWpfHelper.Helpers;

namespace LyuWpfHelper.Controls;

/// <summary>
/// Theme change payload for <see cref="LyuWindow"/>.
/// </summary>
public sealed class LyuWindowThemeChangedEventArgs : EventArgs
{
    public LyuWindowThemeChangedEventArgs(
        WindowThemeMode requestedTheme,
        WindowThemeMode effectiveTheme
    )
    {
        RequestedTheme = requestedTheme;
        EffectiveTheme = effectiveTheme;
    }

    /// <summary>
    /// Theme requested by caller (can be FollowSystem).
    /// </summary>
    public WindowThemeMode RequestedTheme { get; }

    /// <summary>
    /// Actual theme applied to the window (Light/Dark).
    /// </summary>
    public WindowThemeMode EffectiveTheme { get; }
}
