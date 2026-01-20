using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LyuWpfHelper.Converters;

/// <summary>
/// 字符串空值到可见性转换器
/// Empty/Null -> Collapsed, NotEmpty -> Visible
/// </summary>
[ValueConversion(typeof(string), typeof(Visibility))]
public class StringEmptyToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// 是否反转转换逻辑（Empty -> Visible, NotEmpty -> Collapsed）
    /// </summary>
    public bool IsInverted { get; set; }

    /// <summary>
    /// 隐藏时使用 Hidden 而不是 Collapsed
    /// </summary>
    public bool UseHidden { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isEmpty = string.IsNullOrWhiteSpace(value as string);

        if (IsInverted)
            isEmpty = !isEmpty;

        if (!isEmpty)
            return Visibility.Visible;

        return UseHidden ? Visibility.Hidden : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
