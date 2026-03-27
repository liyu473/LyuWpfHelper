using LyuWpfHelper.Helpers;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace LyuWpfHelper.Controls;

[StyleTypedProperty(Property = nameof(ItemContainerStyle), StyleTargetType = typeof(Drawer))]
[TemplatePart(Name = PartOverlayLayer, Type = typeof(FrameworkElement))]
public class DrawersHost : ItemsControl
{
    private static readonly TimeSpan DefaultOverlayAnimationDuration = TimeSpan.FromMilliseconds(500);
    private const string PartOverlayLayer = "PART_OverlayLayer";
    private const string LightOverlayBrushResourceKey = "LyuDrawer.Light.OverlayBrush";
    private const string DarkOverlayBrushResourceKey = "LyuDrawer.Dark.OverlayBrush";
    private static readonly Brush DefaultOverlayBrush = new SolidColorBrush(Color.FromArgb(0x8C, 0, 0, 0));
    private static readonly ControlTemplate FallbackTemplate = CreateFallbackTemplate();

    private static readonly DependencyPropertyDescriptor? ThemeDescriptor =
        DependencyPropertyDescriptor.FromProperty(WindowThemeHelper.CurrentThemeProperty, typeof(Window));

    private FrameworkElement? _overlayLayer;
    private Drawer? _lastOverlayDrawer;
    private Window? _ownerWindow;
    private LyuWindow? _ownerLyuWindow;
    private WindowThemeMode _actualTheme = WindowThemeMode.Light;
    private int _overlayAnimationVersion;
    private bool _isApplyingFallbackTemplate;
    private readonly MouseButtonEventHandler _hostPreviewMouseDownHandler;
    private readonly MouseButtonEventHandler _ownerWindowPreviewMouseDownHandler;

