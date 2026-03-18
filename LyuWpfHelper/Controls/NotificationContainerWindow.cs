using System;
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
        private readonly Window _ownerWindow;

        public NotificationContainerWindow(Window ownerWindow)
        {
            _ownerWindow = ownerWindow ?? throw new ArgumentNullException(nameof(ownerWindow));

            // 窗口配置
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;
            ResizeMode = ResizeMode.NoResize;

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

            // 订阅主窗口事件
            _ownerWindow.LocationChanged += OnOwnerWindowLocationChanged;
            _ownerWindow.SizeChanged += OnOwnerWindowSizeChanged;
            _ownerWindow.StateChanged += OnOwnerWindowStateChanged;
            _ownerWindow.Activated += OnOwnerWindowActivated;
            _ownerWindow.Closed += OnOwnerWindowClosed;

            // 初始化位置
            Loaded += (s, e) => UpdatePosition();
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

        private void OnOwnerWindowLocationChanged(object sender, EventArgs e)
        {
            UpdatePosition();
        }

        private void OnOwnerWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePosition();
        }

        private void OnOwnerWindowStateChanged(object sender, EventArgs e)
        {
            if (_ownerWindow.WindowState == WindowState.Minimized)
            {
                Visibility = Visibility.Collapsed;
            }
            else
            {
                Visibility = Visibility.Visible;
                UpdatePosition();
            }
        }

        private void OnOwnerWindowActivated(object? sender, EventArgs e)
        {
            // 全屏时，主窗口被激活会覆盖通知窗口，需要重新刷新 Z-order
            bool isFullScreen = Helpers.LyuWindowHelper.GetIsFullScreen(_ownerWindow);
            if (isFullScreen)
            {
                Topmost = false;
                Topmost = true;
            }
        }

        private void OnOwnerWindowClosed(object? sender, EventArgs e)
        {
            Close();
        }

        private void UpdatePosition()
        {
            if (_ownerWindow.WindowState == WindowState.Minimized)
                return;

            // 检查是否全屏
            bool isFullScreen = Helpers.LyuWindowHelper.GetIsFullScreen(_ownerWindow);

            if (isFullScreen)
            {
                // 全屏状态：覆盖整个窗口，无标题栏
                Left = _ownerWindow.Left;
                Top = _ownerWindow.Top;
                Width = _ownerWindow.ActualWidth;
                Height = _ownerWindow.ActualHeight;

                // 强制通知窗口置于最顶层（解决与全屏主窗口的 Z-order 冲突）
                Topmost = false;
                Topmost = true;
            }
            else if (_ownerWindow.WindowState == WindowState.Maximized)
            {
                // 最大化状态：使用工作区，排除标题栏
                var source = PresentationSource.FromVisual(_ownerWindow);
                if (source?.CompositionTarget != null)
                {
                    var screenBounds = SystemParameters.WorkArea;
                    double captionHeight = SystemParameters.WindowCaptionHeight;

                    Left = screenBounds.Left;
                    Top = screenBounds.Top + captionHeight;  // 排除标题栏
                    Width = screenBounds.Width;
                    Height = screenBounds.Height - captionHeight;
                }
                else
                {
                    // 备用方案
                    double captionHeight = SystemParameters.WindowCaptionHeight;
                    Left = 0;
                    Top = captionHeight;
                    Width = _ownerWindow.ActualWidth;
                    Height = _ownerWindow.ActualHeight - captionHeight;
                }
            }
            else
            {
                // 普通窗口状态：排除标题栏区域
                double captionHeight = SystemParameters.WindowCaptionHeight;

                Left = _ownerWindow.Left;
                Top = _ownerWindow.Top + captionHeight;  // 向下偏移标题栏高度
                Width = _ownerWindow.ActualWidth;
                Height = _ownerWindow.ActualHeight - captionHeight;  // 减去标题栏高度
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // 清理事件订阅
            if (_ownerWindow != null)
            {
                _ownerWindow.LocationChanged -= OnOwnerWindowLocationChanged;
                _ownerWindow.SizeChanged -= OnOwnerWindowSizeChanged;
                _ownerWindow.StateChanged -= OnOwnerWindowStateChanged;
                _ownerWindow.Activated -= OnOwnerWindowActivated;
                _ownerWindow.Closed -= OnOwnerWindowClosed;
            }
            base.OnClosed(e);
        }
    }
}
