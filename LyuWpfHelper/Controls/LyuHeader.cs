using System.Windows;
using System.Windows.Controls;

namespace LyuWpfHelper.Controls;

/// <summary>
/// 带标题的内容容器，支持标题位于内容上方或左侧。
/// </summary>
public class LyuHeader : GroupBox
{
    private const string PartHeader = "PART_Header";

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(Orientation),
        typeof(LyuHeader),
        new FrameworkPropertyMetadata(Orientation.Vertical)
    );

    /// <summary>
    /// 获取或设置标题与内容的布局方向。纵向时标题在上方，横向时标题在左侧。
    /// </summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    static LyuHeader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(LyuHeader),
            new FrameworkPropertyMetadata(typeof(LyuHeader))
        );
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateHeaderVisibility();
    }

    protected override void OnHeaderChanged(object oldHeader, object newHeader)
    {
        base.OnHeaderChanged(oldHeader, newHeader);
        UpdateHeaderVisibility();
    }

    private void UpdateHeaderVisibility()
    {
        if (GetTemplateChild(PartHeader) is not FrameworkElement header)
        {
            return;
        }

        header.Visibility = Header switch
        {
            string text when string.IsNullOrEmpty(text) => Visibility.Collapsed,
            null => Visibility.Collapsed,
            _ => Visibility.Visible,
        };
    }
}
