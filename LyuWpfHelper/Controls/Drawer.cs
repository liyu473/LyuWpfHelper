using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LyuWpfHelper.Helpers;

namespace LyuWpfHelper.Controls;

[TemplatePart(Name = PartRoot, Type = typeof(Border))]
[TemplatePart(Name = PartContentHost, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartCloseButton, Type = typeof(Button))]
public class Drawer : HeaderedContentControl
{
    private const string PartRoot = "PART_Root";
    private const string PartContentHost = "PART_ContentHost";
    private const string PartCloseButton = "PART_CloseButton";

    private static readonly DependencyPropertyDescriptor? ThemeDescriptor =
        DependencyPropertyDescriptor.FromProperty(WindowThemeHelper.CurrentThemeProperty, typeof(Window));

    private Border? _rootElement;
    private FrameworkElement? _contentHost;
    private Button? _closeButton;
    private TranslateTransform? _translateTransform;
    private DispatcherTimer? _autoCloseTimer;
    private Window? _ownerWindow;
    private LyuWindow? _ownerLyuWindow;
    private int _animationVersion;

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

    public static readonly DependencyProperty IsPinnedProperty = DependencyProperty.Register(
        nameof(IsPinned),
        typeof(bool),
        typeof(Drawer),
        new PropertyMetadata(false)
    );

    public static readonly DependencyProperty IsModalProperty = DependencyProperty.Register(
        nameof(IsModal),
        typeof(bool),
        typeof(Drawer),
        new PropertyMetadata(false, OnOverlayRelevantPropertyChanged)
    );

    public static readonly DependencyProperty ShowOverlayProperty = DependencyProperty.Register(
        nameof(ShowOverlay),
        typeof(bool),
        typeof(Drawer),
        new PropertyMetadata(true, OnOverlayRelevantPropertyChanged)
    );

    public static readonly DependencyProperty ShowHeaderProperty = DependencyProperty.Register(
        nameof(ShowHeader),
        typeof(bool),
        typeof(Drawer),
        new PropertyMetadata(true)
    );

