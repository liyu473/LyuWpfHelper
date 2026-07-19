using System.Windows;
using System.Windows.Controls;

namespace LyuWpfHelper.Controls;

[TemplatePart(Name = PartAppName, Type = typeof(TextBlock))]
[TemplatePart(Name = PartAppIcon, Type = typeof(Image))]
[TemplatePart(Name = PartCustomContent, Type = typeof(ContentPresenter))]
public class LyuSplashScreen : Control
{
    private const string PartAppName = "PART_AppName";
    private const string PartAppIcon = "PART_AppIcon";
    private const string PartCustomContent = "PART_CustomContent";

    private TextBlock? _appName;
    private Image? _appIcon;
    private ContentPresenter? _customContent;

    public static readonly DependencyProperty SplashScreenProperty = DependencyProperty.Register(
        nameof(SplashScreen),
        typeof(ILyuApplicationSplashScreen),
        typeof(LyuSplashScreen),
        new PropertyMetadata(null, OnSplashScreenChanged)
    );

    static LyuSplashScreen()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(LyuSplashScreen),
            new FrameworkPropertyMetadata(typeof(LyuSplashScreen))
        );
    }

    public ILyuApplicationSplashScreen? SplashScreen
    {
        get => (ILyuApplicationSplashScreen?)GetValue(SplashScreenProperty);
        set => SetValue(SplashScreenProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _appName = GetTemplateChild(PartAppName) as TextBlock;
        _appIcon = GetTemplateChild(PartAppIcon) as Image;
        _customContent = GetTemplateChild(PartCustomContent) as ContentPresenter;
        UpdateContent();
    }

    private static void OnSplashScreenChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is LyuSplashScreen splashScreen)
        {
            splashScreen.UpdateContent();
        }
    }

    private void UpdateContent()
    {
        if (_appName != null)
        {
            _appName.Visibility = Visibility.Collapsed;
            _appName.Text = null;
        }

        if (_appIcon != null)
        {
            _appIcon.Visibility = Visibility.Collapsed;
            _appIcon.Source = null;
        }

        if (_customContent != null)
        {
            _customContent.Visibility = Visibility.Collapsed;
            _customContent.Content = null;
        }

        if (SplashScreen?.SplashScreenContent is { } customContent && _customContent != null)
        {
            _customContent.Content = customContent;
            _customContent.Visibility = Visibility.Visible;
        }
        else if (SplashScreen?.AppIcon is { } appIcon && _appIcon != null)
        {
            _appIcon.Source = appIcon;
            _appIcon.Visibility = Visibility.Visible;
        }
        else if (SplashScreen?.AppName is { } appName && _appName != null)
        {
            if (!string.IsNullOrWhiteSpace(appName))
            {
                _appName.Text = appName;
                _appName.Visibility = Visibility.Visible;
            }
        }
    }
}
