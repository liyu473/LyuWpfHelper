using System.Windows;
using System.Windows.Controls;

namespace LyuWpfHelper.Controls
{
    /// <summary>
    /// 通知容器窗口，用于显示通知消息
    /// </summary>
    internal class NotificationContainerWindow : Window
    {
        private readonly StackPanel _topRightPanel;
        private readonly StackPanel _bottomRightPanel;
        private readonly StackPanel _topCenterPanel;
        private readonly StackPanel _bottomCenterPanel;

        public NotificationContainerWindow()
        {
            // 窗口配置
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;

            // 创建根容器
            var rootGrid = new Grid
            {
                Background = System.Windows.Media.Brushes.Transparent
            };

            // 右上角容器
            _topRightPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 20, 0),
                IsHitTestVisible = true
            };

            // 右下角容器
            _bottomRightPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 20, 20),
                IsHitTestVisible = true
            };

            // 中上容器
            _topCenterPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0),
                IsHitTestVisible = true
            };

            // 中下容器
            _bottomCenterPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                IsHitTestVisible = true
            };

            rootGrid.Children.Add(_topRightPanel);
            rootGrid.Children.Add(_bottomRightPanel);
            rootGrid.Children.Add(_topCenterPanel);
            rootGrid.Children.Add(_bottomCenterPanel);

            Content = rootGrid;
        }

        /// <summary>
        /// 获取指定位置的容器面板
        /// </summary>
        public StackPanel GetPanel(Helpers.NotificationPosition position)
        {
            return position switch
            {
                Helpers.NotificationPosition.TopRight => _topRightPanel,
                Helpers.NotificationPosition.BottomRight => _bottomRightPanel,
                Helpers.NotificationPosition.TopCenter => _topCenterPanel,
                Helpers.NotificationPosition.BottomCenter => _bottomCenterPanel,
                _ => _topRightPanel
            };
        }

        /// <summary>
        /// 检查是否所有容器都为空
        /// </summary>
        public bool IsEmpty()
        {
            return _topRightPanel.Children.Count == 0 &&
                   _bottomRightPanel.Children.Count == 0 &&
                   _topCenterPanel.Children.Count == 0 &&
                   _bottomCenterPanel.Children.Count == 0;
        }
    }
}
