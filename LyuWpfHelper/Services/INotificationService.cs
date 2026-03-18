using System.Windows;
using LyuWpfHelper.Controls;
using LyuWpfHelper.Helpers;

namespace LyuWpfHelper.Services
{
    /// <summary>
    /// 通知服务接口
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// 设置通知的所有者窗口
        /// </summary>
        /// <param name="owner">所有者窗口</param>
        void SetOwnerWindow(Window owner);

        /// <summary>
        /// 显示通知
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息内容</param>
        /// <param name="type">通知类型</param>
        /// <param name="position">显示位置</param>
        /// <param name="durationSeconds">显示时长（秒），0表示不自动关闭</param>
        void Show(
            string title,
            string message,
            NotificationType type = NotificationType.Information,
            NotificationPosition position = NotificationPosition.TopRight,
            int durationSeconds = 3
        );
    }
}
