using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LyuWpfHelper.Controls;

public class BusyMaskControl : ContentControl
{
    private Border? _panel;
    private TranslateTransform? _panelOffset;

    static BusyMaskControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(BusyMaskControl),
            new FrameworkPropertyMetadata(typeof(BusyMaskControl))
        );
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(BusyMaskControl),
        new PropertyMetadata("Please wait")
    );

    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message),
        typeof(string),
        typeof(BusyMaskControl),
        new PropertyMetadata("Processing your request...")
    );

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _panel = GetTemplateChild("PART_Panel") as Border;
        _panelOffset = GetTemplateChild("PART_PanelOffset") as TranslateTransform;
    }

    public void PlayShowAnimation()
    {
        if (_panel == null || _panelOffset == null)
        {
            ApplyTemplate();
            _panel = GetTemplateChild("PART_Panel") as Border;
            _panelOffset = GetTemplateChild("PART_PanelOffset") as TranslateTransform;
        }

        if (_panel == null || _panelOffset == null)
        {
            return;
        }

        _panel.BeginAnimation(UIElement.OpacityProperty, null);
        _panelOffset.BeginAnimation(TranslateTransform.YProperty, null);

        _panel.Opacity = 0;
        _panelOffset.Y = 40;

        var fadeAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(360),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var slideAnimation = new DoubleAnimation
        {
            From = 40,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(460),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        _panel.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
        _panelOffset.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
    }
}
