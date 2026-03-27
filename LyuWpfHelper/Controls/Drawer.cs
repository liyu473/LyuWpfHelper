using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LyuWpfHelper.Helpers;

namespace LyuWpfHelper.Controls;

[TemplatePart(Name = PartRoot, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartHeaderContainer, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartContentHost, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartCloseButton, Type = typeof(Button))]
public class Drawer : HeaderedContentControl
{
    private const string PartRoot = "PART_Root";
    private const string PartHeaderContainer = "PART_HeaderContainer";
    private const string PartContentHost = "PART_ContentHost";
    private const string PartCloseButton = "PART_CloseButton";
    private const string StateHide = "Hide";
    private const string StateShow = "Show";
    private const string StateHideDirect = "HideDirect";
    private const string StateShowDirect = "ShowDirect";

    private static readonly DependencyPropertyDescriptor? ThemeDescriptor =
        DependencyPropertyDescriptor.FromProperty(WindowThemeHelper.CurrentThemeProperty, typeof(Window));

    private FrameworkElement? _rootElement;
    private FrameworkElement? _headerContainer;
    private FrameworkElement? _contentHost;
    private Button? _closeButton;
    private DispatcherTimer? _autoCloseTimer;
    private Window? _ownerWindow;
    private LyuWindow? _ownerLyuWindow;
    private Storyboard? _showStoryboard;
    private Storyboard? _hideStoryboard;
    private SplineDoubleKeyFrame? _hideFrame;
    private SplineDoubleKeyFrame? _hideFrameY;
    private SplineDoubleKeyFrame? _showFrame;
    private SplineDoubleKeyFrame? _showFrameY;
    private SplineDoubleKeyFrame? _fadeOutFrame;
    private SplineDoubleKeyFrame? _fadeInFrame;
    private bool _isSyncingVisibilityState;
    private bool _isSyncingFocusState;
    private bool _isSyncingAutoCloseState;
    private bool _isSyncingThemeState;

    public static readonly RoutedEvent IsOpenChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(IsOpenChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(Drawer)
    );

    public static readonly RoutedEvent OpenedEvent = EventManager.RegisterRoutedEvent(
        nameof(Opened),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(Drawer)
    );

    public static readonly RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent(
        nameof(Closed),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(Drawer)
    );

