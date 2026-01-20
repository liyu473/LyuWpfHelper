using System.Windows;
using System.Windows.Controls;

namespace LyuWpfHelper.Helpers;

/// <summary>
/// Grid 辅助类，提供简化的行列定义语法
/// </summary>
public static class GridHelper
{
    #region RowDefinitions 附加属性

    /// <summary>
    /// RowDefinitions 附加属性
    /// 支持的格式：
    /// - Auto: GridLength.Auto
    /// - *: 1*
    /// - 2*: 2*
    /// - 100: 固定值 100
    /// 示例: "Auto,*,2*,100"
    /// </summary>
    public static readonly DependencyProperty RowDefinitionsProperty =
        DependencyProperty.RegisterAttached(
            "RowDefinitions",
            typeof(string),
            typeof(GridHelper),
            new PropertyMetadata(null, OnRowDefinitionsChanged));

    public static string GetRowDefinitions(DependencyObject obj)
    {
        return (string)obj.GetValue(RowDefinitionsProperty);
    }

    public static void SetRowDefinitions(DependencyObject obj, string value)
    {
        obj.SetValue(RowDefinitionsProperty, value);
    }

    private static void OnRowDefinitionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Grid grid)
            return;

        grid.RowDefinitions.Clear();

        if (e.NewValue is not string definitions || string.IsNullOrWhiteSpace(definitions))
            return;

        var rows = definitions.Split(',');
        foreach (var row in rows)
        {
            var trimmedRow = row.Trim();
            if (string.IsNullOrEmpty(trimmedRow))
                continue;

            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = ParseGridLength(trimmedRow)
            });
        }
    }

    #endregion

    #region ColumnDefinitions 附加属性

    /// <summary>
    /// ColumnDefinitions 附加属性
    /// 支持的格式：
    /// - Auto: GridLength.Auto
    /// - *: 1*
    /// - 2*: 2*
    /// - 100: 固定值 100
    /// 示例: "Auto,*,2*,100"
    /// </summary>
    public static readonly DependencyProperty ColumnDefinitionsProperty =
        DependencyProperty.RegisterAttached(
            "ColumnDefinitions",
            typeof(string),
            typeof(GridHelper),
            new PropertyMetadata(null, OnColumnDefinitionsChanged));

    public static string GetColumnDefinitions(DependencyObject obj)
    {
        return (string)obj.GetValue(ColumnDefinitionsProperty);
    }

    public static void SetColumnDefinitions(DependencyObject obj, string value)
    {
        obj.SetValue(ColumnDefinitionsProperty, value);
    }

    private static void OnColumnDefinitionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Grid grid)
            return;

        grid.ColumnDefinitions.Clear();

        if (e.NewValue is not string definitions || string.IsNullOrWhiteSpace(definitions))
            return;

        var columns = definitions.Split(',');
        foreach (var column in columns)
        {
            var trimmedColumn = column.Trim();
            if (string.IsNullOrEmpty(trimmedColumn))
                continue;

            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = ParseGridLength(trimmedColumn)
            });
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 解析 GridLength 字符串
    /// </summary>
    private static GridLength ParseGridLength(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new GridLength(1, GridUnitType.Star);

        value = value.Trim();

        // Auto
        if (value.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            return GridLength.Auto;

        // Star (*)
        if (value == "*")
            return new GridLength(1, GridUnitType.Star);

        // 带数字的 Star (2*, 3* 等)
        if (value.EndsWith("*"))
        {
            var numberPart = value.Substring(0, value.Length - 1);
            if (double.TryParse(numberPart, out double starValue))
                return new GridLength(starValue, GridUnitType.Star);
        }

        // 固定值 (100, 200 等)
        if (double.TryParse(value, out double pixelValue))
            return new GridLength(pixelValue, GridUnitType.Pixel);

        // 默认返回 Auto
        return GridLength.Auto;
    }

    #endregion
}
