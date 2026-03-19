using System.Windows;

namespace LyuWpfHelper.Helpers;

/// <summary>
/// 资源加载辅助类
/// </summary>
public static class ResourceHelper
{
    /// <summary>
    /// 从应用程序资源中获取指定键的资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="key">资源键</param>
    /// <returns>找到的资源，如果未找到则返回 null</returns>
    public static T? GetResource<T>(string key) where T : class
    {
        return Application.Current?.TryFindResource(key) as T;
    }
}
