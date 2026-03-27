using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LyuWpfHelper.Helpers;

namespace LyuWpfHelper.Controls;

[StyleTypedProperty(Property = nameof(ItemContainerStyle), StyleTargetType = typeof(Drawer))]
[TemplatePart(Name = PartOverlayButton, Type = typeof(Button))]
public class DrawersHost : ItemsControl
{
    private const string PartOverlayButton = "PART_OverlayButton";

    private static readonly DependencyPropertyDescriptor? ThemeDescriptor =
        DependencyPropertyDescriptor.FromProperty(WindowThemeHelper.CurrentThemeProperty, typeof(Window));

    private Button? _overlayButton;
    private Window? _ownerWindow;
    private LyuWindow? _ownerLyuWindow;

    public static readonly DependencyProperty MainContentProperty = DependencyProperty.Register(
        nameof(MainContent),
        typeof(object),
        typeof(DrawersHost),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty OverlayBrushProperty = DependencyProperty.Register(
        nameof(OverlayBrush),
        typeof(Brush),
        typeof(DrawersHost),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty OverlayOpacityProperty = DependencyProperty.Register(
        nameof(OverlayOpacity),
        typeof(double),
        typeof(DrawersHost),
        new PropertyMetadata(0.35d)
    );

    public static readonly DependencyProperty OverlayModeProperty = DependencyProperty.Register(
        nameof(OverlayMode),
        typeof(DrawerOverlayMode),
        typeof(DrawersHost),
        new PropertyMetadata(DrawerOverlayMode.WhenAnyOpen, OnOverlayBehaviorPropertyChanged)
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
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(DrawersHost),
            new FrameworkPropertyMetadata(typeof(DrawersHost))
        );
    }

    public DrawersHost()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        AddHandler(Drawer.IsOpenChangedEvent, new RoutedEventHandler(OnDrawerIsOpenChanged));
        PreviewKeyDown += OnPreviewKeyDown;
    }

    public object? MainContent
    {
        get => GetValue(MainContentProperty);
        set => SetValue(MainContentProperty, value);
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
        if (_overlayButton is not null)
        {
            _overlayButton.Click -= OverlayButtonOnClick;
        }

        base.OnApplyTemplate();

        _overlayButton = GetTemplateChild(PartOverlayButton) as Button;
        if (_overlayButton is not null)
        {
            _overlayButton.Click += OverlayButtonOnClick;
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

        ReorderZIndices(changedDrawer);
        UpdateOverlayState();
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

    private void OverlayButtonOnClick(object sender, RoutedEventArgs e)
    {
        if (!CloseOnOverlayClick)
        {
            return;
        }

        List<Drawer> closable = GetOpenDrawers()
            .Where(d => IsOverlayParticipant(d) && d.CloseOnOverlayClick && !d.IsPinned)
            .OrderByDescending(Panel.GetZIndex)
            .ToList();

        if (closable.Count == 0)
        {
            return;
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

        e.Handled = true;
    }

    private void UpdateOverlayState()
    {
        bool hasVisibleOverlay = GetOpenDrawers().Any(IsOverlayParticipant);
        SetValue(HasVisibleOverlayPropertyKey, hasVisibleOverlay);
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
}
