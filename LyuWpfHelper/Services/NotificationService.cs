using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using LyuWpfHelper.Adorners;
using LyuWpfHelper.Controls;
using LyuWpfHelper.Helpers;

namespace LyuWpfHelper.Services
{
    /// <summary>
    /// 通知服务实现，用于显示和管理通知消息
    /// </summary>
    public class NotificationService : INotificationService
    {
        private NotificationAdorner? _adorner;
        private AdornerLayer? _adornerLayer;
        private readonly object _lock = new();
        private readonly Dictionary<NotificationPosition, List<NotificationItem>> _notifications =
        [];
        private const int MaxNotificationsPerPosition = 5;
        private Window? _ownerWindow;

        public NotificationService()
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
        public void SetOwnerWindow(Window owner)
        {
            // Clean up previous owner if changing
            if (_ownerWindow != null && _ownerWindow != owner)
            {
                RemoveAdorner();
                _ownerWindow.Closed -= OnOwnerWindowClosed;
            }

            _ownerWindow = owner;

            // Pre-initialize adorner layer
            if (_ownerWindow != null)
            {
                InitializeAdornerLayer();
            }
        }

        /// <summary>
        /// 显示通知
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息内容</param>
        /// <param name="type">通知类型</param>
        /// <param name="position">显示位置</param>
        /// <param name="durationSeconds">显示时长（秒），0表示不自动关闭</param>
        public void Show(
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
                    if (!EnsureAdorner())
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
                    var panel = _adorner!.GetPanel(position);

                    // 保存现有通知的当前位置
                    var existingChildren = new List<(UIElement child, double currentY)>();
                    foreach (UIElement child in panel.Children)
                    {
                        var transform = child.RenderTransform as System.Windows.Media.TranslateTransform;
                        double currentY = transform?.Y ?? 0;
                        existingChildren.Add((child, currentY));
                    }

                    // 根据位置决定插入位置
                    if (position == NotificationPosition.TopRight || position == NotificationPosition.TopCenter)
                    {
                        // 顶部位置：新通知插入到最上方（index 0）
                        panel.Children.Insert(0, notificationControl);
                        notifications.Insert(0, item);
                    }
                    else
                    {
                        // 底部位置：新通知添加到最下方（末尾）
                        panel.Children.Add(notificationControl);
                        notifications.Insert(0, item); // 列表仍然保持新的在前面，方便管理
                    }

                    // 强制布局更新
                    panel.UpdateLayout();

                    // 获取新通知高度。快速连续添加时，ActualHeight 可能还没稳定，做一次兜底测量。
                    double newNotificationHeight = notificationControl.ActualHeight;
                    if (newNotificationHeight <= 0)
                    {
                        var measureWidth = panel.ActualWidth > 0
                            ? panel.ActualWidth
                            : notificationControl.MaxWidth;
                        notificationControl.Measure(new Size(measureWidth, double.PositiveInfinity));
                        newNotificationHeight = notificationControl.DesiredSize.Height;
                    }

                    var pushDistance = Math.Max(newNotificationHeight, 1) + panel.Spacing;

                    // 动画现有通知
                    if (position == NotificationPosition.TopRight || position == NotificationPosition.TopCenter)
                    {
                        // 顶部：将现有通知临时偏移回原位置，然后向下推
                        foreach (var (child, oldY) in existingChildren)
                        {
                            if (child.RenderTransform is not System.Windows.Media.TranslateTransform)
                            {
                                child.RenderTransform = new System.Windows.Media.TranslateTransform();
                            }
                            var transform = (System.Windows.Media.TranslateTransform)child.RenderTransform;
                            transform.Y = oldY - pushDistance; // 向上偏移，抵消布局向下移动

                            var pushAnimation = new DoubleAnimation
                            {
                                From = oldY - pushDistance,
                                To = 0,
                                Duration = TimeSpan.FromMilliseconds(300),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                            };
                            transform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, pushAnimation);
                        }
                    }
                    else
                    {
                        // 底部：将现有通知临时偏移回原位置，然后向上推
                        foreach (var (child, oldY) in existingChildren)
                        {
                            if (child.RenderTransform is not System.Windows.Media.TranslateTransform)
                            {
                                child.RenderTransform = new System.Windows.Media.TranslateTransform();
                            }
                            var transform = (System.Windows.Media.TranslateTransform)child.RenderTransform;
                            transform.Y = oldY + pushDistance; // 向下偏移，抵消布局向上移动

                            var pushAnimation = new DoubleAnimation
                            {
                                From = oldY + pushDistance,
                                To = 0,
                                Duration = TimeSpan.FromMilliseconds(300),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                            };
                            transform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, pushAnimation);
                        }
                    }

