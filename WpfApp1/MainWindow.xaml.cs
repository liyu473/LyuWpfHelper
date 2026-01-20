using System.Windows;

namespace WpfApp1
{
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var data = _vm.SelectedItem;
            _vm.Datas.Remove(data);
        }
    }
}