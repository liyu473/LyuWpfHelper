using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using LyuWpfHelper.Helpers;

namespace LyuWpfHelper.Controls;

[StyleTypedProperty(Property = nameof(ItemContainerStyle), StyleTargetType = typeof(Drawer))]
[TemplatePart(Name = PartOverlayLayer, Type = typeof(FrameworkElement))]
public class DrawersHost : ItemsControl
{
    private const string PartOverlayLayer = "PART_OverlayLayer";
    private static readonly Brush DefaultOverlayBrush = new SolidColorBrush(Color.FromArgb(0x8C, 0, 0, 0));
    private static readonly ControlTemplate FallbackTemplate = CreateFallbackTemplate();

    private static readonly DependencyPropertyDescriptor? ThemeDescriptor =
        DependencyPropertyDescriptor.FromProperty(WindowThemeHelper.CurrentThemeProperty, typeof(Window));

    private FrameworkElement? _overlayLayer;
    private Window? _ownerWindow;
    private LyuWindow? _ownerLyuWindow;
    private bool _isApplyingFallbackTemplate;
    private readonly MouseButtonEventHandler _hostPreviewMouseDownHandler;
    private readonly MouseButtonEventHandler _ownerWindowPreviewMouseDownHandler;

    public static readonly DependencyProperty OverlayBrushProperty = DependencyProperty.Register(
        nameof(OverlayBrush),
        typeof(Brush),
        typeof(DrawersHost),
        new PropertyMetadata(null, OnOverlayVisualPropertyChanged)
    );

    public static readonly DependencyProperty OverlayOpacityProperty = DependencyProperty.Register(
        nameof(OverlayOpacity),
        typeof(double),
        typeof(DrawersHost),
        new PropertyMetadata(1d, OnOverlayVisualPropertyChanged)
    );

    public static readonly DependencyProperty OverlayModeProperty = DependencyProperty.Register(
        nameof(OverlayMode),
        typeof(DrawerOverlayMode),
        typeof(DrawersHost),
        new PropertyMetadata(DrawerOverlayMode.ModalOnly, OnOverlayBehaviorPropertyChanged)
    );

    public static readonly DependencyProperty OverlayCloseBehaviorProperty =
        DependencyProperty.Register(
            nameof(OverlayCloseBehavior),
            typeof(DrawerOverlayCloseBehavior),
            typeof(DrawersHost),
            new PropertyMetadata(
                DrawerOverlayCloseBehavior.CloseTopMost,
                OnOverlayBehaviorPropertyChanged
            )
        );

    public static readonly DependencyProperty CloseOnOverlayClickProperty =
        DependencyProperty.Register(
            nameof(CloseOnOverlayClick),
            typeof(bool),
            typeof(DrawersHost),
            new PropertyMetadata(true)
        );

    public static readonly DependencyProperty CloseOnEscapeProperty = DependencyProperty.Register(
        nameof(CloseOnEscape),
        typeof(bool),
        typeof(DrawersHost),
        new PropertyMetadata(true)
    );

