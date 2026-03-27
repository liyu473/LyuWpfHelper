using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace LyuWpfHelper.Helpers;

/// <summary>
/// WPF 全局异常捕获辅助类
/// </summary>
public class GlobalExceptionHandler
{
    private readonly ILogger? _logger;
    private readonly Action<Exception>? _customHandler;
    private bool _isInitialized;

    /// <summary>
    /// 初始化全局异常处理器
    /// </summary>
    /// <param name="logger">日志记录器(可选)</param>
    /// <param name="customHandler">自定义异常处理逻辑(可选)</param>
    public GlobalExceptionHandler(ILogger? logger = null, Action<Exception>? customHandler = null)
    {
        _logger = logger;
        _customHandler = customHandler;
    }

    /// <summary>
    /// 启用全局异常捕获
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
            return;

        // UI 线程未处理异常
        Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 非 UI 线程未处理异常
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // Task 未处理异常
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _isInitialized = true;
    }

    /// <summary>
    /// 禁用全局异常捕获
    /// </summary>
    public void Uninitialize()
    {
        if (!_isInitialized)
            return;

        Application.Current.DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

        _isInitialized = false;
    }

    private void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e
    )
    {
        HandleException(e.Exception);
        e.Handled = true; // 标记为已处理,防止应用崩溃
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            HandleException(exception);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleException(e.Exception);
        e.SetObserved(); // 标记为已观察,防止应用崩溃
    }

    private void HandleException(Exception exception)
    {
        // 优先执行自定义处理逻辑
        if (_customHandler != null)
        {
            try
            {
                _customHandler(exception);
            }
            catch (Exception handlerException)
            {
                // 自定义处理器本身出错时,记录到日志
                _logger?.LogError(handlerException, "自定义异常处理器执行失败");
            }
        }

        // 默认记录日志
        _logger?.LogError(exception, "捕获到未处理的异常");
    }
}
