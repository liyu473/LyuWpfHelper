using iNKORE.UI.WPF.Modern;
using LyuWpfHelper.Controls;
using LyuWpfHelper.Extensions;
using LyuWpfHelper.Helpers;
using LyuWpfHelper.Services;
using System.Windows;
using System.Windows.Controls;

namespace WpfTest;

/// <summary>
/// Interaction logic for BackdropTestWindow.xaml
/// </summary>
public partial class BackdropTestWindow : Window
{
    private readonly INotificationService _notificationService;
    private readonly IBusyService busyService;
    public BackdropTestWindow(INotificationService notificationService,IBusyService busy)
    {
        InitializeComponent();
        _notificationService = notificationService;
        busyService = busy;

        _notificationService.SetOwnerWindow(this);
        busyService.SetOwnerWindow(this);
        Loaded += (_, _) => SyncRequestedTheme();
    }

    private void SetDefault_Click(object sender, RoutedEventArgs e)
    {
        WindowBackdropHelper.SetBackdrop(this, WindowBackdropType.Default);
        SyncRequestedTheme();
        DescriptionText.Text = "Default - solid background (no backdrop effect).";
    }

    private void SetAcrylic_Click(object sender, RoutedEventArgs e)
    {
        WindowBackdropHelper.SetBackdrop(this, WindowBackdropType.Acrylic);
        SyncRequestedTheme();
        DescriptionText.Text = "Acrylic - translucent backdrop with blur (Windows 11+).";
    }

    private void SetMica_Click(object sender, RoutedEventArgs e)
    {
        WindowBackdropHelper.SetBackdrop(this, WindowBackdropType.Mica);
        SyncRequestedTheme();
        DescriptionText.Text = "Mica - subtle material backdrop (Windows 11+).";
    }

    private void SetTabbed_Click(object sender, RoutedEventArgs e)
    {
        WindowBackdropHelper.SetBackdrop(this, WindowBackdropType.Tabbed);
        SyncRequestedTheme();
        DescriptionText.Text = "Tabbed - material optimized for tabbed windows (Windows 11 22H2+).";
    }

    private void BackdropThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox || comboBox.SelectedItem is not ComboBoxItem selectedItem)
        {
            return;
        }

        if (selectedItem.Tag is not string themeTag)
        {
            return;
        }

        if (Enum.TryParse(themeTag, true, out WindowThemeMode themeMode))
        {
            WindowThemeHelper.SetTheme(this, themeMode);
            SyncRequestedTheme();
        }
    }

    private void SyncRequestedTheme()
    {
        WindowThemeMode currentTheme = WindowThemeHelper.GetCurrentTheme(this);
        WindowThemeMode effectiveTheme = WindowThemeHelper.GetEffectiveTheme(currentTheme);
        ElementTheme elementTheme = effectiveTheme == WindowThemeMode.Dark
            ? ElementTheme.Dark
            : ElementTheme.Light;

        if (Content is FrameworkElement root)
        {
            ThemeManager.SetRequestedTheme(root, elementTheme);
        }
        else
        {
            ThemeManager.SetRequestedTheme(this, elementTheme);
        }
    }

    private void Drawer_Click(object sender, RoutedEventArgs e)
    {
        TestDrawer.IsOpen = !TestDrawer.IsOpen;
    }

    private void Busy_Click(object sender, RoutedEventArgs e)
    {
        busyService.Show("正在加载数据，请稍候...", timeout: 3000);
    }

    private void Noticification_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
           "警告",
           "电池电量低",
           NotificationType.Warning,
           NotificationPosition.BottomCenter,
           5
       );
    }
}
