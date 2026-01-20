using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LyuWpfHelper.Converters;

/// <summary>
/// 颜色到画刷转换器
/// 将 Color 转换为 SolidColorBrush
/// </summary>
[ValueConversion(typeof(Color), typeof(Brush))]
public class ColorToBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return new SolidColorBrush(color);
        }

        if (value is string colorString)
        {
            try
            {
                var color2 = (Color)ColorConverter.ConvertFromString(colorString);
                return new SolidColorBrush(color2);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            return brush.Color;
        }

        return null;
    }
}
