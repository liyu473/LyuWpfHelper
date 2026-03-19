using LyuWpfHelper.Controls;
using LyuWpfHelper.Helpers;
using LyuWpfHelper.Services;

namespace LyuWpfHelper.Extensions
{
    /// <summary>
    /// 通知服务扩展方法
    /// </summary>
    public static class NotificationExtensions
    {
        /// <summary>
        /// 显示错误通知
        /// </summary>
        /// <param name="service">通知服务实例</param>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题，默认为"错误"</param>
        /// <param name="position">显示位置</param>
        /// <param name="durationSeconds">显示时长（秒），0表示不自动关闭，默认5秒</param>
        public static void ShowError(
            this INotificationService service,
            string message,
            string title = "错误",
            NotificationPosition position = NotificationPosition.TopRight,
            int durationSeconds = 5
        )
        {
            service.Show(title, message, NotificationType.Error, position, durationSeconds);
        }

        /// <summary>
        /// 显示提示通知
        /// </summary>
        /// <param name="service">通知服务实例</param>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题，默认为"提示"</param>
        /// <param name="position">显示位置</param>
        /// <param name="durationSeconds">显示时长（秒），0表示不自动关闭，默认3秒</param>
        public static void ShowInformation(
            this INotificationService service,
            string message,
            string title = "提示",
            NotificationPosition position = NotificationPosition.TopRight,
            int durationSeconds = 3
        )
        {
            service.Show(title, message, NotificationType.Information, position, durationSeconds);
        }

        /// <summary>
        /// 显示成功通知
        /// </summary>
        /// <param name="service">通知服务实例</param>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题，默认为"成功"</param>
        /// <param name="position">显示位置</param>
        /// <param name="durationSeconds">显示时长（秒），0表示不自动关闭，默认3秒</param>
        public static void ShowSuccess(
            this INotificationService service,
            string message,
            string title = "成功",
            NotificationPosition position = NotificationPosition.TopRight,
            int durationSeconds = 3
        )
        {
            service.Show(title, message, NotificationType.Success, position, durationSeconds);
        }

        /// <summary>
        /// 显示警告通知
        /// </summary>
        /// <param name="service">通知服务实例</param>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题，默认为"警告"</param>
        /// <param name="position">显示位置</param>
        /// <param name="durationSeconds">显示时长（秒），0表示不自动关闭，默认4秒</param>
        public static void ShowWarning(
            this INotificationService service,
            string message,
            string title = "警告",
            NotificationPosition position = NotificationPosition.TopRight,
            int durationSeconds = 4
        )
        {
            service.Show(title, message, NotificationType.Warning, position, durationSeconds);
        }
    }
}
