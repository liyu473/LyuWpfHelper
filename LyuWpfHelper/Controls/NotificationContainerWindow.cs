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

        private void OnOwnerWindowClosed(object? sender, EventArgs e)
        {
            Close();
        }

        private void UpdatePosition()
        {
            if (_ownerWindow.WindowState == WindowState.Minimized)
                return;

            // 处理最大化状态：使用主窗口的实际渲染区域
            if (_ownerWindow.WindowState == WindowState.Maximized)
            {
                // 最大化时，使用系统工作区
                var source = PresentationSource.FromVisual(_ownerWindow);
                if (source?.CompositionTarget != null)
                {
                    var screenBounds = SystemParameters.WorkArea;

                    Left = screenBounds.Left;
                    Top = screenBounds.Top;
                    Width = screenBounds.Width;
                    Height = screenBounds.Height;
                }
                else
                {
                    // 备用方案：使用主窗口的实际尺寸
                    Left = 0;
                    Top = 0;
                    Width = _ownerWindow.ActualWidth;
                    Height = _ownerWindow.ActualHeight;
                }
            }
            else
            {
                Left = _ownerWindow.Left;
                Top = _ownerWindow.Top;
                Width = _ownerWindow.ActualWidth;
                Height = _ownerWindow.ActualHeight;
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
                _ownerWindow.Closed -= OnOwnerWindowClosed;
            }
            base.OnClosed(e);
        }
    }
}
