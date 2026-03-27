using LyuWpfHelper.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LyuWpfHelper.Extensions
{
    /// <summary>
    /// 服务集合扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加 LyuWpfHelper 服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddLyuNotificationService(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Transient
        )
        {
            services.Add(
                new ServiceDescriptor(
                    typeof(INotificationService),
                    typeof(NotificationService),
                    lifetime
                )
            );
            return services;
        }

        /// <summary>
        /// 添加 LyuWpfHelper Busy 服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddLyuBusyService(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Transient
        )
        {
            services.Add(
                new ServiceDescriptor(typeof(IBusyService), typeof(BusyService), lifetime)
            );
            return services;
        }
    }
}
