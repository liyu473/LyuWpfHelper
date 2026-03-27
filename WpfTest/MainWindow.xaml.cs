using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using iNKORE.UI.WPF.Modern;
using LyuWpfHelper.Controls;
using LyuWpfHelper.Extensions;
using LyuWpfHelper.Helpers;
using LyuWpfHelper.Panels;
using LyuWpfHelper.Services;

namespace WpfTest;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : LyuWindow
{
    private readonly MainViewModel _vm;
    private readonly INotificationService _notificationService;
    private readonly IBusyService _busyService;

    public MainWindow(
        MainViewModel viewModel,
        INotificationService notificationService,
        IBusyService busyService
    )
    {
        InitializeComponent();
        _vm = viewModel;
        _notificationService = notificationService;
        _busyService = busyService;
        DataContext = _vm;
    }

    private void TitleBarRefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "标题栏按钮",
            "已点击标题栏中的自定义按钮。",
            NotificationType.Information,
            NotificationPosition.TopRight,
            3
        );
    }

    private void OpenBackdropTest_Click(object sender, RoutedEventArgs e)
    {
        var backdropWindow = App.GetService<BackdropTestWindow>();
        backdropWindow.Show();
    }

    private void SetMainDefault_Click(object sender, RoutedEventArgs e)
    {
        WindowBackdropHelper.SetBackdrop(this, WindowBackdropType.Default);
    }

    private void SetMainAcrylic_Click(object sender, RoutedEventArgs e)
    {
        WindowBackdropHelper.SetBackdrop(this, WindowBackdropType.Acrylic);
    }

    private void SetMainMica_Click(object sender, RoutedEventArgs e)
    {
        WindowBackdropHelper.SetBackdrop(this, WindowBackdropType.Mica);
    }

    private void SetMainTabbed_Click(object sender, RoutedEventArgs e)
    {
        WindowBackdropHelper.SetBackdrop(this, WindowBackdropType.Tabbed);
    }

    private void ToggleFullScreenButton_Click(object sender, RoutedEventArgs e)
    {
        LyuWindowHelper.ToggleFullScreen(this);
    }

    // 右上角通知
    private void ShowTopRightInfo_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "信息",
            "这是一条信息通知",
            NotificationType.Information,
            NotificationPosition.TopRight,
            3
        );
    }

    private void ShowTopRightSuccess_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "成功",
            "操作已成功完成",
            NotificationType.Success,
            NotificationPosition.TopRight,
            3
        );
    }

    private void ShowTopRightWarning_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "警告",
            "请注意磁盘空间不足",
            NotificationType.Warning,
            NotificationPosition.TopRight,
            0
        );
    }

    private void ShowTopRightError_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "错误",
            "无法连接到服务器",
            NotificationType.Error,
            NotificationPosition.TopRight,
            10
        );
    }

    // 右下角通知
    private void ShowBottomRightInfo_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "信息",
            "这是一条信息通知",
            NotificationType.Information,
            NotificationPosition.BottomRight,
            3
        );
    }

    private void ShowBottomRightSuccess_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "成功",
            "文件保存成功",
            NotificationType.Success,
            NotificationPosition.BottomRight,
            3
        );
    }

    private void ShowBottomRightWarning_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "警告",
            "网络连接不稳定",
            NotificationType.Warning,
            NotificationPosition.BottomRight,
            5
        );
    }

    private void ShowBottomRightError_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "错误",
            "数据加载失败",
            NotificationType.Error,
            NotificationPosition.BottomRight,
            10
        );
    }

    // 中上通知
    private void ShowTopCenterInfo_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "信息",
            "这是一条信息通知",
            NotificationType.Information,
            NotificationPosition.TopCenter,
            3
        );
    }

    private void ShowTopCenterSuccess_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "成功",
            "更新已安装",
            NotificationType.Success,
            NotificationPosition.TopCenter,
            3
        );
    }

    private void ShowTopCenterWarning_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "警告",
            "即将进行系统维护",
            NotificationType.Warning,
            NotificationPosition.TopCenter,
            5
        );
    }

    private void ShowTopCenterError_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "错误",
            "认证失败，请重新登录",
            NotificationType.Error,
            NotificationPosition.TopCenter,
            10
        );
    }

    // 中下通知
    private void ShowBottomCenterInfo_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "信息",
            "这是一条信息通知",
            NotificationType.Information,
            NotificationPosition.BottomCenter,
            3
        );
    }

    private void ShowBottomCenterSuccess_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "成功",
            "同步完成",
            NotificationType.Success,
            NotificationPosition.BottomCenter,
            3
        );
    }

    private void ShowBottomCenterWarning_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "警告",
            "电池电量低",
            NotificationType.Warning,
            NotificationPosition.BottomCenter,
            5
        );
    }

    private void ShowBottomCenterError_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "错误",
            "打印机离线",
            NotificationType.Error,
            NotificationPosition.BottomCenter,
            10
        );
    }

    // 批量测试
    private void ShowMultipleNotifications_Click(object sender, RoutedEventArgs e)
    {
        for (int i = 1; i <= 7; i++)
        {
            NotificationType type = (i % 4) switch
            {
                0 => NotificationType.Error,
                1 => NotificationType.Information,
                2 => NotificationType.Success,
                _ => NotificationType.Warning,
            };
            _notificationService.Show(
                $"通知 {i}",
                $"这是第 {i} 条通知，测试最多显示 5 条",
                type,
                NotificationPosition.TopRight,
                5
            );
        }
    }

    private void ShowAllPositions_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show(
            "右上",
            "右上角通知",
            NotificationType.Information,
            NotificationPosition.TopRight,
            5
        );
        _notificationService.Show(
            "右下",
            "右下角通知",
            NotificationType.Success,
            NotificationPosition.BottomRight,
            5
        );
        _notificationService.Show(
            "中上",
            "中上通知",
            NotificationType.Warning,
            NotificationPosition.TopCenter,
            5
        );
        _notificationService.Show(
            "中下",
            "中下通知",
            NotificationType.Error,
            NotificationPosition.BottomCenter,
            5
        );
    }

    // TransitioningContentControl 测试
    private void SetTransitionMode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string modeString)
        {
            if (Enum.TryParse<TransitionMode>(modeString, out var mode))
            {
                TransitionControl.TransitionMode = mode;
            }
        }
    }

    private void ChangeContent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string contentNumber)
        {
            Border newContent = contentNumber switch
            {
                "1" => new Border
                {
                    Padding = new Thickness(40, 30, 40, 30),
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#4CAF50")!
                    ),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(8),
                    Child = new SimpleStackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Vertical,
                        Spacing = 10,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "内容 1",
                                FontSize = 24,
                                FontWeight = FontWeights.Bold,
                                Foreground = new SolidColorBrush(
                                    (Color)ColorConverter.ConvertFromString("#4CAF50")!
                                ),
                            },
                            new TextBlock
                            {
                                Text = "这是第一个内容",
                                FontSize = 14,
                                Foreground = new SolidColorBrush(
                                    (Color)ColorConverter.ConvertFromString("#757575")!
                                ),
                            },
                        },
                    },
                },
                "2" => new Border
                {
                    Padding = new Thickness(40, 30, 40, 30),
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#FF9800")!
                    ),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(8),
                    Child = new SimpleStackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Vertical,
                        Spacing = 10,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "内容 2",
                                FontSize = 24,
                                FontWeight = FontWeights.Bold,
                                Foreground = new SolidColorBrush(
                                    (Color)ColorConverter.ConvertFromString("#FF9800")!
                                ),
                            },
                            new TextBlock
                            {
                                Text = "这是第二个内容",
                                FontSize = 14,
                                Foreground = new SolidColorBrush(
                                    (Color)ColorConverter.ConvertFromString("#757575")!
                                ),
                            },
                        },
                    },
                },
                "3" => new Border
                {
                    Padding = new Thickness(40, 30, 40, 30),
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#F44336")!
                    ),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(8),
                    Child = new SimpleStackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Vertical,
                        Spacing = 10,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "内容 3",
                                FontSize = 24,
                                FontWeight = FontWeights.Bold,
                                Foreground = new SolidColorBrush(
                                    (Color)ColorConverter.ConvertFromString("#F44336")!
                                ),
                            },
                            new TextBlock
                            {
                                Text = "这是第三个内容",
                                FontSize = 14,
                                Foreground = new SolidColorBrush(
                                    (Color)ColorConverter.ConvertFromString("#757575")!
                                ),
                            },
                        },
                    },
                },
                _ => throw new ArgumentException("Invalid content number"),
            };

            TransitionControl.Content = newContent;
        }
    }

    private void MainThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (
            sender is not ComboBox comboBox
            || comboBox.SelectedItem is not ComboBoxItem selectedItem
        )
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
        }
    }

    private void OpenDemoDrawer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag)
        {
            return;
        }

        GetDemoDrawerByTag(tag)?.Open();
    }

    private Drawer? GetDemoDrawerByTag(string tag)
    {
        return tag switch
        {
            "Left" => LeftDemoDrawer,
            "Right" => RightDemoDrawer,
            "Top" => TopDemoDrawer,
            "Bottom" => BottomDemoDrawer,
            _ => null,
        };
    }

    protected override void OnThemeChanged(LyuWindowThemeChangedEventArgs e)
    {
        base.OnThemeChanged(e);
        var elementTheme =
            e.EffectiveTheme == WindowThemeMode.Dark ? ElementTheme.Dark : ElementTheme.Light;

        ThemeManager.SetRequestedTheme(this, elementTheme);
    }

    private void ShowDefaultBusy_Click(object sender, RoutedEventArgs e)
    {
        _busyService.Show("正在加载数据，请稍候...", timeout: 5000);
    }

    private void ShowCustomBusy_Click(object sender, RoutedEventArgs e)
    {
        var customContent = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        customContent.Children.Add(
            new TextBlock
            {
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = "正在同步数据...",
            }
        );
        customContent.Children.Add(
            new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = "请稍候片刻",
            }
        );
        customContent.Children.Add(
            new ProgressBar
            {
                Width = 240,
                Height = 8,
                Margin = new Thickness(0, 14, 0, 0),
                IsIndeterminate = true,
            }
        );

        _busyService.ShowWithContent(customContent, timeout: 2000);
    }

    private void ThrowError_Click(object sender, RoutedEventArgs e)
    {
        throw new Exception("这是一个测试异常");
    }

    private async void ShowTaskBusy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _busyService.RunWithBusyAsync(
                async (ct) =>
                {
                    await Task.Delay(5000, ct);
                },
                timeout: TimeSpan.FromSeconds(2),
                onTimeout: ts => _notificationService.ShowWarning($"超时：{ts.TotalSeconds}s")
            );
        }
        catch (TimeoutException) { }
        catch (Exception ex)
        {
            _notificationService.ShowError($"任务执行失败: {ex.Message}");
        }
    }

    private async void ShowTaskBusywithoutTimeout_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _busyService.RunWithBusyAsync(async () =>
            {
                await Task.Delay(5000);
            });
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"任务执行失败: {ex.Message}");
        }
    }
}
