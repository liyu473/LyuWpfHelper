using LyuWpfHelper.Helpers;
using iNKORE.UI.WPF.Modern;
using System.Windows;
using System.Windows.Controls;

namespace WpfTest;

/// <summary>
/// Interaction logic for BackdropTestWindow.xaml
/// </summary>
public partial class BackdropTestWindow : Window
{
    public BackdropTestWindow()
    {
        InitializeComponent();
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
            WindowThemeHelper.ApplyTheme(this, themeMode);
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
}
