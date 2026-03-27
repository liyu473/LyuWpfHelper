using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using LyuWpfHelper.Controls;

namespace LyuWpfHelper.Adorners;

internal class BusyAdorner : Adorner
{
    private readonly Grid _rootGrid;
    private readonly VisualCollection _visualChildren;

    public BusyAdorner(UIElement adornedElement) : base(adornedElement)
    {
        _visualChildren = new VisualCollection(this);

        BusyMask = new BusyMaskControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsHitTestVisible = true
        };

        _rootGrid = new Grid
        {
            Background = Brushes.Transparent,
            IsHitTestVisible = true
        };

        _rootGrid.Children.Add(BusyMask);
        _visualChildren.Add(_rootGrid);
    }

    public BusyMaskControl BusyMask { get; }

    public void SetInputBlocking(bool blockInput)
    {
        _rootGrid.IsHitTestVisible = blockInput;
        _rootGrid.Background = blockInput ? Brushes.Transparent : null;
        BusyMask.IsHitTestVisible = blockInput;
    }

    protected override int VisualChildrenCount => _visualChildren.Count;

    protected override Visual GetVisualChild(int index)
    {
        if (index < 0 || index >= _visualChildren.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _visualChildren[index];
    }

    protected override Size MeasureOverride(Size constraint)
    {
        _rootGrid.Measure(constraint);
        return constraint;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _rootGrid.Arrange(new Rect(finalSize));
        return finalSize;
    }
}
