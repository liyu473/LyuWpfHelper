using System.Windows;
using System.Windows.Controls;

namespace LyuWpfHelper.Panels;

public class SimpleWrapPanel : Panel
{
    public static readonly DependencyProperty SpacingProperty =
        DependencyProperty.Register(
            nameof(Spacing),
            typeof(double),
            typeof(SimpleWrapPanel),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(SimpleWrapPanel),
            new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return Orientation == Orientation.Horizontal
            ? MeasureHorizontal(availableSize)
            : MeasureVertical(availableSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return Orientation == Orientation.Horizontal
            ? ArrangeHorizontal(finalSize)
            : ArrangeVertical(finalSize);
    }

    private Size MeasureHorizontal(Size availableSize)
    {
        double lineWidth = 0;
        double lineHeight = 0;
        double desiredWidth = 0;
        double desiredHeight = 0;
        double maxLineWidth = availableSize.Width;
        bool hasLineChild = false;

        foreach (UIElement child in InternalChildren)
        {
            if (child is null) continue;

            child.Measure(availableSize);

            Size childSize = child.DesiredSize;
            double nextLineWidth = hasLineChild ? lineWidth + Spacing + childSize.Width : childSize.Width;

            // 可用宽度有限且当前行已有元素时，超出范围就转到下一行。
            if (hasLineChild && !double.IsInfinity(maxLineWidth) && nextLineWidth > maxLineWidth)
            {
                desiredWidth = Math.Max(desiredWidth, lineWidth);
                desiredHeight += lineHeight + (desiredHeight > 0 ? Spacing : 0);

                lineWidth = childSize.Width;
                lineHeight = childSize.Height;
            }
            else
            {
                lineWidth = nextLineWidth;
                lineHeight = Math.Max(lineHeight, childSize.Height);
            }

            hasLineChild = true;
        }

        if (hasLineChild)
        {
            desiredWidth = Math.Max(desiredWidth, lineWidth);
            desiredHeight += lineHeight + (desiredHeight > 0 ? Spacing : 0);
        }

        return new Size(desiredWidth, desiredHeight);
    }

    private Size MeasureVertical(Size availableSize)
    {
        double columnWidth = 0;
        double columnHeight = 0;
        double desiredWidth = 0;
        double desiredHeight = 0;
        double maxColumnHeight = availableSize.Height;
        bool hasColumnChild = false;

        foreach (UIElement child in InternalChildren)
        {
            if (child is null) continue;

            child.Measure(availableSize);

            Size childSize = child.DesiredSize;
            double nextColumnHeight = hasColumnChild ? columnHeight + Spacing + childSize.Height : childSize.Height;

            // 可用高度有限且当前列已有元素时，超出范围就转到下一列。
            if (hasColumnChild && !double.IsInfinity(maxColumnHeight) && nextColumnHeight > maxColumnHeight)
            {
                desiredWidth += columnWidth + (desiredWidth > 0 ? Spacing : 0);
                desiredHeight = Math.Max(desiredHeight, columnHeight);

                columnWidth = childSize.Width;
                columnHeight = childSize.Height;
            }
            else
            {
                columnWidth = Math.Max(columnWidth, childSize.Width);
                columnHeight = nextColumnHeight;
            }

            hasColumnChild = true;
        }

        if (hasColumnChild)
        {
            desiredWidth += columnWidth + (desiredWidth > 0 ? Spacing : 0);
            desiredHeight = Math.Max(desiredHeight, columnHeight);
        }

        return new Size(desiredWidth, desiredHeight);
    }

    private Size ArrangeHorizontal(Size finalSize)
    {
        double x = 0;
        double y = 0;
        double lineHeight = 0;
        double maxLineWidth = finalSize.Width;
        int lineStartIndex = 0;

        for (int i = 0; i < InternalChildren.Count; i++)
        {
            UIElement child = InternalChildren[i];
            if (child is null) continue;

            Size childSize = child.DesiredSize;
            double nextX = i > lineStartIndex ? x + Spacing + childSize.Width : childSize.Width;

            if (i > lineStartIndex && !double.IsInfinity(maxLineWidth) && nextX > maxLineWidth)
            {
                ArrangeHorizontalLine(lineStartIndex, i, y, lineHeight);

                y += lineHeight + Spacing;
                x = childSize.Width;
                lineHeight = childSize.Height;
                lineStartIndex = i;
            }
            else
            {
                x = nextX;
                lineHeight = Math.Max(lineHeight, childSize.Height);
            }
        }

        ArrangeHorizontalLine(lineStartIndex, InternalChildren.Count, y, lineHeight);
        return finalSize;
    }

    private void ArrangeHorizontalLine(int startIndex, int endIndex, double y, double lineHeight)
    {
        double x = 0;

        for (int i = startIndex; i < endIndex; i++)
        {
            UIElement child = InternalChildren[i];
            if (child is null) continue;

            child.Arrange(new Rect(x, y, child.DesiredSize.Width, lineHeight));
            x += child.DesiredSize.Width + Spacing;
        }
    }

    private Size ArrangeVertical(Size finalSize)
    {
        double x = 0;
        double y = 0;
        double columnWidth = 0;
        double maxColumnHeight = finalSize.Height;
        int columnStartIndex = 0;

        for (int i = 0; i < InternalChildren.Count; i++)
        {
            UIElement child = InternalChildren[i];
            if (child is null) continue;

            Size childSize = child.DesiredSize;
            double nextY = i > columnStartIndex ? y + Spacing + childSize.Height : childSize.Height;

            if (i > columnStartIndex && !double.IsInfinity(maxColumnHeight) && nextY > maxColumnHeight)
            {
                ArrangeVerticalColumn(columnStartIndex, i, x, columnWidth);

                x += columnWidth + Spacing;
                y = childSize.Height;
                columnWidth = childSize.Width;
                columnStartIndex = i;
            }
            else
            {
                y = nextY;
                columnWidth = Math.Max(columnWidth, childSize.Width);
            }
        }

        ArrangeVerticalColumn(columnStartIndex, InternalChildren.Count, x, columnWidth);
        return finalSize;
    }

    private void ArrangeVerticalColumn(int startIndex, int endIndex, double x, double columnWidth)
    {
        double y = 0;

        for (int i = startIndex; i < endIndex; i++)
        {
            UIElement child = InternalChildren[i];
            if (child is null) continue;

            child.Arrange(new Rect(x, y, columnWidth, child.DesiredSize.Height));
            y += child.DesiredSize.Height + Spacing;
        }
    }
}