    static DrawersHost()
    {
        if (DefaultOverlayBrush.CanFreeze)
        {
            DefaultOverlayBrush.Freeze();
        }

        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(DrawersHost),
            new FrameworkPropertyMetadata(typeof(DrawersHost))
        );
    }

    public DrawersHost()
    {
        if (ReadLocalValue(HorizontalAlignmentProperty) == DependencyProperty.UnsetValue)
        {
            SetCurrentValue(HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        }

        if (ReadLocalValue(VerticalAlignmentProperty) == DependencyProperty.UnsetValue)
        {
            SetCurrentValue(VerticalAlignmentProperty, VerticalAlignment.Stretch);
        }

        if (ReadLocalValue(Panel.ZIndexProperty) == DependencyProperty.UnsetValue)
        {
            SetCurrentValue(Panel.ZIndexProperty, int.MaxValue);
        }

        _hostPreviewMouseDownHandler = OnPreviewMouseDown;
        _ownerWindowPreviewMouseDownHandler = OwnerWindowPreviewMouseDown;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        AddHandler(Drawer.IsOpenChangedEvent, new RoutedEventHandler(OnDrawerIsOpenChanged));
        AddHandler(UIElement.PreviewMouseDownEvent, _hostPreviewMouseDownHandler, true);
        PreviewKeyDown += OnPreviewKeyDown;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _overlayLayer = GetTemplateChild(PartOverlayLayer) as FrameworkElement;
        if (_overlayLayer is null && !_isApplyingFallbackTemplate)
        {
            _isApplyingFallbackTemplate = true;
            Template = FallbackTemplate;
            ApplyTemplate();
            _isApplyingFallbackTemplate = false;
            return;
        }

        UpdateOverlayState();
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new Drawer();
    }

    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        return item is Drawer;
    }

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        UpdateOverlayState();
    }

    protected override Size MeasureOverride(Size constraint)
    {
        Size measured = base.MeasureOverride(constraint);

        // Some containers (e.g. TabControl content presenters) can align content to Top/Left.
        // Returning the available finite size keeps the host stretched so overlay/click area is correct.
        double width = double.IsInfinity(constraint.Width)
            ? measured.Width
            : Math.Max(measured.Width, constraint.Width);
        double height = double.IsInfinity(constraint.Height)
            ? measured.Height
            : Math.Max(measured.Height, constraint.Height);

        return new Size(width, height);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachOwnerWindow();
        UpdateActualTheme();
        UpdateOverlayState();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachOwnerWindow();
    }

    private void AttachOwnerWindow()
    {
        Window? owner = Window.GetWindow(this);
        if (ReferenceEquals(owner, _ownerWindow))
        {
            return;
        }

        DetachOwnerWindow();

        _ownerWindow = owner;
        _ownerLyuWindow = owner as LyuWindow;
        if (_ownerLyuWindow is not null)
        {
            _ownerLyuWindow.ThemeChanged += OwnerWindowThemeChanged;
        }

        if (_ownerWindow is not null)
        {
            ThemeDescriptor?.AddValueChanged(_ownerWindow, OwnerWindowAttachedThemeChanged);
            _ownerWindow.Closed += OwnerWindowClosed;
            _ownerWindow.AddHandler(
                UIElement.PreviewMouseDownEvent,
                _ownerWindowPreviewMouseDownHandler,
                true
            );
        }
    }

    private void DetachOwnerWindow()
    {
        if (_ownerLyuWindow is not null)
        {
            _ownerLyuWindow.ThemeChanged -= OwnerWindowThemeChanged;
            _ownerLyuWindow = null;
        }

        if (_ownerWindow is not null)
        {
            ThemeDescriptor?.RemoveValueChanged(_ownerWindow, OwnerWindowAttachedThemeChanged);
            _ownerWindow.Closed -= OwnerWindowClosed;
            _ownerWindow.RemoveHandler(
                UIElement.PreviewMouseDownEvent,
                _ownerWindowPreviewMouseDownHandler
            );
            _ownerWindow = null;
        }
    }

    private void OwnerWindowThemeChanged(object? sender, LyuWindowThemeChangedEventArgs e)
    {
        UpdateActualTheme();
        UpdateOverlayVisualState();
    }

    private void OwnerWindowAttachedThemeChanged(object? sender, EventArgs e)
    {
        UpdateActualTheme();
        UpdateOverlayVisualState();
    }

    private void OwnerWindowClosed(object? sender, EventArgs e)
    {
        DetachOwnerWindow();
    }

    private void OwnerWindowPreviewMouseDown(object? sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && IsDescendantOf(source, this))
        {
            return;
        }

        if (TryCloseByExternalClick())
        {
            e.Handled = true;
        }
    }

    private void UpdateActualTheme()
    {
        WindowThemeMode resolved = WindowThemeMode.Light;

        if (_ownerLyuWindow is not null)
        {
            resolved =
                _ownerLyuWindow.EffectiveTheme == WindowThemeMode.FollowSystem
                    ? WindowThemeHelper.GetEffectiveTheme(_ownerLyuWindow.EffectiveTheme)
                    : _ownerLyuWindow.EffectiveTheme;
        }
        else if (_ownerWindow is not null)
        {
            WindowThemeMode currentTheme = WindowThemeHelper.GetCurrentTheme(_ownerWindow);
            resolved = WindowThemeHelper.GetEffectiveTheme(currentTheme);
        }

        if (resolved == WindowThemeMode.FollowSystem)
        {
            resolved = WindowThemeMode.Light;
        }

        _actualTheme = resolved;
    }

    private void OnDrawerIsOpenChanged(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Drawer changedDrawer)
        {
            return;
        }

        NotifyDrawerStateChanged(changedDrawer);
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        Drawer? drawerToClose = GetOpenDrawers()
            .Where(d => d.CloseOnEscape)
            .OrderByDescending(Panel.GetZIndex)
            .FirstOrDefault();

        if (drawerToClose is null)
        {
            return;
        }

        drawerToClose.SetCurrentValue(Drawer.IsOpenProperty, false);
        e.Handled = true;
    }

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        List<Drawer> openDrawers = GetOpenDrawers()
            .OrderByDescending(Panel.GetZIndex)
            .ToList();

        if (openDrawers.Count == 0)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject source && openDrawers.Any(d => IsDescendantOf(source, d)))
        {
            return;
        }

        if (TryCloseByExternalClick())
        {
            e.Handled = true;
        }
    }

    private void UpdateOverlayState()
    {
        List<Drawer> overlayDrawers = GetOpenDrawers()
            .Where(IsOverlayParticipant)
            .OrderByDescending(Panel.GetZIndex)
            .ToList();

        bool hasVisibleOverlay = overlayDrawers.Count > 0;
        Drawer? activeOverlayDrawer = overlayDrawers.FirstOrDefault();
        if (activeOverlayDrawer is not null)
        {
            _lastOverlayDrawer = activeOverlayDrawer;
        }

        UpdateOverlayVisualState(hasVisibleOverlay, activeOverlayDrawer ?? _lastOverlayDrawer);
    }

    private void UpdateOverlayVisualState(bool? hasVisibleOverlay = null, Drawer? activeOverlayDrawer = null)
    {
        if (_overlayLayer is null)
        {
            return;
        }

        activeOverlayDrawer ??= GetOpenDrawers()
            .Where(IsOverlayParticipant)
            .OrderByDescending(Panel.GetZIndex)
            .FirstOrDefault()
            ?? _lastOverlayDrawer;

        bool visible = hasVisibleOverlay ?? GetOpenDrawers().Any(IsOverlayParticipant);
        Brush overlayBrush = ResolveOverlayBrush(activeOverlayDrawer);
        switch (_overlayLayer)
        {
            case Shape shape:
                shape.Fill = overlayBrush;
                break;
            case Border border:
                border.Background = overlayBrush;
                break;
            case Control control:
                control.Background = overlayBrush;
                break;
            case Panel panel:
                panel.Background = overlayBrush;
                break;
        }

        double targetOpacity = visible ? ResolveOverlayOpacity(activeOverlayDrawer) : 0d;
        bool animate = activeOverlayDrawer?.AreAnimationsEnabled ?? true;
        TimeSpan animationDuration = ResolveOverlayAnimationDuration(activeOverlayDrawer);
        AnimateOverlay(visible, targetOpacity, animate, animationDuration);
    }

    private void AnimateOverlay(
        bool visible,
        double targetOpacity,
        bool useTransitions,
        TimeSpan duration
    )
    {
        if (_overlayLayer is null)
        {
            return;
        }

        _overlayAnimationVersion++;
        int animationVersion = _overlayAnimationVersion;

        bool wasVisible = _overlayLayer.Visibility == Visibility.Visible;
        double startOpacity = wasVisible ? _overlayLayer.Opacity : 0d;
        if (double.IsNaN(startOpacity) || double.IsInfinity(startOpacity))
        {
            startOpacity = 0d;
        }

        if (!useTransitions || duration <= TimeSpan.Zero || startOpacity.Equals(targetOpacity))
        {
            _overlayLayer.BeginAnimation(UIElement.OpacityProperty, null);
            _overlayLayer.Opacity = targetOpacity;
            _overlayLayer.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            _overlayLayer.IsHitTestVisible = visible;
            if (!visible)
            {
                _lastOverlayDrawer = null;
            }

            return;
        }

        if (visible)
        {
            _overlayLayer.Visibility = Visibility.Visible;
            _overlayLayer.IsHitTestVisible = true;
        }
        else
        {
            _overlayLayer.IsHitTestVisible = false;
            if (!wasVisible)
            {
                _overlayLayer.BeginAnimation(UIElement.OpacityProperty, null);
                _overlayLayer.Opacity = 0d;
                _overlayLayer.Visibility = Visibility.Hidden;
                _lastOverlayDrawer = null;
                return;
            }
        }

        DoubleAnimation animation = new()
        {
            From = startOpacity,
            To = targetOpacity,
            Duration = new Duration(duration),
            FillBehavior = FillBehavior.Stop,
            EasingFunction = new CubicEase
            {
                EasingMode = visible ? EasingMode.EaseOut : EasingMode.EaseInOut,
            },
        };

        animation.Completed += (_, _) =>
        {
            if (_overlayLayer is null || animationVersion != _overlayAnimationVersion)
            {
                return;
            }

            _overlayLayer.BeginAnimation(UIElement.OpacityProperty, null);
            _overlayLayer.Opacity = targetOpacity;

            if (!visible)
            {
                if (GetOpenDrawers().Any(IsOverlayParticipant))
                {
                    UpdateOverlayState();
                    return;
                }

                _overlayLayer.Visibility = Visibility.Hidden;
                _overlayLayer.IsHitTestVisible = false;
                _lastOverlayDrawer = null;
                return;
            }

            _overlayLayer.Visibility = Visibility.Visible;
            _overlayLayer.IsHitTestVisible = true;
        };

        _overlayLayer.BeginAnimation(
            UIElement.OpacityProperty,
            animation,
            HandoffBehavior.SnapshotAndReplace
        );
    }

    private Brush ResolveOverlayBrush(Drawer? drawer)
    {
        if (drawer?.OverlayBrush is Brush drawerBrush)
        {
            return drawerBrush;
        }

        string resourceKey = _actualTheme == WindowThemeMode.Dark
            ? DarkOverlayBrushResourceKey
            : LightOverlayBrushResourceKey;

        return TryFindResource(resourceKey) as Brush ?? DefaultOverlayBrush;
    }

    private static double ResolveOverlayOpacity(Drawer? drawer)
    {
        double overlayOpacity = drawer?.OverlayOpacity ?? 1d;
        if (double.IsNaN(overlayOpacity))
        {
            return 1d;
        }

        if (overlayOpacity < 0d)
        {
            return 0d;
        }

        if (overlayOpacity > 1d)
        {
            return 1d;
        }

        return overlayOpacity;
    }

    private static TimeSpan ResolveOverlayAnimationDuration(Drawer? drawer)
    {
        Duration duration = drawer?.AnimationDuration ?? new Duration(DefaultOverlayAnimationDuration);
        if (!duration.HasTimeSpan)
        {
            return DefaultOverlayAnimationDuration;
        }

        TimeSpan timeSpan = duration.TimeSpan;
        return timeSpan < TimeSpan.Zero ? TimeSpan.Zero : timeSpan;
    }

    private static bool IsOverlayParticipant(Drawer drawer)
    {
        return drawer.IsModal;
    }

    private IEnumerable<Drawer> GetDrawers()
    {
        foreach (object item in Items)
        {
            Drawer? drawer = item as Drawer ?? ItemContainerGenerator.ContainerFromItem(item) as Drawer;
            if (drawer is not null)
            {
                yield return drawer;
            }
        }
    }

    private IEnumerable<Drawer> GetOpenDrawers()
    {
        return GetDrawers().Where(d => d.IsOpen);
    }

    private bool TryCloseByExternalClick()
    {
        Drawer? topMostClosableDrawer = GetOpenDrawers()
            .Where(d => d.CloseOnOverlayClick)
            .OrderByDescending(Panel.GetZIndex)
            .FirstOrDefault();

        if (topMostClosableDrawer is null)
        {
            return false;
        }

        topMostClosableDrawer.SetCurrentValue(Drawer.IsOpenProperty, false);
        return true;
    }

    private static bool IsDescendantOf(DependencyObject source, DependencyObject ancestor)
    {
        DependencyObject? current = source;
        while (current is not null)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }

            current = GetParent(current);
        }

        return false;
    }

    private static DependencyObject? GetParent(DependencyObject current)
    {
        return current switch
        {
            Visual => VisualTreeHelper.GetParent(current),
            Visual3D => VisualTreeHelper.GetParent(current),
            FrameworkContentElement fce => fce.Parent,
            _ => LogicalTreeHelper.GetParent(current) as DependencyObject,
        };
    }

    private static ControlTemplate CreateFallbackTemplate()
    {
        FrameworkElementFactory root = new(typeof(Grid));
        root.SetValue(UIElement.ClipToBoundsProperty, true);

        FrameworkElementFactory overlayLayer = new(typeof(Rectangle));
        overlayLayer.SetValue(FrameworkElement.NameProperty, PartOverlayLayer);
        overlayLayer.SetValue(UIElement.IsHitTestVisibleProperty, false);
        overlayLayer.SetValue(UIElement.VisibilityProperty, Visibility.Hidden);

        FrameworkElementFactory itemsPresenter = new(typeof(ItemsPresenter));

        root.AppendChild(overlayLayer);
        root.AppendChild(itemsPresenter);

        return new ControlTemplate(typeof(DrawersHost))
        {
            VisualTree = root,
        };
    }

    private void ReorderZIndices(Drawer lastChanged)
    {
        List<Drawer> openedOthers = GetOpenDrawers()
            .Where(d => !ReferenceEquals(d, lastChanged))
            .OrderBy(Panel.GetZIndex)
            .ToList();

        int index = 0;
        foreach (Drawer drawer in openedOthers)
        {
            Panel.SetZIndex(drawer, index++);
        }

        if (lastChanged.IsOpen)
        {
            Panel.SetZIndex(lastChanged, index);
        }
    }

    internal void NotifyDrawerStateChanged(Drawer changedDrawer)
    {
        ReorderZIndices(changedDrawer);
        UpdateOverlayState();
    }
}