    public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
        nameof(Position),
        typeof(DrawerPosition),
        typeof(Drawer),
        new PropertyMetadata(DrawerPosition.Right, OnPositionChanged)
    );

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen),
        typeof(bool),
        typeof(Drawer),
        new FrameworkPropertyMetadata(
            false,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnIsOpenChanged
        )
    );

    private static readonly DependencyPropertyKey IsShownPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsShown),
            typeof(bool),
            typeof(Drawer),
            new PropertyMetadata(false)
        );

    public static readonly DependencyProperty IsShownProperty = IsShownPropertyKey.DependencyProperty;

    public static readonly DependencyProperty IsModalProperty = DependencyProperty.Register(
        nameof(IsModal),
        typeof(bool),
        typeof(Drawer),
        new PropertyMetadata(true, OnOverlayRelevantPropertyChanged)
    );

    public static readonly DependencyProperty OverlayBrushProperty = DependencyProperty.Register(
        nameof(OverlayBrush),
        typeof(Brush),
        typeof(Drawer),
        new PropertyMetadata(null, OnOverlayVisualPropertyChanged)
    );

    public static readonly DependencyProperty OverlayOpacityProperty = DependencyProperty.Register(
        nameof(OverlayOpacity),
        typeof(double),
        typeof(Drawer),
        new PropertyMetadata(1d, OnOverlayVisualPropertyChanged)
    );

    public static readonly DependencyProperty ShowHeaderProperty = DependencyProperty.Register(
        nameof(ShowHeader),
        typeof(bool),
        typeof(Drawer),
        new PropertyMetadata(true, OnShowHeaderChanged)
    );

    public static readonly DependencyProperty ShowCloseButtonProperty = DependencyProperty.Register(
        nameof(ShowCloseButton),
        typeof(bool),
        typeof(Drawer),
        new PropertyMetadata(true, OnShowCloseButtonChanged)
    );

    public static readonly DependencyProperty TitleVisibilityProperty = DependencyProperty.Register(
        nameof(TitleVisibility),
        typeof(Visibility),
        typeof(Drawer),
        new PropertyMetadata(Visibility.Visible, OnTitleVisibilityChanged)
    );

    public static readonly DependencyProperty CloseButtonVisibilityProperty =
        DependencyProperty.Register(
            nameof(CloseButtonVisibility),
            typeof(Visibility),
            typeof(Drawer),
            new PropertyMetadata(Visibility.Visible, OnCloseButtonVisibilityChanged)
        );

    public static readonly DependencyProperty CloseButtonIsCancelProperty =
        DependencyProperty.Register(
            nameof(CloseButtonIsCancel),
            typeof(bool),
            typeof(Drawer),
            new PropertyMetadata(false)
        );

    public static readonly DependencyProperty CloseOnEscapeProperty = DependencyProperty.Register(
        nameof(CloseOnEscape),
        typeof(bool),
        typeof(Drawer),
        new PropertyMetadata(true)
    );

    public static readonly DependencyProperty CloseOnOverlayClickProperty = DependencyProperty.Register(
        nameof(CloseOnOverlayClick),
        typeof(bool),
        typeof(Drawer),
        new PropertyMetadata(true)
    );

    public static readonly DependencyProperty AreAnimationsEnabledProperty = DependencyProperty.Register(
        nameof(AreAnimationsEnabled),
        typeof(bool),
        typeof(Drawer),
        new PropertyMetadata(true)
    );

    public static readonly DependencyProperty AnimateOpacityProperty = DependencyProperty.Register(
        nameof(AnimateOpacity),
        typeof(bool),
        typeof(Drawer),
        new FrameworkPropertyMetadata(false, OnAnimateOpacityChanged)
    );

    public static readonly DependencyProperty AnimateOnPositionChangeProperty =
        DependencyProperty.Register(
            nameof(AnimateOnPositionChange),
            typeof(bool),
            typeof(Drawer),
            new PropertyMetadata(true)
        );

    public static readonly DependencyProperty AnimationDurationProperty = DependencyProperty.Register(
        nameof(AnimationDuration),
        typeof(Duration),
        typeof(Drawer),
        new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(500)), OnAnimationDurationChanged)
    );

    public static readonly DependencyProperty AutoCloseDelayProperty = DependencyProperty.Register(
        nameof(AutoCloseDelay),
        typeof(TimeSpan),
        typeof(Drawer),
        new PropertyMetadata(TimeSpan.Zero, OnAutoCloseDelayChanged)
    );

    public static readonly DependencyProperty IsAutoCloseEnabledProperty = DependencyProperty.Register(
        nameof(IsAutoCloseEnabled),
        typeof(bool),
        typeof(Drawer),
        new PropertyMetadata(false, OnIsAutoCloseEnabledChanged)
    );

    public static readonly DependencyProperty AutoCloseIntervalProperty = DependencyProperty.Register(
        nameof(AutoCloseInterval),
        typeof(long),
        typeof(Drawer),
        new PropertyMetadata(5000L, OnAutoCloseIntervalChanged)
    );

    public static readonly DependencyProperty HiddenOffsetProperty = DependencyProperty.Register(
        nameof(HiddenOffset),
        typeof(double),
        typeof(Drawer),
        new PropertyMetadata(0d, OnHiddenOffsetChanged)
    );

    public static readonly DependencyProperty FocusContentOnOpenProperty = DependencyProperty.Register(
        nameof(FocusContentOnOpen),
        typeof(bool),
        typeof(Drawer),
        new PropertyMetadata(true, OnFocusContentOnOpenChanged)
    );

    public static readonly DependencyProperty AllowFocusElementProperty = DependencyProperty.Register(
        nameof(AllowFocusElement),
        typeof(bool),
        typeof(Drawer),
        new PropertyMetadata(true, OnAllowFocusElementChanged)
    );

    public static readonly DependencyProperty FocusedElementProperty = DependencyProperty.Register(
        nameof(FocusedElement),
        typeof(FrameworkElement),
        typeof(Drawer),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty ThemeModeProperty = DependencyProperty.Register(
        nameof(ThemeMode),
        typeof(DrawerThemeMode),
        typeof(Drawer),
        new PropertyMetadata(DrawerThemeMode.Adapt, OnThemeModeChanged)
    );

    public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register(
        nameof(Theme),
        typeof(DrawerThemeMode),
        typeof(Drawer),
        new PropertyMetadata(DrawerThemeMode.Adapt, OnThemeChanged)
    );

    private static readonly DependencyPropertyKey ResolvedThemePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(ResolvedTheme),
            typeof(WindowThemeMode),
            typeof(Drawer),
            new PropertyMetadata(WindowThemeMode.Light)
        );

    public static readonly DependencyProperty ResolvedThemeProperty =
        ResolvedThemePropertyKey.DependencyProperty;

    public static readonly DependencyProperty HeaderBackgroundProperty = DependencyProperty.Register(
        nameof(HeaderBackground),
        typeof(Brush),
        typeof(Drawer),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty HeaderForegroundProperty = DependencyProperty.Register(
        nameof(HeaderForeground),
        typeof(Brush),
        typeof(Drawer),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty CloseButtonForegroundProperty =
        DependencyProperty.Register(
            nameof(CloseButtonForeground),
            typeof(Brush),
            typeof(Drawer),
            new PropertyMetadata(null)
        );

    public static readonly DependencyProperty CloseButtonHoverBackgroundProperty =
        DependencyProperty.Register(
            nameof(CloseButtonHoverBackground),
            typeof(Brush),
            typeof(Drawer),
            new PropertyMetadata(null)
        );

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius),
        typeof(CornerRadius),
        typeof(Drawer),
        new PropertyMetadata(new CornerRadius(0))
    );

    public static readonly DependencyProperty HeaderPaddingProperty = DependencyProperty.Register(
        nameof(HeaderPadding),
        typeof(Thickness),
        typeof(Drawer),
        new PropertyMetadata(new Thickness(16, 12, 10, 12))
    );

    public static readonly DependencyProperty ContentPaddingProperty = DependencyProperty.Register(
        nameof(ContentPadding),
        typeof(Thickness),
        typeof(Drawer),
        new PropertyMetadata(new Thickness(16))
    );

    public static readonly DependencyProperty CloseCommandProperty = DependencyProperty.Register(
        nameof(CloseCommand),
        typeof(ICommand),
        typeof(Drawer),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty CloseCommandParameterProperty =
        DependencyProperty.Register(
            nameof(CloseCommandParameter),
            typeof(object),
            typeof(Drawer),
            new PropertyMetadata(null)
        );

    static Drawer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(Drawer),
            new FrameworkPropertyMetadata(typeof(Drawer))
        );
    }

    public Drawer()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        InitializeAutoCloseTimer();
    }

    public event RoutedEventHandler IsOpenChanged
    {
        add => AddHandler(IsOpenChangedEvent, value);
        remove => RemoveHandler(IsOpenChangedEvent, value);
    }

    public event RoutedEventHandler Opened
    {
        add => AddHandler(OpenedEvent, value);
        remove => RemoveHandler(OpenedEvent, value);
    }

    public event RoutedEventHandler Closed
    {
        add => AddHandler(ClosedEvent, value);
        remove => RemoveHandler(ClosedEvent, value);
    }

    public DrawerPosition Position
    {
        get => (DrawerPosition)GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public bool IsShown => (bool)GetValue(IsShownProperty);

    public bool IsModal
    {
        get => (bool)GetValue(IsModalProperty);
        set => SetValue(IsModalProperty, value);
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

    public bool ShowHeader
    {
        get => (bool)GetValue(ShowHeaderProperty);
        set => SetValue(ShowHeaderProperty, value);
    }

    public bool ShowCloseButton
    {
        get => (bool)GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, value);
    }

    public Visibility TitleVisibility
    {
        get => (Visibility)GetValue(TitleVisibilityProperty);
        set => SetValue(TitleVisibilityProperty, value);
    }

    public Visibility CloseButtonVisibility
    {
        get => (Visibility)GetValue(CloseButtonVisibilityProperty);
        set => SetValue(CloseButtonVisibilityProperty, value);
    }

    public bool CloseButtonIsCancel
    {
        get => (bool)GetValue(CloseButtonIsCancelProperty);
        set => SetValue(CloseButtonIsCancelProperty, value);
    }

    public bool CloseOnEscape
    {
        get => (bool)GetValue(CloseOnEscapeProperty);
        set => SetValue(CloseOnEscapeProperty, value);
    }

    public bool CloseOnOverlayClick
    {
        get => (bool)GetValue(CloseOnOverlayClickProperty);
        set => SetValue(CloseOnOverlayClickProperty, value);
    }

    public bool AreAnimationsEnabled
    {
        get => (bool)GetValue(AreAnimationsEnabledProperty);
        set => SetValue(AreAnimationsEnabledProperty, value);
    }

    public bool AnimateOpacity
    {
        get => (bool)GetValue(AnimateOpacityProperty);
        set => SetValue(AnimateOpacityProperty, value);
    }

    public bool AnimateOnPositionChange
    {
        get => (bool)GetValue(AnimateOnPositionChangeProperty);
        set => SetValue(AnimateOnPositionChangeProperty, value);
    }

    public Duration AnimationDuration
    {
        get => (Duration)GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }

    public TimeSpan AutoCloseDelay
    {
        get => (TimeSpan)GetValue(AutoCloseDelayProperty);
        set => SetValue(AutoCloseDelayProperty, value);
    }

    public bool IsAutoCloseEnabled
    {
        get => (bool)GetValue(IsAutoCloseEnabledProperty);
        set => SetValue(IsAutoCloseEnabledProperty, value);
    }

    public long AutoCloseInterval
    {
        get => (long)GetValue(AutoCloseIntervalProperty);
        set => SetValue(AutoCloseIntervalProperty, value);
    }

    public double HiddenOffset
    {
        get => (double)GetValue(HiddenOffsetProperty);
        set => SetValue(HiddenOffsetProperty, value);
    }

    public bool FocusContentOnOpen
    {
        get => (bool)GetValue(FocusContentOnOpenProperty);
        set => SetValue(FocusContentOnOpenProperty, value);
    }

    public bool AllowFocusElement
    {
        get => (bool)GetValue(AllowFocusElementProperty);
        set => SetValue(AllowFocusElementProperty, value);
    }

    public FrameworkElement? FocusedElement
    {
        get => (FrameworkElement?)GetValue(FocusedElementProperty);
        set => SetValue(FocusedElementProperty, value);
    }

    public DrawerThemeMode ThemeMode
    {
        get => (DrawerThemeMode)GetValue(ThemeModeProperty);
        set => SetValue(ThemeModeProperty, value);
    }

    public DrawerThemeMode Theme
    {
        get => (DrawerThemeMode)GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    public WindowThemeMode ResolvedTheme => (WindowThemeMode)GetValue(ResolvedThemeProperty);

    public Brush? HeaderBackground
    {
        get => (Brush?)GetValue(HeaderBackgroundProperty);
        set => SetValue(HeaderBackgroundProperty, value);
    }

    public Brush? HeaderForeground
    {
        get => (Brush?)GetValue(HeaderForegroundProperty);
        set => SetValue(HeaderForegroundProperty, value);
    }

    public Brush? CloseButtonForeground
    {
        get => (Brush?)GetValue(CloseButtonForegroundProperty);
        set => SetValue(CloseButtonForegroundProperty, value);
    }

    public Brush? CloseButtonHoverBackground
    {
        get => (Brush?)GetValue(CloseButtonHoverBackgroundProperty);
        set => SetValue(CloseButtonHoverBackgroundProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public Thickness HeaderPadding
    {
        get => (Thickness)GetValue(HeaderPaddingProperty);
        set => SetValue(HeaderPaddingProperty, value);
    }

    public Thickness ContentPadding
    {
        get => (Thickness)GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public object? CloseCommandParameter
    {
        get => GetValue(CloseCommandParameterProperty);
        set => SetValue(CloseCommandParameterProperty, value);
    }

    public override void OnApplyTemplate()
    {
        if (_closeButton is not null)
        {
            _closeButton.Click -= CloseButtonOnClick;
        }

        base.OnApplyTemplate();

        _rootElement = GetTemplateChild(PartRoot) as FrameworkElement;
        _headerContainer = GetTemplateChild(PartHeaderContainer) as FrameworkElement;
        _contentHost = GetTemplateChild(PartContentHost) as FrameworkElement;
        _closeButton = GetTemplateChild(PartCloseButton) as Button;
        if (_closeButton is not null)
        {
            _closeButton.Click += CloseButtonOnClick;
        }

#pragma warning disable WPF0130 // Add [TemplatePart] to the type.
        _showStoryboard = GetTemplateChild("ShowStoryboard") as Storyboard;
        _hideStoryboard = GetTemplateChild("HideStoryboard") as Storyboard;
        _hideFrame = GetTemplateChild("hideFrame") as SplineDoubleKeyFrame;
        _hideFrameY = GetTemplateChild("hideFrameY") as SplineDoubleKeyFrame;
        _showFrame = GetTemplateChild("showFrame") as SplineDoubleKeyFrame;
        _showFrameY = GetTemplateChild("showFrameY") as SplineDoubleKeyFrame;
        _fadeOutFrame = GetTemplateChild("fadeOutFrame") as SplineDoubleKeyFrame;
        _fadeInFrame = GetTemplateChild("fadeInFrame") as SplineDoubleKeyFrame;
#pragma warning restore WPF0130 // Add [TemplatePart] to the type.

        UpdateAnimationDuration();
        UpdateOpacityChange();
        ApplyAnimation(Position, AnimateOpacity);
        UpdateResolvedTheme();

        _ = VisualStateManager.GoToState(this, IsOpen ? StateShowDirect : StateHideDirect, false);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);

        if (!IsOpen)
        {
            return;
        }

        if (!sizeInfo.WidthChanged && !sizeInfo.HeightChanged)
        {
            return;
        }

        if (
            _rootElement is null
            || _hideFrame is null
            || _showFrame is null
            || _hideFrameY is null
            || _showFrameY is null
        )
        {
            return;
        }

        if (Position is DrawerPosition.Left or DrawerPosition.Right)
        {
            _showFrame.Value = 0d;
        }

        if (Position is DrawerPosition.Top or DrawerPosition.Bottom)
        {
            _showFrameY.Value = 0d;
        }

        double width = ResolveWidth();
        double height = ResolveHeight();
        double offset = Math.Max(0d, HiddenOffset);

        switch (Position)
        {
            default:
            case DrawerPosition.Left:
                _hideFrame.Value = -width - Margin.Left - offset;
                break;
            case DrawerPosition.Right:
                _hideFrame.Value = width + Margin.Right + offset;
                break;
            case DrawerPosition.Top:
                _hideFrameY.Value = -height - 1d - Margin.Top - offset;
                break;
            case DrawerPosition.Bottom:
                _hideFrameY.Value = height + Margin.Bottom + offset;
                break;
        }
    }

    public void Open() => SetCurrentValue(IsOpenProperty, true);

    public void Close() => SetCurrentValue(IsOpenProperty, false);

    private static void OnPositionChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not Drawer drawer)
        {
            return;
        }

        bool wasOpen = drawer.IsOpen;
        DrawerPosition newPosition = (DrawerPosition)e.NewValue;

        if (wasOpen && drawer.AnimateOnPositionChange && drawer.AreAnimationsEnabled)
        {
            drawer.ApplyAnimation(newPosition, drawer.AnimateOpacity);
            _ = VisualStateManager.GoToState(drawer, StateHide, true);
        }
        else
        {
            drawer.ApplyAnimation(newPosition, drawer.AnimateOpacity, false);
        }

        if (wasOpen && drawer.AnimateOnPositionChange && drawer.AreAnimationsEnabled)
        {
            drawer.ApplyAnimation(newPosition, drawer.AnimateOpacity);
            _ = VisualStateManager.GoToState(drawer, StateShow, true);
        }
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Drawer drawer)
        {
            return;
        }
        Action openedChangedAction = () =>
        {
            if (!Equals(e.NewValue, e.OldValue))
            {
                drawer.ApplyOpenState((bool)e.NewValue);
            }

            drawer.RaiseEvent(new RoutedEventArgs(IsOpenChangedEvent, drawer));
            drawer.NotifyOwnerHostStateChanged();
        };

        _ = drawer.Dispatcher.BeginInvoke(DispatcherPriority.Background, openedChangedAction);
    }

    private static void OnAnimateOpacityChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is Drawer drawer)
        {
            drawer.UpdateOpacityChange();
            drawer.ApplyAnimation(drawer.Position, drawer.AnimateOpacity, resetShowFrame: false);
        }
    }

    private static void OnAnimationDurationChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is Drawer drawer)
        {
            drawer.UpdateAnimationDuration();
        }
    }

    private static void OnShowHeaderChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not Drawer drawer || drawer._isSyncingVisibilityState)
        {
            return;
        }

        drawer._isSyncingVisibilityState = true;
        drawer.SetCurrentValue(
            TitleVisibilityProperty,
            (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed
        );
        drawer._isSyncingVisibilityState = false;
    }

    private static void OnShowCloseButtonChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not Drawer drawer || drawer._isSyncingVisibilityState)
        {
            return;
        }

        drawer._isSyncingVisibilityState = true;
        drawer.SetCurrentValue(
            CloseButtonVisibilityProperty,
            (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed
        );
        drawer._isSyncingVisibilityState = false;
    }

    private static void OnTitleVisibilityChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not Drawer drawer || drawer._isSyncingVisibilityState)
        {
            return;
        }

        drawer._isSyncingVisibilityState = true;
        drawer.SetCurrentValue(
            ShowHeaderProperty,
            (Visibility)e.NewValue is Visibility.Visible
        );
        drawer._isSyncingVisibilityState = false;
    }

    private static void OnCloseButtonVisibilityChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not Drawer drawer || drawer._isSyncingVisibilityState)
        {
            return;
        }

        drawer._isSyncingVisibilityState = true;
        drawer.SetCurrentValue(
            ShowCloseButtonProperty,
            (Visibility)e.NewValue is Visibility.Visible
        );
        drawer._isSyncingVisibilityState = false;
    }

    private static void OnFocusContentOnOpenChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not Drawer drawer || drawer._isSyncingFocusState)
        {
            return;
        }

        drawer._isSyncingFocusState = true;
        drawer.SetCurrentValue(AllowFocusElementProperty, e.NewValue);
        drawer._isSyncingFocusState = false;
    }

    private static void OnAllowFocusElementChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not Drawer drawer || drawer._isSyncingFocusState)
        {
            return;
        }

        drawer._isSyncingFocusState = true;
        drawer.SetCurrentValue(FocusContentOnOpenProperty, e.NewValue);
        drawer._isSyncingFocusState = false;
    }

    private static void OnThemeModeChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is Drawer drawer)
        {
            if (!drawer._isSyncingThemeState)
            {
                drawer._isSyncingThemeState = true;
                drawer.SetCurrentValue(ThemeProperty, e.NewValue);
                drawer._isSyncingThemeState = false;
            }

            drawer.UpdateResolvedTheme();
        }
    }

    private static void OnThemeChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not Drawer drawer || drawer._isSyncingThemeState)
        {
            return;
        }

        // Respect explicit ThemeMode values (local value/binding) to avoid being
        // overridden by legacy Theme setter updates from styles.
        ValueSource themeModeSource = DependencyPropertyHelper.GetValueSource(drawer, ThemeModeProperty);
        if (themeModeSource.BaseValueSource == BaseValueSource.Local)
        {
            drawer.UpdateResolvedTheme();
            return;
        }

        drawer._isSyncingThemeState = true;
        drawer.SetCurrentValue(ThemeModeProperty, e.NewValue);
        drawer._isSyncingThemeState = false;
        drawer.UpdateResolvedTheme();
    }

    private static void OnOverlayRelevantPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is Drawer { IsOpen: true } drawer)
        {
            drawer.RaiseEvent(new RoutedEventArgs(IsOpenChangedEvent, drawer));
            drawer.NotifyOwnerHostStateChanged();
        }
    }

    private static void OnOverlayVisualPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is Drawer { IsOpen: true } drawer)
        {
            drawer.NotifyOwnerHostStateChanged();
        }
    }

    private static void OnAutoCloseDelayChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not Drawer drawer)
        {
            return;
        }

        if ((TimeSpan)e.NewValue < TimeSpan.Zero)
        {
            drawer.SetCurrentValue(AutoCloseDelayProperty, TimeSpan.Zero);
            return;
        }

        if (!drawer._isSyncingAutoCloseState)
        {
            drawer._isSyncingAutoCloseState = true;
            TimeSpan delay = (TimeSpan)e.NewValue;
            drawer.SetCurrentValue(IsAutoCloseEnabledProperty, delay > TimeSpan.Zero);
            drawer.SetCurrentValue(
                AutoCloseIntervalProperty,
                delay > TimeSpan.Zero ? (long)Math.Max(1d, delay.TotalMilliseconds) : 0L
            );
            drawer._isSyncingAutoCloseState = false;
        }

        Action autoCloseChangedAction = () =>
        {
            drawer.InitializeAutoCloseTimer();
            if (drawer.IsAutoCloseEnabled && drawer.IsOpen)
            {
                drawer.StartAutoCloseTimer();
            }
        };

        _ = drawer.Dispatcher.BeginInvoke(DispatcherPriority.Background, autoCloseChangedAction);
    }

    private static void OnIsAutoCloseEnabledChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not Drawer drawer || drawer._isSyncingAutoCloseState)
        {
            return;
        }

        drawer._isSyncingAutoCloseState = true;
        bool enabled = (bool)e.NewValue;
        if (!enabled)
        {
            drawer.SetCurrentValue(AutoCloseDelayProperty, TimeSpan.Zero);
        }
        else if (drawer.AutoCloseDelay <= TimeSpan.Zero)
        {
            long interval = Math.Max(1L, drawer.AutoCloseInterval);
            drawer.SetCurrentValue(AutoCloseDelayProperty, TimeSpan.FromMilliseconds(interval));
        }
        drawer._isSyncingAutoCloseState = false;
    }

    private static void OnAutoCloseIntervalChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not Drawer drawer || drawer._isSyncingAutoCloseState)
        {
            return;
        }

        long interval = (long)e.NewValue;
        if (interval < 0L)
        {
            drawer.SetCurrentValue(AutoCloseIntervalProperty, 0L);
            return;
        }

        if (!drawer.IsAutoCloseEnabled)
        {
            return;
        }

        drawer._isSyncingAutoCloseState = true;
        drawer.SetCurrentValue(
            AutoCloseDelayProperty,
            interval <= 0L ? TimeSpan.Zero : TimeSpan.FromMilliseconds(interval)
        );
        drawer._isSyncingAutoCloseState = false;
    }

    private static void OnHiddenOffsetChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not Drawer drawer)
        {
            return;
        }

        if ((double)e.NewValue < 0)
        {
            drawer.SetCurrentValue(HiddenOffsetProperty, 0d);
            return;
        }

        drawer.ApplyAnimation(drawer.Position, drawer.AnimateOpacity, resetShowFrame: !drawer.IsOpen);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachOwnerWindow();
        UpdateResolvedTheme();
        ApplyAnimation(Position, AnimateOpacity);
        NotifyOwnerHostStateChanged();

        if (IsOpen && IsAutoCloseEnabled)
        {
            StartAutoCloseTimer();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachOwnerWindow();
        StopAutoCloseTimer();

        if (_showStoryboard is not null)
        {
            _showStoryboard.Completed -= ShowStoryboardCompleted;
        }

        if (_hideStoryboard is not null)
        {
            _hideStoryboard.Completed -= HideStoryboardCompleted;
        }
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
        UpdateResolvedTheme();
    }

    private void OwnerWindowAttachedThemeChanged(object? sender, EventArgs e)
    {
        UpdateResolvedTheme();
    }

    private void OwnerWindowClosed(object? sender, EventArgs e)
    {
        DetachOwnerWindow();
    }

    private void UpdateResolvedTheme()
    {
        WindowThemeMode windowTheme = ResolveOwnerTheme();
        WindowThemeMode resolvedTheme = ThemeMode switch
        {
            DrawerThemeMode.Light => WindowThemeMode.Light,
            DrawerThemeMode.Dark => WindowThemeMode.Dark,
            DrawerThemeMode.Inverse => windowTheme == WindowThemeMode.Dark
                ? WindowThemeMode.Light
                : WindowThemeMode.Dark,
            _ => windowTheme,
        };

        if (resolvedTheme == WindowThemeMode.FollowSystem)
        {
            resolvedTheme = WindowThemeMode.Light;
        }

        SetValue(ResolvedThemePropertyKey, resolvedTheme);
    }

    private WindowThemeMode ResolveOwnerTheme()
    {
        if (_ownerLyuWindow is not null)
        {
            return _ownerLyuWindow.EffectiveTheme == WindowThemeMode.FollowSystem
                ? WindowThemeHelper.GetEffectiveTheme(_ownerLyuWindow.EffectiveTheme)
                : _ownerLyuWindow.EffectiveTheme;
        }

        if (_ownerWindow is not null)
        {
            WindowThemeMode currentTheme = WindowThemeHelper.GetCurrentTheme(_ownerWindow);
            return WindowThemeHelper.GetEffectiveTheme(currentTheme);
        }

        return WindowThemeMode.Light;
    }

    private void InitializeAutoCloseTimer()
    {
        _autoCloseTimer ??= new DispatcherTimer(DispatcherPriority.Normal, Dispatcher);
        _autoCloseTimer.Tick -= AutoCloseTimerOnTick;
        _autoCloseTimer.Tick += AutoCloseTimerOnTick;
        _autoCloseTimer.Interval = AutoCloseDelay > TimeSpan.Zero
            ? AutoCloseDelay
            : TimeSpan.FromMilliseconds(1);
    }

    private void StartAutoCloseTimer()
    {
        if (_autoCloseTimer is null || !IsAutoCloseEnabled)
        {
            return;
        }

        if (DesignerProperties.GetIsInDesignMode(this))
        {
            return;
        }

        _autoCloseTimer.Stop();
        _autoCloseTimer.Interval = AutoCloseDelay;
        _autoCloseTimer.Start();
    }

    private void StopAutoCloseTimer()
    {
        if (_autoCloseTimer is not null && _autoCloseTimer.IsEnabled)
        {
            _autoCloseTimer.Stop();
        }
    }

    private void AutoCloseTimerOnTick(object? sender, EventArgs e)
    {
        StopAutoCloseTimer();
        if (IsOpen && IsAutoCloseEnabled)
        {
            SetCurrentValue(IsOpenProperty, false);
        }
    }

    private void CloseButtonOnClick(object sender, RoutedEventArgs e)
    {
        ExecuteCloseCommand();
        SetCurrentValue(IsOpenProperty, false);
        e.Handled = true;
    }

    private void ExecuteCloseCommand()
    {
        if (CloseCommand is null)
        {
            return;
        }

        if (CloseCommand.CanExecute(CloseCommandParameter))
        {
            CloseCommand.Execute(CloseCommandParameter);
        }
    }

    private void ApplyOpenState(bool isOpen)
    {
        if (_rootElement is null)
        {
            Visibility = isOpen ? Visibility.Visible : Visibility.Hidden;
            SetValue(IsShownPropertyKey, isOpen);
            RaiseEvent(new RoutedEventArgs(isOpen ? OpenedEvent : ClosedEvent, this));
            return;
        }

        if (AreAnimationsEnabled)
        {
            if (isOpen)
            {
                if (_hideStoryboard is not null)
                {
                    _hideStoryboard.Completed -= HideStoryboardCompleted;
                }

                Visibility = Visibility.Visible;
                ApplyAnimation(Position, AnimateOpacity);
                TryFocusDrawer();

                if (_showStoryboard is not null)
                {
                    _showStoryboard.Completed -= ShowStoryboardCompleted;
                    _showStoryboard.Completed += ShowStoryboardCompleted;
                }
                else
                {
                    Shown();
                }

                if (IsAutoCloseEnabled)
                {
                    StartAutoCloseTimer();
                }
            }
            else
            {
                if (_showStoryboard is not null)
                {
                    _showStoryboard.Completed -= ShowStoryboardCompleted;
                }

                StopAutoCloseTimer();
                SetValue(IsShownPropertyKey, false);

                if (_hideStoryboard is not null)
                {
                    _hideStoryboard.Completed -= HideStoryboardCompleted;
                    _hideStoryboard.Completed += HideStoryboardCompleted;
                }
                else
                {
                    Hide();
                }
            }

            _ = VisualStateManager.GoToState(this, isOpen ? StateShow : StateHide, true);
            return;
        }

        if (isOpen)
        {
            Visibility = Visibility.Visible;
            TryFocusDrawer();
            Shown();

            if (IsAutoCloseEnabled)
            {
                StartAutoCloseTimer();
            }
        }
        else
        {
            StopAutoCloseTimer();
            SetValue(IsShownPropertyKey, false);
            Hide();
        }

        _ = VisualStateManager.GoToState(this, isOpen ? StateShowDirect : StateHideDirect, true);
    }

    private void UpdateAnimationDuration()
    {
        TimeSpan duration = ResolveDuration();
        KeyTime keyTime = KeyTime.FromTimeSpan(duration);

        if (_hideFrame is not null)
        {
            _hideFrame.KeyTime = keyTime;
        }

        if (_hideFrameY is not null)
        {
            _hideFrameY.KeyTime = keyTime;
        }

        if (_showFrame is not null)
        {
            _showFrame.KeyTime = keyTime;
        }

        if (_showFrameY is not null)
        {
            _showFrameY.KeyTime = keyTime;
        }

        if (_fadeOutFrame is not null)
        {
            _fadeOutFrame.KeyTime = keyTime;
        }

        if (_fadeInFrame is not null)
        {
            _fadeInFrame.KeyTime = keyTime;
        }
    }

    private void NotifyOwnerHostStateChanged()
    {
        DrawersHost? ownerHost = TryFindParentHost(this);
        ownerHost?.NotifyDrawerStateChanged(this);
    }

    private static DrawersHost? TryFindParentHost(DependencyObject start)
    {
        DependencyObject? current = start;
        while (current is not null)
        {
            if (current is DrawersHost host)
            {
                return host;
            }

            current = current switch
            {
                Visual => VisualTreeHelper.GetParent(current),
                FrameworkContentElement fce => fce.Parent,
                _ => LogicalTreeHelper.GetParent(current) as DependencyObject,
            };
        }

        return null;
    }

    private void UpdateOpacityChange()
    {
        if (_rootElement is null || _fadeOutFrame is null)
        {
            return;
        }

        if (!AnimateOpacity)
        {
            _fadeOutFrame.Value = 1d;
            _rootElement.Opacity = 1d;
        }
        else
        {
            _fadeOutFrame.Value = 0d;
            if (!IsOpen)
            {
                _rootElement.Opacity = 0d;
            }
        }
    }

    private void HideStoryboardCompleted(object? sender, EventArgs e)
    {
        if (_hideStoryboard is not null)
        {
            _hideStoryboard.Completed -= HideStoryboardCompleted;
        }

        Hide();
    }

    private void ShowStoryboardCompleted(object? sender, EventArgs e)
    {
        if (_showStoryboard is not null)
        {
            _showStoryboard.Completed -= ShowStoryboardCompleted;
        }

        Shown();
    }

    private void Hide()
    {
        // Keep Hidden (instead of Collapsed) to preserve measure size for first open animation.
        Visibility = Visibility.Hidden;
        RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
    }

    private void Shown()
    {
        SetValue(IsShownPropertyKey, true);
        RaiseEvent(new RoutedEventArgs(OpenedEvent, this));
    }

    internal void ApplyAnimation(
        DrawerPosition position,
        bool animateOpacity,
        bool resetShowFrame = true
    )
    {
        if (
            _rootElement is null
            || _hideFrame is null
            || _showFrame is null
            || _hideFrameY is null
            || _showFrameY is null
            || _fadeOutFrame is null
        )
        {
            return;
        }

        double width = ResolveWidth();
        double height = ResolveHeight();
        double offset = Math.Max(0d, HiddenOffset);

        if (position is DrawerPosition.Left or DrawerPosition.Right)
        {
            _showFrame.Value = 0d;
        }

        if (position is DrawerPosition.Top or DrawerPosition.Bottom)
        {
            _showFrameY.Value = 0d;
        }

        if (!animateOpacity)
        {
            _fadeOutFrame.Value = 1d;
            _rootElement.Opacity = 1d;
        }
        else
        {
            _fadeOutFrame.Value = 0d;
            if (!IsOpen)
            {
                _rootElement.Opacity = 0d;
            }
        }

        switch (position)
        {
            default:
            case DrawerPosition.Left:
                HorizontalAlignment =
                    Margin.Right <= 0d
                        ? HorizontalContentAlignment != HorizontalAlignment.Stretch
                            ? HorizontalAlignment.Left
                            : HorizontalContentAlignment
                        : HorizontalAlignment.Stretch;
                VerticalAlignment = VerticalAlignment.Stretch;
                _hideFrame.Value = -width - Margin.Left - offset;
                if (resetShowFrame)
                {
                    _rootElement.RenderTransform = new TranslateTransform(-width - offset, 0d);
                }
                break;
            case DrawerPosition.Right:
                HorizontalAlignment =
                    Margin.Left <= 0d
                        ? HorizontalContentAlignment != HorizontalAlignment.Stretch
                            ? HorizontalAlignment.Right
                            : HorizontalContentAlignment
                        : HorizontalAlignment.Stretch;
                VerticalAlignment = VerticalAlignment.Stretch;
                _hideFrame.Value = width + Margin.Right + offset;
                if (resetShowFrame)
                {
                    _rootElement.RenderTransform = new TranslateTransform(width + offset, 0d);
                }
                break;
            case DrawerPosition.Top:
                HorizontalAlignment = HorizontalAlignment.Stretch;
                VerticalAlignment =
                    Margin.Bottom <= 0d
                        ? VerticalContentAlignment != VerticalAlignment.Stretch
                            ? VerticalAlignment.Top
                            : VerticalContentAlignment
                        : VerticalAlignment.Stretch;
                _hideFrameY.Value = -height - 1d - Margin.Top - offset;
                if (resetShowFrame)
                {
                    _rootElement.RenderTransform = new TranslateTransform(0d, -height - 1d - offset);
                }
                break;
            case DrawerPosition.Bottom:
                HorizontalAlignment = HorizontalAlignment.Stretch;
                VerticalAlignment =
                    Margin.Top <= 0d
                        ? VerticalContentAlignment != VerticalAlignment.Stretch
                            ? VerticalAlignment.Bottom
                            : VerticalContentAlignment
                        : VerticalAlignment.Stretch;
                _hideFrameY.Value = height + Margin.Bottom + offset;
                if (resetShowFrame)
                {
                    _rootElement.RenderTransform = new TranslateTransform(0d, height + offset);
                }
                break;
        }
    }

    private double ResolveWidth()
    {
        if (_rootElement?.ActualWidth > 0d)
        {
            return _rootElement.ActualWidth;
        }

        if (ActualWidth > 0d)
        {
            return ActualWidth;
        }

        if (!double.IsNaN(Width) && Width > 0d)
        {
            return Width;
        }

        return 320d;
    }

    private double ResolveHeight()
    {
        if (_rootElement?.ActualHeight > 0d)
        {
            return _rootElement.ActualHeight;
        }

        if (ActualHeight > 0d)
        {
            return ActualHeight;
        }

        if (!double.IsNaN(Height) && Height > 0d)
        {
            return Height;
        }

        return 260d;
    }

    private TimeSpan ResolveDuration()
    {
        if (!AnimationDuration.HasTimeSpan)
        {
            return TimeSpan.Zero;
        }

        if (AnimationDuration.TimeSpan < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return AnimationDuration.TimeSpan;
    }

    private void TryFocusDrawer()
    {
        if (!AllowFocusElement)
        {
            return;
        }

        _ = Focus();

        if (FocusedElement is not null)
        {
            _ = FocusedElement.Focus();
            return;
        }

        if (_contentHost is not null && _contentHost.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)))
        {
            return;
        }

        _ = _headerContainer?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }
}
