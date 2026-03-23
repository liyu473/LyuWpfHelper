using LyuWpfHelper.Extensions;
using iNKORE.UI.WPF.Modern;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace WpfTest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // 注册 LyuWpfHelper 服务
                    services.AddLyuNotificationService();

                    // 注册 ViewModel
                    services.AddSingleton<MainViewModel>();

                    // 注册 MainWindow
                    services.AddSingleton<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // 在创建任何窗口之前初始化 iNKORE 主题
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;

            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();

            base.OnExit(e);
        }
    }

}
