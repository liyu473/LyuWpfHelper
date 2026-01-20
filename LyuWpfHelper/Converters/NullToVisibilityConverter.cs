using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LyuWpfHelper.Converters;

/// <summary>
/// 空值到可见性转换器
/// Null -> Collapsed, NotNull -> Visible
/// </summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public class NullToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// 是否反转转换逻辑（Null -> Visible, NotNull -> Collapsed）
    /// </summary>
    public bool IsInverted { get; set; }

    /// <summary>
    /// 隐藏时使用 Hidden 而不是 Collapsed
    /// </summary>
    public bool UseHidden { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null;

        if (IsInverted)
            isNull = !isNull;

        if (!isNull)
            return Visibility.Visible;

        return UseHidden ? Visibility.Hidden : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
