using System.IO;
using System.Windows.Media.Imaging;

namespace LyuWpfHelper.Helpers;

/// <summary>
/// 字节数组扩展方法
/// </summary>
public static class ByteArrayExtensions
{
    /// <summary>
    /// 将字节数组转换为 BitmapSource
    /// </summary>
    /// <param name="bytes">图像字节数组</param>
    /// <returns>BitmapSource 对象，如果转换失败则返回 null</returns>
    public static BitmapSource? ToBitmapSource(this byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return null;

        try
        {
            using var stream = new MemoryStream(bytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze(); // 冻结以提高性能并允许跨线程访问
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 将字节数组转换为 BitmapSource，支持指定解码宽度和高度
    /// </summary>
    /// <param name="bytes">图像字节数组</param>
    /// <param name="decodePixelWidth">解码宽度（0 表示使用原始宽度）</param>
    /// <param name="decodePixelHeight">解码高度（0 表示使用原始高度）</param>
    /// <returns>BitmapSource 对象，如果转换失败则返回 null</returns>
    public static BitmapSource? ToBitmapSource(this byte[] bytes, int decodePixelWidth, int decodePixelHeight = 0)
    {
        if (bytes == null || bytes.Length == 0)
            return null;

        try
        {
            using var stream = new MemoryStream(bytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            
            if (decodePixelWidth > 0)
                bitmap.DecodePixelWidth = decodePixelWidth;
            
            if (decodePixelHeight > 0)
                bitmap.DecodePixelHeight = decodePixelHeight;
            
            bitmap.EndInit();
            bitmap.Freeze(); // 冻结以提高性能并允许跨线程访问
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
