using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using LyuWpfHelper.Helpers;

namespace LyuWpfHelper.Controls;

/// <summary>
/// 带过渡动画效果的内容控件
/// </summary>
public class TransitioningContentControl : ContentControl
{
    private FrameworkElement? _contentPresenter;
    private Storyboard? _storyboardBuildIn;
    private Storyboard? _currentStoryboard;

    private static readonly Lazy<Storyboard?> StoryboardBuildInDefault = new(
        () => ResourceHelper.GetResource<Storyboard>($"{default(TransitionMode)}Transition"),
        isThreadSafe: true
    );

    static TransitioningContentControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(TransitioningContentControl),
            new FrameworkPropertyMetadata(typeof(TransitioningContentControl))
        );
    }

    /// <summary>
    /// 过渡动画模式依赖属性
    /// </summary>
    public static readonly DependencyProperty TransitionModeProperty =
        DependencyProperty.Register(
            nameof(TransitionMode),
            typeof(TransitionMode),
            typeof(TransitioningContentControl),
            new PropertyMetadata(TransitionMode.Right2Left, OnTransitionModeChanged)
        );

    /// <summary>
    /// 自定义过渡动画故事板依赖属性
    /// </summary>
    public static readonly DependencyProperty TransitionStoryboardProperty =
        DependencyProperty.Register(
            nameof(TransitionStoryboard),
            typeof(Storyboard),
            typeof(TransitioningContentControl)
        );

    /// <summary>
    /// 获取或设置过渡动画模式
    /// </summary>
    public TransitionMode TransitionMode
    {
        get => (TransitionMode)GetValue(TransitionModeProperty);
        set => SetValue(TransitionModeProperty, value);
    }

    /// <summary>
    /// 获取或设置自定义过渡动画故事板（优先级最高）
    /// </summary>
    public Storyboard? TransitionStoryboard
    {
        get => (Storyboard?)GetValue(TransitionStoryboardProperty);
        set => SetValue(TransitionStoryboardProperty, value);
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public TransitioningContentControl()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>
    /// 应用控件模板
    /// </summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _contentPresenter = GetTemplateChild("PART_ContentPresenter") as FrameworkElement;
        if (_contentPresenter != null)
        {
            // 设置变换原点为中心
            _contentPresenter.RenderTransformOrigin = new Point(0.5, 0.5);

            // 预设 TransformGroup（顺序固定：Scale, Skew, Rotate, Translate）
            _contentPresenter.RenderTransform = new TransformGroup
            {
                Children =
                {
                    new ScaleTransform(), // [0]
                    new SkewTransform(), // [1]
                    new RotateTransform(), // [2]
                    new TranslateTransform() // [3]
                }
            };

            StartTransition();
        }
    }

    /// <summary>
    /// 内容改变时触发动画
    /// </summary>
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        StartTransition();
    }

    /// <summary>
    /// 控件加载时注册可见性变化事件
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var descriptor = DependencyPropertyDescriptor.FromProperty(
            IsVisibleProperty,
            typeof(TransitioningContentControl)
        );
        descriptor?.AddValueChanged(this, OnIsVisibleChanged);
    }

    /// <summary>
    /// 控件卸载时取消注册可见性变化事件
    /// </summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        var descriptor = DependencyPropertyDescriptor.FromProperty(
            IsVisibleProperty,
            typeof(TransitioningContentControl)
        );
        descriptor?.RemoveValueChanged(this, OnIsVisibleChanged);
    }

    /// <summary>
    /// 可见性改变时触发动画
    /// </summary>
    private void OnIsVisibleChanged(object? sender, EventArgs e)
    {
        if (IsVisible)
        {
            StartTransition();
        }
    }

    /// <summary>
    /// 过渡模式改变时重新加载内置动画
    /// </summary>
    private static void OnTransitionModeChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        var control = (TransitioningContentControl)d;
        var newMode = (TransitionMode)e.NewValue;

        if (newMode == TransitionMode.Custom)
        {
            control._storyboardBuildIn = null;
        }
        else
        {
            control._storyboardBuildIn = ResourceHelper.GetResource<Storyboard>(
                $"{newMode}Transition"
            );
        }
    }

    /// <summary>
    /// 启动过渡动画（三级优先级）
    /// </summary>
    private void StartTransition()
    {
        if (_contentPresenter == null || !IsVisible)
            return;

        // 停止之前的动画
        _currentStoryboard?.Stop(_contentPresenter);

        Storyboard? storyboard = null;

        // 优先级 1: 自定义 Storyboard
        if (TransitionStoryboard != null)
        {
            storyboard = TransitionStoryboard;
        }
        // 优先级 2: 模式内置动画
        else if (_storyboardBuildIn != null)
        {
            storyboard = _storyboardBuildIn;
        }
        // 优先级 3: 默认动画
        else
        {
            storyboard = StoryboardBuildInDefault.Value;
        }

        if (storyboard != null)
        {
            _currentStoryboard = storyboard;
            storyboard.Begin(_contentPresenter);
        }
    }
}
