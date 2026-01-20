using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LyuWpfHelper.Converters;

/// <summary>
/// 布尔值到可见性转换器
/// True -> Visible, False -> Collapsed
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class BooleanToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// 是否反转转换逻辑（True -> Collapsed, False -> Visible）
    /// </summary>
    public bool IsInverted { get; set; }

    /// <summary>
    /// False 时使用 Hidden 而不是 Collapsed
    /// </summary>
    public bool UseHidden { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
            return Visibility.Collapsed;

        if (IsInverted)
            boolValue = !boolValue;

        if (boolValue)
            return Visibility.Visible;

        return UseHidden ? Visibility.Hidden : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Visibility visibility)
            return false;

        bool result = visibility == Visibility.Visible;
        return IsInverted ? !result : result;
    }
}
