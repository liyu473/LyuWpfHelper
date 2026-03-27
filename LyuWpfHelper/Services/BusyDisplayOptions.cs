using System.Windows;

namespace LyuWpfHelper.Services;

public class BusyDisplayOptions
{
    public string? Title { get; set; }

    public string? Message { get; set; }

    public object? Content { get; set; }

    internal DataTemplate? ContentTemplate { get; set; }

    /// <summary>
    /// 是否阻止蒙版下方的交互，默认 true。
    /// </summary>
    public bool BlockInput { get; set; } = true;
}
