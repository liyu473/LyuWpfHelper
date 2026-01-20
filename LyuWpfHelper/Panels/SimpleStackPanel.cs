using System.Windows;
using System.Windows.Controls;

namespace LyuWpfHelper.Panels;

public class SimpleStackPanel : Panel
{
    public static readonly DependencyProperty SpacingProperty =
        DependencyProperty.Register(
            nameof(Spacing),
            typeof(double),
            typeof(SimpleStackPanel),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(SimpleStackPanel),
            new FrameworkPropertyMetadata(Orientation.Vertical, FrameworkPropertyMetadataOptions.AffectsMeasure));

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0;
        double height = 0;
        int count = 0;

        foreach (UIElement child in InternalChildren)
        {
            if (child is null) continue;

            child.Measure(availableSize);
            count++;

            if (Orientation == Orientation.Vertical)
            {
                height += child.DesiredSize.Height;
                width = Math.Max(width, child.DesiredSize.Width);
            }
            else
            {
                width += child.DesiredSize.Width;
                height = Math.Max(height, child.DesiredSize.Height);
            }
        }

        if (count > 1)
        {
            if (Orientation == Orientation.Vertical)
                height += Spacing * (count - 1);
            else
                width += Spacing * (count - 1);
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double offset = 0;

        foreach (UIElement child in InternalChildren)
        {
            if (child is null) continue;

            if (Orientation == Orientation.Vertical)
            {
                child.Arrange(new Rect(0, offset, finalSize.Width, child.DesiredSize.Height));
                offset += child.DesiredSize.Height + Spacing;
            }
            else
            {
                child.Arrange(new Rect(offset, 0, child.DesiredSize.Width, finalSize.Height));
                offset += child.DesiredSize.Width + Spacing;
            }
        }

        return finalSize;
    }
}
