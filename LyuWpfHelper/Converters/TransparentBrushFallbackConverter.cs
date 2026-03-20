using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LyuWpfHelper.Converters;

/// <summary>
/// Returns the first non-transparent brush in the input list.
/// </summary>
public class TransparentBrushFallbackConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        Brush? lastBrush = null;
        foreach (object? value in values)
        {
            if (value is not Brush brush)
            {
                continue;
            }

            lastBrush = brush;
            if (!IsTransparent(brush))
            {
                return brush;
            }
        }

        return lastBrush ?? Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static bool IsTransparent(Brush? brush)
    {
        if (brush is null || brush.Opacity <= 0.01)
        {
            return true;
        }

        if (brush is SolidColorBrush solidBrush)
        {
            return solidBrush.Color.A <= 2;
        }

        return false;
    }
}