                    // 播放滑入动画
                    PlaySlideInAnimation(notificationControl, position);

                    // 设置自动关闭动画
                    if (durationSeconds > 0)
                    {
                        notificationControl.Duration = durationSeconds;

                        // 使用动画平滑更新进度条
                        var progressAnimation = new DoubleAnimation
                        {
                            From = 1.0,
                            To = 0.0,
                            Duration = TimeSpan.FromSeconds(durationSeconds),
                        };

                        progressAnimation.Completed += (s, e) =>
                        {
                            RemoveNotification(item, true);
                        };

                        notificationControl.BeginAnimation(
                            NotificationControl.RemainingProgressProperty,
                            progressAnimation
                        );

                        // 保存动画引用以便后续可以停止
                        item.Animation = progressAnimation;
                    }
                }
            });
        }

        private void InitializeAdornerLayer()
        {
            if (_ownerWindow?.Content is UIElement content)
            {
                _adornerLayer = AdornerLayer.GetAdornerLayer(content);

                // Fallback: try window itself
                _adornerLayer ??= AdornerLayer.GetAdornerLayer(_ownerWindow);

                // Subscribe to cleanup event
                _ownerWindow.Closed += OnOwnerWindowClosed;
            }

            if (_adornerLayer == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Warning: AdornerLayer not available. Window.Content may not be set."
                );
            }
        }

        private void OnOwnerWindowClosed(object? sender, EventArgs e)
        {
            RemoveAdorner();
            if (_ownerWindow == null)
            {
                return;
            }
            _ownerWindow.Closed -= OnOwnerWindowClosed;
        }

        private bool EnsureAdorner()
        {
            if (_adorner != null)
                return true;

            var owner = _ownerWindow ?? Application.Current?.MainWindow;
            if (owner == null)
                return false;

            if (_adornerLayer == null)
            {
                _ownerWindow = owner;
                InitializeAdornerLayer();
            }

            if (_adornerLayer == null)
            {
                System.Diagnostics.Debug.WriteLine("Warning: AdornerLayer not available.");
                return false;
            }

            if (owner.Content is UIElement content)
            {
                _adorner = new NotificationAdorner(content);
                _adornerLayer.Add(_adorner);
                return true;
            }

            return false;
        }

        private void RemoveNotification(NotificationItem item, bool animate)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    // 停止进度动画并保持当前值
                    if (item.Animation != null)
                    {
                        // 获取当前进度值
                        var currentProgress = item.Control.RemainingProgress;

                        // 停止动画
                        item.Control.BeginAnimation(
                            NotificationControl.RemainingProgressProperty,
                            null
                        );

                        // 恢复当前值，防止跳变
                        item.Control.RemainingProgress = currentProgress;

                        item.Animation = null;
                    }

                    var notifications = _notifications[item.Position];
                    notifications.Remove(item);

                    if (animate)
                    {
                        PlaySlideOutAnimation(
                            item.Control,
                            () =>
                            {
                                if (_adorner != null)
                                {
                                    var panel = _adorner.GetPanel(item.Position);
                                    panel.Children.Remove(item.Control);
                                    CheckAndRemoveAdorner();
                                }
                            }
                        );
                    }
                    else
                    {
                        if (_adorner != null)
                        {
                            var panel = _adorner.GetPanel(item.Position);
                            panel.Children.Remove(item.Control);
                            CheckAndRemoveAdorner();
                        }
                    }
                }
            });
        }

        private void CheckAndRemoveAdorner()
        {
            if (_adorner != null && _adorner.IsEmpty())
            {
                RemoveAdorner();
            }
        }

        private void RemoveAdorner()
        {
            if (_adorner != null && _adornerLayer != null)
            {
                _adornerLayer.Remove(_adorner);
                _adorner = null;
            }
        }

        private void PlaySlideInAnimation(
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

        private void PlaySlideOutAnimation(NotificationControl control, Action onCompleted)
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
            public DoubleAnimation? Animation { get; set; }
        }
    }
}
