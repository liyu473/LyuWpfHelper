using LyuWpfHelper.Controls;
using LyuWpfHelper.Helpers;
using LyuWpfHelper.Services;
using System.Windows;

namespace WpfTest;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly INotificationService _notificationService;

    public MainWindow(MainViewModel viewModel, INotificationService notificationService)
    {
        InitializeComponent();
        _vm = viewModel;
        _notificationService = notificationService;
        DataContext = _vm;

        _notificationService.SetOwnerWindow(this);
    }

    private void ToggleFullScreenButton_Click(object sender, RoutedEventArgs e)
    {
        LyuWindowHelper.ToggleFullScreen(this);
    }

    // 右上角通知
    private void ShowTopRightInfo_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("信息", "这是一条信息通知", NotificationType.Information, NotificationPosition.TopRight, 3);
    }

    private void ShowTopRightSuccess_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("成功", "操作已成功完成", NotificationType.Success, NotificationPosition.TopRight, 3);
    }

    private void ShowTopRightWarning_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("警告", "请注意磁盘空间不足", NotificationType.Warning, NotificationPosition.TopRight, 0);
    }

    private void ShowTopRightError_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("错误", "无法连接到服务器", NotificationType.Error, NotificationPosition.TopRight, 10);
    }

    // 右下角通知
    private void ShowBottomRightInfo_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("信息", "这是一条信息通知", NotificationType.Information, NotificationPosition.BottomRight, 3);
    }

    private void ShowBottomRightSuccess_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("成功", "文件保存成功", NotificationType.Success, NotificationPosition.BottomRight, 3);
    }

    private void ShowBottomRightWarning_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("警告", "网络连接不稳定", NotificationType.Warning, NotificationPosition.BottomRight, 5);
    }

    private void ShowBottomRightError_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("错误", "数据加载失败", NotificationType.Error, NotificationPosition.BottomRight, 10);
    }

    // 中上通知
    private void ShowTopCenterInfo_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("信息", "这是一条信息通知", NotificationType.Information, NotificationPosition.TopCenter, 3);
    }

    private void ShowTopCenterSuccess_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("成功", "更新已安装", NotificationType.Success, NotificationPosition.TopCenter, 3);
    }

    private void ShowTopCenterWarning_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("警告", "即将进行系统维护", NotificationType.Warning, NotificationPosition.TopCenter, 5);
    }

    private void ShowTopCenterError_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("错误", "认证失败，请重新登录", NotificationType.Error, NotificationPosition.TopCenter, 10);
    }

    // 中下通知
    private void ShowBottomCenterInfo_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("信息", "这是一条信息通知", NotificationType.Information, NotificationPosition.BottomCenter, 3);
    }

    private void ShowBottomCenterSuccess_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("成功", "同步完成", NotificationType.Success, NotificationPosition.BottomCenter, 3);
    }

    private void ShowBottomCenterWarning_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("警告", "电池电量低", NotificationType.Warning, NotificationPosition.BottomCenter, 5);
    }

    private void ShowBottomCenterError_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("错误", "打印机离线", NotificationType.Error, NotificationPosition.BottomCenter, 10);
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
                _ => NotificationType.Warning
            };
            _notificationService.Show($"通知 {i}", $"这是第 {i} 条通知，测试最多显示5个", type, NotificationPosition.TopRight, 5);
        }
    }

    private void ShowAllPositions_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.Show("右上", "右上角通知", NotificationType.Information, NotificationPosition.TopRight, 5);
        _notificationService.Show("右下", "右下角通知", NotificationType.Success, NotificationPosition.BottomRight, 5);
        _notificationService.Show("中上", "中上通知", NotificationType.Warning, NotificationPosition.TopCenter, 5);
        _notificationService.Show("中下", "中下通知", NotificationType.Error, NotificationPosition.BottomCenter, 5);
    }
}