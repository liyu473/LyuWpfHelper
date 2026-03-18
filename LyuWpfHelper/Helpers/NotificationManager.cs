using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LyuWpfHelper.Controls;

namespace LyuWpfHelper.Helpers
{
    /// <summary>
    /// 通知管理器，用于显示和管理通知消息
    /// </summary>
    public static class NotificationManager
    {
        private static NotificationContainerWindow? _containerWindow;
        private static readonly object _lock = new();
        private static readonly Dictionary<
            NotificationPosition,
            List<NotificationItem>
        > _notifications = [];
        private const int MaxNotificationsPerPosition = 5;
        private static Window? _ownerWindow;

        static NotificationManager()
        {
            _notifications[NotificationPosition.TopRight] = [];
            _notifications[NotificationPosition.BottomRight] = [];
            _notifications[NotificationPosition.TopCenter] = [];
            _notifications[NotificationPosition.BottomCenter] = [];
        }

        /// <summary>
        /// 设置通知的所有者窗口
        /// </summary>
        /// <param name="owner">所有者窗口</param>
        public static void SetOwnerWindow(Window owner)
        {
            _ownerWindow = owner;
        }

        /// <summary>
        /// 显示通知
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息内容</param>
        /// <param name="type">通知类型</param>
        /// <param name="position">显示位置</param>
        /// <param name="durationSeconds">显示时长（秒），0表示不自动关闭</param>
        public static void Show(
            string title,
            string message,
            NotificationType type = NotificationType.Information,
            NotificationPosition position = NotificationPosition.TopRight,
            int durationSeconds = 3
        )
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    if (!EnsureContainerWindow())
                    {
                        return;
                    }

                    var notifications = _notifications[position];

                    // 如果达到最大数量，移除最旧的
                    if (notifications.Count >= MaxNotificationsPerPosition)
                    {
                        RemoveNotification(notifications[^1], false);
                    }

                    // 创建通知控件
                    var notificationControl = new NotificationControl
                    {
                        Title = title,
                        Message = message,
                        NotificationType = type,
                        Margin = new Thickness(0, 0, 0, 10),
                        IsHitTestVisible = true,
                    };

                    // 创建通知项
                    var item = new NotificationItem
                    {
                        Control = notificationControl,
                        Position = position,
                    };

                    // 设置关闭事件
                    notificationControl.Closed += (s, e) => RemoveNotification(item, true);

                    // 添加到容器
                    var panel = _containerWindow!.GetPanel(position);
                    panel.Children.Insert(0, notificationControl);
                    notifications.Insert(0, item);

                    // 播放滑入动画
                    PlaySlideInAnimation(notificationControl, position);

                    // 设置自动关闭计时器
                    if (durationSeconds > 0)
                    {
                        item.Timer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromSeconds(durationSeconds),
                        };
                        item.Timer.Tick += (s, e) =>
                        {
                            item.Timer.Stop();
                            RemoveNotification(item, true);
                        };
                        item.Timer.Start();
                    }
                }
            });
        }

        private static bool EnsureContainerWindow()
        {
            if (_containerWindow == null)
            {
                var owner = _ownerWindow ?? Application.Current?.MainWindow;
                if (owner == null)
                {
                    return false;
                }

                _containerWindow = new NotificationContainerWindow(owner);
                _containerWindow.Show();
            }
            return true;
        }

        private static void RemoveNotification(NotificationItem item, bool animate)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    // 停止计时器
                    item.Timer?.Stop();
                    item.Timer = null;

                    var notifications = _notifications[item.Position];
                    notifications.Remove(item);

                    if (animate)
                    {
                        PlaySlideOutAnimation(
                            item.Control,
                            () =>
                            {
                                var panel = _containerWindow!.GetPanel(item.Position);
                                panel.Children.Remove(item.Control);
                                CheckAndCloseContainer();
                            }
                        );
                    }
                    else
                    {
                        var panel = _containerWindow!.GetPanel(item.Position);
                        panel.Children.Remove(item.Control);
                        CheckAndCloseContainer();
                    }
                }
            });
        }

        private static void CheckAndCloseContainer()
        {
            if (_containerWindow != null && _containerWindow.IsEmpty())
            {
                _containerWindow.Close();
                _containerWindow = null;
            }
        }

        private static void PlaySlideInAnimation(
            NotificationControl control,
            NotificationPosition position
        )
        {
            control.RenderTransform = new System.Windows.Media.TranslateTransform();

            var slideAnimation = new DoubleAnimation
            {
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            };

            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
            };

            // 根据位置设置滑入方向
            if (
                position == NotificationPosition.TopRight
                || position == NotificationPosition.TopCenter
            )
            {
                slideAnimation.From = -100;
                slideAnimation.To = 0;
            }
            else
            {
                slideAnimation.From = 100;
                slideAnimation.To = 0;
            }

            control.RenderTransform.BeginAnimation(
                System.Windows.Media.TranslateTransform.YProperty,
                slideAnimation
            );
            control.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
        }

        private static void PlaySlideOutAnimation(NotificationControl control, Action onCompleted)
        {
            var fadeAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
            };

            var heightAnimation = new DoubleAnimation
            {
                From = control.ActualHeight,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
            };

            fadeAnimation.Completed += (s, e) => onCompleted?.Invoke();

            control.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
            control.BeginAnimation(FrameworkElement.HeightProperty, heightAnimation);
        }

        private class NotificationItem
        {
            public NotificationControl Control { get; set; } = null!;
            public NotificationPosition Position { get; set; }
            public DispatcherTimer? Timer { get; set; }
        }
    }
}
