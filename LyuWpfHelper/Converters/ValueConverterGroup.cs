using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace LyuWpfHelper.Converters;

/// <summary>
/// 转换器组合，允许将多个转换器串联使用
/// </summary>
[ContentProperty(nameof(Converters))]
public class ValueConverterGroup : IValueConverter
{
    public Collection<IValueConverter> Converters { get; } = [];

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        foreach (var converter in Converters)
        {
            value = converter.Convert(value, targetType, parameter, culture);
        }
        return value;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        for (int i = Converters.Count - 1; i >= 0; i--)
        {
            value = Converters[i].ConvertBack(value, targetType, parameter, culture);
        }
        return value;
    }
}
