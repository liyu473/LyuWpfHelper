using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LyuWpfHelper.Controls;

public class NotificationControl : ContentControl
{
    static NotificationControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NotificationControl),
            new FrameworkPropertyMetadata(typeof(NotificationControl)));
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(NotificationControl));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(NotificationControl));

    public static readonly DependencyProperty NotificationTypeProperty =
        DependencyProperty.Register(nameof(NotificationType), typeof(NotificationType), typeof(NotificationControl),
            new PropertyMetadata(NotificationType.Information));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(Geometry), typeof(NotificationControl));

    public static readonly DependencyProperty RemainingProgressProperty =
        DependencyProperty.Register(nameof(RemainingProgress), typeof(double), typeof(NotificationControl),
            new PropertyMetadata(1.0));

    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register(nameof(Duration), typeof(double), typeof(NotificationControl),
            new PropertyMetadata(0.0));

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

    public NotificationType NotificationType
    {
        get => (NotificationType)GetValue(NotificationTypeProperty);
        set => SetValue(NotificationTypeProperty, value);
    }

    public Geometry? Icon
    {
        get => (Geometry?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public double RemainingProgress
    {
        get => (double)GetValue(RemainingProgressProperty);
        set => SetValue(RemainingProgressProperty, value);
    }

    public double Duration
    {
        get => (double)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public event EventHandler? Closed;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("PART_CloseButton") is Button closeButton)
        {
            closeButton.Click += (s, e) => Close();
        }
    }

    internal void Close()
    {
        Closed?.Invoke(this, EventArgs.Empty);
    }
}

public enum NotificationType
{
    Information,
    Success,
    Warning,
    Error
}