    public static readonly DependencyProperty ShowCloseButtonProperty = DependencyProperty.Register(
        nameof(ShowCloseButton),
        typeof(bool),
        typeof(Drawer),
        new PropertyMetadata(true)
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
        new PropertyMetadata(true)
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
        new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(260)))
    );

    public static readonly DependencyProperty AutoCloseDelayProperty = DependencyProperty.Register(
        nameof(AutoCloseDelay),
        typeof(TimeSpan),
        typeof(Drawer),
        new PropertyMetadata(TimeSpan.Zero, OnAutoCloseDelayChanged)
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
        new PropertyMetadata(true)
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

    public bool IsPinned
    {
        get => (bool)GetValue(IsPinnedProperty);
        set => SetValue(IsPinnedProperty, value);
    }

    public bool IsModal
    {
        get => (bool)GetValue(IsModalProperty);
        set => SetValue(IsModalProperty, value);
    }

    public bool ShowOverlay
    {
        get => (bool)GetValue(ShowOverlayProperty);
        set => SetValue(ShowOverlayProperty, value);
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

        _rootElement = GetTemplateChild(PartRoot) as Border;
        _contentHost = GetTemplateChild(PartContentHost) as FrameworkElement;
        _closeButton = GetTemplateChild(PartCloseButton) as Button;
        if (_closeButton is not null)
        {
            _closeButton.Click += CloseButtonOnClick;
        }

        EnsureTranslateTransform();
        UpdateResolvedTheme();
        ApplyState(IsOpen, useTransitions: false, raiseEvents: false);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        if (!IsOpen && (sizeInfo.WidthChanged || sizeInfo.HeightChanged))
        {
            ApplyHiddenState();
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

        if (!drawer.IsOpen)
        {
            drawer.ApplyHiddenState();
            return;
        }

        if (!drawer.AreAnimationsEnabled || !drawer.AnimateOnPositionChange)
        {
            drawer.ApplyState(isOpen: true, useTransitions: false, raiseEvents: false);
            return;
        }

        drawer.ApplyState(isOpen: true, useTransitions: true, raiseEvents: false, startFromHidden: true);
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Drawer drawer)
        {
            return;
        }

        bool isOpen = (bool)e.NewValue;
        drawer.RaiseEvent(new RoutedEventArgs(IsOpenChangedEvent, drawer));
        drawer.ApplyState(isOpen, useTransitions: true, raiseEvents: true);
    }

    private static void OnThemeModeChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is Drawer drawer)
        {
            drawer.UpdateResolvedTheme();
        }
    }

    private static void OnOverlayRelevantPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is Drawer { IsOpen: true } drawer)
        {
            drawer.RaiseEvent(new RoutedEventArgs(IsOpenChangedEvent, drawer));
        }
    }

    private static void OnAutoCloseDelayChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is Drawer drawer)
        {
            drawer.ConfigureAutoCloseTimer();
        }
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

        if (!drawer.IsOpen)
        {
            drawer.ApplyHiddenState();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachOwnerWindow();
        UpdateResolvedTheme();
        ConfigureAutoCloseTimer();
        ApplyState(IsOpen, useTransitions: false, raiseEvents: false);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachOwnerWindow();
        StopAutoCloseTimer();
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

    private void ConfigureAutoCloseTimer()
    {
        StopAutoCloseTimer();

        if (AutoCloseDelay <= TimeSpan.Zero)
        {
            return;
        }

        _autoCloseTimer ??= new DispatcherTimer(DispatcherPriority.Normal, Dispatcher)
        {
            IsEnabled = false,
        };
        _autoCloseTimer.Tick -= AutoCloseTimerOnTick;
        _autoCloseTimer.Tick += AutoCloseTimerOnTick;
        _autoCloseTimer.Interval = AutoCloseDelay;

        if (IsOpen)
        {
            StartAutoCloseTimer();
        }
    }

    private void StartAutoCloseTimer()
    {
        if (_autoCloseTimer is null || AutoCloseDelay <= TimeSpan.Zero)
        {
            return;
        }

        _autoCloseTimer.Stop();
        _autoCloseTimer.Start();
    }

    private void StopAutoCloseTimer()
    {
        _autoCloseTimer?.Stop();
    }

    private void AutoCloseTimerOnTick(object? sender, EventArgs e)
    {
        StopAutoCloseTimer();
        if (IsOpen)
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

    private void ApplyState(
        bool isOpen,
        bool useTransitions,
        bool raiseEvents,
        bool startFromHidden = false
    )
    {
        if (_rootElement is null)
        {
            Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;
            SetValue(IsShownPropertyKey, isOpen);
            return;
        }

        EnsureTranslateTransform();
        if (_translateTransform is null)
        {
            return;
        }

        if (isOpen)
        {
            StopCurrentAnimations();

            if (startFromHidden || Visibility is not Visibility.Visible)
            {
                (double hiddenX, double hiddenY) = GetHiddenPosition();
                _translateTransform.X = hiddenX;
                _translateTransform.Y = hiddenY;
                _rootElement.Opacity = AnimateOpacity ? 0d : 1d;
            }

            Visibility = Visibility.Visible;
            SetValue(IsShownPropertyKey, false);

            if (!AreAnimationsEnabled || !useTransitions)
            {
                _translateTransform.X = 0d;
                _translateTransform.Y = 0d;
                _rootElement.Opacity = 1d;
                SetValue(IsShownPropertyKey, true);
                if (raiseEvents)
                {
                    RaiseEvent(new RoutedEventArgs(OpenedEvent, this));
                }
            }
            else
            {
                AnimateTo(0d, 0d, 1d, () =>
                {
                    SetValue(IsShownPropertyKey, true);
                    if (raiseEvents)
                    {
                        RaiseEvent(new RoutedEventArgs(OpenedEvent, this));
                    }
                });
            }

            if (FocusContentOnOpen)
            {
                _ = Dispatcher.BeginInvoke(TryFocusDrawer, DispatcherPriority.Input);
            }

            StartAutoCloseTimer();
            return;
        }

        StopAutoCloseTimer();
        StopCurrentAnimations();
        SetValue(IsShownPropertyKey, false);

        (double targetX, double targetY) = GetHiddenPosition();
        double targetOpacity = AnimateOpacity ? 0d : 1d;

        if (!AreAnimationsEnabled || !useTransitions)
        {
            _translateTransform.X = targetX;
            _translateTransform.Y = targetY;
            _rootElement.Opacity = targetOpacity;
            Visibility = Visibility.Collapsed;
            if (raiseEvents)
            {
                RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
            }
            return;
        }

        AnimateTo(targetX, targetY, targetOpacity, () =>
        {
            Visibility = Visibility.Collapsed;
            if (raiseEvents)
            {
                RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
            }
        });
    }

    private void ApplyHiddenState()
    {
        if (_rootElement is null)
        {
            return;
        }

        EnsureTranslateTransform();
        if (_translateTransform is null)
        {
            return;
        }

        (double x, double y) = GetHiddenPosition();
        _translateTransform.X = x;
        _translateTransform.Y = y;
        _rootElement.Opacity = AnimateOpacity ? 0d : 1d;
        Visibility = Visibility.Collapsed;
        SetValue(IsShownPropertyKey, false);
    }

    private (double X, double Y) GetHiddenPosition()
    {
        double extraOffset = Math.Max(0d, HiddenOffset);
        return Position switch
        {
            DrawerPosition.Left => (-(ResolveWidth() + extraOffset), 0d),
            DrawerPosition.Right => (ResolveWidth() + extraOffset, 0d),
            DrawerPosition.Top => (0d, -(ResolveHeight() + extraOffset)),
            DrawerPosition.Bottom => (0d, ResolveHeight() + extraOffset),
            _ => (ResolveWidth() + extraOffset, 0d),
        };
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

        return 280d;
    }

    private void AnimateTo(double x, double y, double opacity, Action completed)
    {
        if (_rootElement is null || _translateTransform is null)
        {
            completed();
            return;
        }

        TimeSpan duration = ResolveDuration();
        if (duration <= TimeSpan.Zero)
        {
            _translateTransform.X = x;
            _translateTransform.Y = y;
            _rootElement.Opacity = opacity;
            completed();
            return;
        }

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        int version = ++_animationVersion;

        DoubleAnimation xAnimation = CreateAnimation(x, duration, easing);
        DoubleAnimation yAnimation = CreateAnimation(y, duration, easing);

        DoubleAnimation completionAnimation =
            Position is DrawerPosition.Left or DrawerPosition.Right ? xAnimation : yAnimation;
        completionAnimation.Completed += (_, _) =>
        {
            if (version != _animationVersion)
            {
                return;
            }

            _translateTransform.X = x;
            _translateTransform.Y = y;
            _rootElement.Opacity = opacity;
            completed();
        };

        _translateTransform.BeginAnimation(
            TranslateTransform.XProperty,
            xAnimation,
            HandoffBehavior.SnapshotAndReplace
        );
        _translateTransform.BeginAnimation(
            TranslateTransform.YProperty,
            yAnimation,
            HandoffBehavior.SnapshotAndReplace
        );

        if (AnimateOpacity)
        {
            DoubleAnimation opacityAnimation = CreateAnimation(opacity, duration, easing);
            _rootElement.BeginAnimation(
                OpacityProperty,
                opacityAnimation,
                HandoffBehavior.SnapshotAndReplace
            );
        }
        else
        {
            _rootElement.Opacity = 1d;
        }
    }

    private static DoubleAnimation CreateAnimation(
        double target,
        TimeSpan duration,
        IEasingFunction easing
    ) =>
        new()
        {
            To = target,
            Duration = duration,
            EasingFunction = easing,
            FillBehavior = FillBehavior.Stop,
        };

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

    private void StopCurrentAnimations()
    {
        _animationVersion++;

        EnsureTranslateTransform();
        if (_translateTransform is not null && !_translateTransform.IsFrozen)
        {
            _translateTransform.BeginAnimation(TranslateTransform.XProperty, null);
            _translateTransform.BeginAnimation(TranslateTransform.YProperty, null);
        }

        _rootElement?.BeginAnimation(OpacityProperty, null);
    }

    private void EnsureTranslateTransform()
    {
        if (_rootElement is null)
        {
            return;
        }

        if (_rootElement.RenderTransform is TranslateTransform transform)
        {
            // WPF may provide a frozen transform instance from template internals.
            // Clone to a mutable instance before writing/animating X/Y.
            if (transform.IsFrozen)
            {
                _translateTransform = transform.CloneCurrentValue();
                _rootElement.RenderTransform = _translateTransform;
                return;
            }

            _translateTransform = transform;
            return;
        }

        _translateTransform = new TranslateTransform();
        _rootElement.RenderTransform = _translateTransform;
    }

    private void TryFocusDrawer()
    {
        if (!IsOpen || !IsVisible)
        {
            return;
        }

        _ = Focus();

        if (FocusedElement is not null)
        {
            _ = FocusedElement.Focus();
            return;
        }

        if (_contentHost is null)
        {
            return;
        }

        _ = _contentHost.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }
}