    private static readonly DependencyPropertyKey HasVisibleOverlayPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HasVisibleOverlay),
            typeof(bool),
            typeof(DrawersHost),
            new PropertyMetadata(false)
        );

    public static readonly DependencyProperty HasVisibleOverlayProperty =
        HasVisibleOverlayPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey ActualThemePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(ActualTheme),
            typeof(WindowThemeMode),
            typeof(DrawersHost),
            new PropertyMetadata(WindowThemeMode.Light)
        );

    public static readonly DependencyProperty ActualThemeProperty = ActualThemePropertyKey.DependencyProperty;

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

    public Brush? OverlayBrush
    {
        get => (Brush?)GetValue(OverlayBrushProperty);
        set => SetValue(OverlayBrushProperty, value);
    }

    public double OverlayOpacity
    {
        get => (double)GetValue(OverlayOpacityProperty);
        set => SetValue(OverlayOpacityProperty, value);
    }

    public DrawerOverlayMode OverlayMode
    {
        get => (DrawerOverlayMode)GetValue(OverlayModeProperty);
        set => SetValue(OverlayModeProperty, value);
    }

    public DrawerOverlayCloseBehavior OverlayCloseBehavior
    {
        get => (DrawerOverlayCloseBehavior)GetValue(OverlayCloseBehaviorProperty);
        set => SetValue(OverlayCloseBehaviorProperty, value);
    }

    public bool CloseOnOverlayClick
    {
        get => (bool)GetValue(CloseOnOverlayClickProperty);
        set => SetValue(CloseOnOverlayClickProperty, value);
    }

    public bool CloseOnEscape
    {
        get => (bool)GetValue(CloseOnEscapeProperty);
        set => SetValue(CloseOnEscapeProperty, value);
    }

    public bool HasVisibleOverlay => (bool)GetValue(HasVisibleOverlayProperty);

    public WindowThemeMode ActualTheme => (WindowThemeMode)GetValue(ActualThemeProperty);

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

    private static void OnOverlayBehaviorPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is DrawersHost host)
        {
            host.UpdateOverlayState();
        }
    }

    private static void OnOverlayVisualPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is DrawersHost host)
        {
            host.UpdateOverlayVisualState();
        }
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
    }

    private void OwnerWindowAttachedThemeChanged(object? sender, EventArgs e)
    {
        UpdateActualTheme();
    }

    private void OwnerWindowClosed(object? sender, EventArgs e)
    {
        DetachOwnerWindow();
    }

    private void OwnerWindowPreviewMouseDown(object? sender, MouseButtonEventArgs e)
    {
        if (!CloseOnOverlayClick)
        {
            return;
        }

        if (
            e.OriginalSource is DependencyObject source
            && IsDescendantOf(source, this)
        )
        {
            return;
        }

        if (TryCloseByExternalClick(e.ChangedButton))
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

        SetValue(ActualThemePropertyKey, resolved);
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
        if (!CloseOnEscape || e.Key != Key.Escape)
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
        if (!CloseOnOverlayClick)
        {
            return;
        }

        List<Drawer> openDrawers = GetOpenDrawers()
            .OrderByDescending(Panel.GetZIndex)
            .ToList();

        if (openDrawers.Count == 0)
        {
            return;
        }

        if (
            e.OriginalSource is DependencyObject source
            && openDrawers.Any(d => IsDescendantOf(source, d))
        )
        {
            return;
        }

        if (TryCloseByExternalClick(e.ChangedButton))
        {
            e.Handled = true;
        }
    }

    private void UpdateOverlayState()
    {
        bool hasVisibleOverlay = GetOpenDrawers().Any(IsOverlayParticipant);
        SetValue(HasVisibleOverlayPropertyKey, hasVisibleOverlay);
        UpdateOverlayVisualState(hasVisibleOverlay);
    }

    private void UpdateOverlayVisualState(bool? hasVisibleOverlay = null)
    {
        if (_overlayLayer is null)
        {
            return;
        }

        bool visible = hasVisibleOverlay ?? HasVisibleOverlay;
        _overlayLayer.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
        _overlayLayer.IsHitTestVisible = visible;

        double overlayOpacity = OverlayOpacity;
        if (double.IsNaN(overlayOpacity))
        {
            overlayOpacity = 1d;
        }
        else if (overlayOpacity < 0d)
        {
            overlayOpacity = 0d;
        }
        else if (overlayOpacity > 1d)
        {
            overlayOpacity = 1d;
        }

        _overlayLayer.Opacity = overlayOpacity;

        Brush overlayBrush = OverlayBrush ?? DefaultOverlayBrush;
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
    }

    private bool IsOverlayParticipant(Drawer drawer)
    {
        if (!drawer.ShowOverlay)
        {
            return false;
        }

        return OverlayMode switch
        {
            DrawerOverlayMode.ModalOnly => drawer.IsModal,
            _ => true,
        };
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

    private bool TryCloseByExternalClick(MouseButton changedButton)
    {
        List<Drawer> closable = GetOpenDrawers()
            .Where(
                d =>
                    d.CloseOnOverlayClick
                    && !d.IsPinned
                    && d.ExternalCloseButton == changedButton
            )
            .OrderByDescending(Panel.GetZIndex)
            .ToList();

        if (closable.Count == 0)
        {
            return false;
        }

        if (OverlayCloseBehavior == DrawerOverlayCloseBehavior.CloseTopMost)
        {
            closable[0].SetCurrentValue(Drawer.IsOpenProperty, false);
        }
        else
        {
            foreach (Drawer drawer in closable)
            {
                drawer.SetCurrentValue(Drawer.IsOpenProperty, false);
            }
        }

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
