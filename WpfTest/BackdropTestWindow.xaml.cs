using LyuWpfHelper.Helpers;
using System.Windows;

namespace WpfTest;

/// <summary>
/// BackdropTestWindow.xaml 的交互逻辑
/// </summary>
public partial class BackdropTestWindow : Window
{
    public BackdropTestWindow()
    {
        InitializeComponent();
    }

    private void SetDefault_Click(object sender, RoutedEventArgs e)
    {
        WindowBackdropHelper.SetBackdrop(this, WindowBackdropType.Default);
        DescriptionText.Text = "Default - 白色背景（无特效）";
    }

    private void SetAcrylic_Click(object sender, RoutedEventArgs e)
    {
        WindowBackdropHelper.SetBackdrop(this, WindowBackdropType.Acrylic);
        DescriptionText.Text = "Acrylic - 亚克力毛玻璃效果，半透明背景带模糊效果（Windows 11+）";
    }

    private void SetMica_Click(object sender, RoutedEventArgs e)
    {
        WindowBackdropHelper.SetBackdrop(this, WindowBackdropType.Mica);
        DescriptionText.Text = "Mica - 云母效果，柔和的半透明背景（Windows 11+）";
    }

    private void SetTabbed_Click(object sender, RoutedEventArgs e)
    {
        WindowBackdropHelper.SetBackdrop(this, WindowBackdropType.Tabbed);
        DescriptionText.Text = "Tabbed - 标签页优化效果，适用于多标签窗口（Windows 11 22H2+）";
    }
}
