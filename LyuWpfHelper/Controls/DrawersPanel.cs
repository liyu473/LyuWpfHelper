using System.Windows;
using System.Windows.Controls;

namespace LyuWpfHelper.Controls;

public class DrawersPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        Size remaining = availableSize;
        Size desired = new();
        double usedWidth = 0d;
        double usedHeight = 0d;

        foreach (UIElement child in InternalChildren)
        {
            bool inline = child is Drawer { Mode: DrawerMode.Inline };
            child.Measure(inline ? remaining : availableSize);
            if (!inline)
            {
                desired.Width = Math.Max(desired.Width, child.DesiredSize.Width);
                desired.Height = Math.Max(desired.Height, child.DesiredSize.Height);
                continue;
            }

            Drawer drawer = (Drawer)child;
            if (drawer.InlineProgress <= 0d)
            {
                continue;
            }

            desired.Width = Math.Max(desired.Width, usedWidth + child.DesiredSize.Width);
            desired.Height = Math.Max(desired.Height, usedHeight + child.DesiredSize.Height);
            if (drawer.Position is DrawerPosition.Left or DrawerPosition.Right)
            {
                double width = Math.Min(remaining.Width, child.DesiredSize.Width) * drawer.InlineProgress;
                usedWidth += width;
                remaining.Width -= width;
            }
            else
            {
                double height = Math.Min(remaining.Height, child.DesiredSize.Height) * drawer.InlineProgress;
                usedHeight += height;
                remaining.Height -= height;
            }
        }

        return desired;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Rect remaining = new(finalSize);
        foreach (UIElement child in InternalChildren)
        {
            if (child is not Drawer { Mode: DrawerMode.Inline } drawer)
            {
                child.Arrange(new Rect(finalSize));
                continue;
            }

            Rect bounds = remaining;
            double width = Math.Min(remaining.Width, drawer.DesiredSize.Width);
            double height = Math.Min(remaining.Height, drawer.DesiredSize.Height);
            double progress = Math.Clamp(drawer.InlineProgress, 0d, 1d);
            switch (drawer.Position)
            {
                case DrawerPosition.Left:
                    bounds.Width = width;
                    if (progress > 0d)
                    {
                        remaining.X += width * progress;
                        remaining.Width -= width * progress;
                    }
                    break;
                case DrawerPosition.Right:
                    bounds.X = remaining.Right - width;
                    bounds.Width = width;
                    if (progress > 0d)
                    {
                        remaining.Width -= width * progress;
                    }
                    break;
                case DrawerPosition.Top:
                    bounds.Height = height;
                    if (progress > 0d)
                    {
                        remaining.Y += height * progress;
                        remaining.Height -= height * progress;
                    }
                    break;
                case DrawerPosition.Bottom:
                    bounds.Y = remaining.Bottom - height;
                    bounds.Height = height;
                    if (progress > 0d)
                    {
                        remaining.Height -= height * progress;
                    }
                    break;
            }

            child.Arrange(bounds);
        }

        if (ItemsControl.GetItemsOwner(this) is DrawersHost host)
        {
            host.SetInlinePadding(new Thickness(remaining.Left, remaining.Top,
                Math.Max(0d, finalSize.Width - remaining.Right),
                Math.Max(0d, finalSize.Height - remaining.Bottom)));
        }

        return finalSize;
    }
}
