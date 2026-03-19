using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using LyuWpfHelper.Helpers;
using LyuWpfHelper.Panels;

namespace LyuWpfHelper.Adorners;

internal class NotificationAdorner : Adorner
{
    private readonly Grid _rootGrid;
    private readonly SimpleStackPanel _topRightPanel;
    private readonly SimpleStackPanel _bottomRightPanel;
    private readonly SimpleStackPanel _topCenterPanel;
    private readonly SimpleStackPanel _bottomCenterPanel;
    private readonly VisualCollection _visualChildren;

    public NotificationAdorner(UIElement adornedElement) : base(adornedElement)
    {
        _visualChildren = new VisualCollection(this);

        // Root grid - null background allows mouse events to pass through empty areas
        _rootGrid = new Grid
        {
            Background = null
        };

        // Create 4 positioned panels with SimpleStackPanel
        _topRightPanel = CreatePanel(
            VerticalAlignment.Top,
            HorizontalAlignment.Right,
            new Thickness(0, 20, 20, 0));

        _bottomRightPanel = CreatePanel(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Right,
            new Thickness(0, 0, 20, 20));

        _topCenterPanel = CreatePanel(
            VerticalAlignment.Top,
            HorizontalAlignment.Center,
            new Thickness(0, 20, 0, 0));

        _bottomCenterPanel = CreatePanel(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Center,
            new Thickness(0, 0, 0, 20));

        _rootGrid.Children.Add(_topRightPanel);
        _rootGrid.Children.Add(_bottomRightPanel);
        _rootGrid.Children.Add(_topCenterPanel);
        _rootGrid.Children.Add(_bottomCenterPanel);

        _visualChildren.Add(_rootGrid);
    }

    private SimpleStackPanel CreatePanel(
        VerticalAlignment vAlign,
        HorizontalAlignment hAlign,
        Thickness margin)
    {
        return new SimpleStackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 10,
            VerticalAlignment = vAlign,
            HorizontalAlignment = hAlign,
            Margin = margin,
            IsHitTestVisible = true
        };
    }

    public SimpleStackPanel GetPanel(NotificationPosition position)
    {
        return position switch
        {
            NotificationPosition.TopRight => _topRightPanel,
            NotificationPosition.BottomRight => _bottomRightPanel,
            NotificationPosition.TopCenter => _topCenterPanel,
            NotificationPosition.BottomCenter => _bottomCenterPanel,
            _ => _topRightPanel
        };
    }

    public bool IsEmpty()
    {
        return _topRightPanel.Children.Count == 0 &&
               _bottomRightPanel.Children.Count == 0 &&
               _topCenterPanel.Children.Count == 0 &&
               _bottomCenterPanel.Children.Count == 0;
    }

    protected override int VisualChildrenCount => _visualChildren.Count;

    protected override Visual GetVisualChild(int index)
    {
        if (index < 0 || index >= _visualChildren.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
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
