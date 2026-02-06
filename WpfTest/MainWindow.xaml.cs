using System.Windows;
using LyuWpfHelper.Helpers;

namespace WpfTest;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    public MainWindow()
    {
        InitializeComponent();
        _vm = new();
        DataContext = _vm;
    }

    private void ToggleFullScreenButton_Click(object sender, RoutedEventArgs e)
    {
        LyuWindowHelper.ToggleFullScreen(this);
    }
}