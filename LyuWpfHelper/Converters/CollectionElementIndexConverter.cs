using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace LyuWpfHelper.Converters;

/// <summary>
/// 集合元素索引转换器
/// 用于获取元素在集合中的索引位置（从1开始）
/// 使用方式：MultiBinding 绑定元素本身和集合
/// </summary>
public class CollectionElementIndexConverter : IMultiValueConverter
{
    /// <summary>
    /// 是否从0开始计数（默认从1开始）
    /// </summary>
    public bool ZeroBased { get; set; }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return Binding.DoNothing;

        var item = values[0];
        if (item == null)
            return Binding.DoNothing;

        if (values[1] is not IList items)
            return Binding.DoNothing;

        int index = items.IndexOf(item);
        
        if (index < 0)
            return Binding.DoNothing;

        // 根据 ZeroBased 属性决定是否加1
        return ZeroBased ? index : index + 1;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("CollectionElementIndexConverter does not support ConvertBack.");
    }
}
