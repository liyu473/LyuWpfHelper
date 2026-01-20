using System.Globalization;
using System.Windows.Data;

namespace LyuWpfHelper.Converters;

/// <summary>
/// 数学运算转换器
/// 支持加减乘除运算，参数格式："+10"、"-5"、"*2"、"/3"
/// </summary>
[ValueConversion(typeof(double), typeof(double))]
public class MathConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return value;

        if (!double.TryParse(value.ToString(), out double numValue))
            return value;

        string operation = parameter.ToString()!;
        if (string.IsNullOrWhiteSpace(operation))
            return value;

        char op = operation[0];
        string numStr = operation[1..];

        if (!double.TryParse(numStr, out double operand))
            return value;

        return op switch
        {
            '+' => numValue + operand,
            '-' => numValue - operand,
            '*' => numValue * operand,
            '/' => operand != 0 ? numValue / operand : value,
            _ => value
        };
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return value;

        if (!double.TryParse(value.ToString(), out double numValue))
            return value;

        string operation = parameter.ToString()!;
        if (string.IsNullOrWhiteSpace(operation))
            return value;

        char op = operation[0];
        string numStr = operation[1..];

        if (!double.TryParse(numStr, out double operand))
            return value;

        return op switch
        {
            '+' => numValue - operand,
            '-' => numValue + operand,
            '*' => operand != 0 ? numValue / operand : value,
            '/' => numValue * operand,
            _ => value
        };
    }
}
