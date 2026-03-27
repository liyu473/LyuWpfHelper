using System.Windows;
using iNKORE.UI.WPF.Modern;
using LyuLogExtension.Builder;
using LyuLogExtension.Extensions;
using LyuWpfHelper.Extensions;
using LyuWpfHelper.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZLogger.Providers;

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
                .ConfigureServices(
                    (context, services) =>
                    {
                        // 配置日志系统
                        services.AddZLogger(builder =>
                            builder
                                .FilterMicrosoft()
                                .FilterSystem()
                                .AddFileOutput("logs/trace/", LogLevel.Trace)
                                .AddFileOutput("logs/info/", LogLevel.Information)
                                .WithRollingInterval(RollingInterval.Day)
                                .WithRollingSizeKB(4096)
                        );

                        // 注册 LyuWpfHelper 服务
                        services.AddLyuNotificationService();
                        services.AddLyuBusyService();

                        // 注册 ViewModel
                        services.AddSingleton<MainViewModel>();

                        // 注册 MainWindow
                        services.AddSingleton<MainWindow>();

                        services.AddTransient<BackdropTestWindow>();
                    }
                )
                .Build();
        }

        /// <summary>
        /// 获取服务实例
        /// </summary>
        public static T GetService<T>() where T : notnull
        {
            return ((App)Current)._host.Services.GetRequiredService<T>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // 在创建任何窗口之前初始化 iNKORE 主题
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;

            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            var logger = _host.Services.GetRequiredService<ILogger<App>>();
            var global = new GlobalExceptionHandler(
                logger,
                ex =>
                {
                    if (Current?.Dispatcher != null)
                    {
                        Current.Dispatcher.BeginInvoke(
                            new Action(() =>
                            {
                                MessageBox.Show(
                                    ex.Message,
                                    "全局异常",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error
                                );
                            })
                        );
                    }
                }
            );
            global.Initialize(); 

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
