using LyuWpfHelper.Services;

namespace LyuWpfHelper.Extensions;

public static class BusyServiceExtensions
{
    public static void Show(this IBusyService busyService, string message, int timeout = 0)
    {
        ShowCore(
            busyService,
            new BusyDisplayOptions
            {
                Title = "Loading",
                Message = message
            },
            timeout
        );
    }

    public static void Show(
        this IBusyService busyService,
        string title,
        string message,
        int timeout = 0
    )
    {
        ShowCore(
            busyService,
            new BusyDisplayOptions
            {
                Title = title,
                Message = message
            },
            timeout
        );
    }

    public static void ShowWithContent(this IBusyService busyService, object content, int timeout = 0)
    {
        ShowCore(
            busyService,
            new BusyDisplayOptions
            {
                Title = "Loading",
                Content = content
            },
            timeout
        );
    }

    public static void ShowWithContent(
        this IBusyService busyService,
        string title,
        object content,
        int timeout = 0
    )
    {
        ShowCore(
            busyService,
            new BusyDisplayOptions
            {
                Title = title,
                Content = content
            },
            timeout
        );
    }

    public static void Show(
        this IBusyService busyService,
        string title,
        string message,
        object content,
        int timeout = 0
    )
    {
        ShowCore(
            busyService,
            new BusyDisplayOptions
            {
                Title = title,
                Message = message,
                Content = content
            },
            timeout
        );
    }

    public static void ShowWithTimeout(
        this IBusyService busyService,
        BusyDisplayOptions options,
        int timeout = 0
    )
    {
        ShowCore(busyService, options, timeout);
    }

    public static Task RunWithBusyAsync(
        this IBusyService busyService,
        Func<CancellationToken, Task> action,
        BusyDisplayOptions? options = null,
        TimeSpan? timeout = null,
        Action<TimeSpan>? onTimeout = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        return RunWithBusyInternalAsync(
            busyService,
            async token =>
            {
                await action(token).ConfigureAwait(false);
                return true;
            },
            options,
            timeout,
            onTimeout,
            cancellationToken
        );
    }

    public static Task<TResult> RunWithBusyAsync<TResult>(
        this IBusyService busyService,
        Func<CancellationToken, Task<TResult>> action,
        BusyDisplayOptions? options = null,
        TimeSpan? timeout = null,
        Action<TimeSpan>? onTimeout = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        return RunWithBusyInternalAsync(
            busyService,
            action,
            options,
            timeout,
            onTimeout,
            cancellationToken
        );
    }

    public static Task RunWithBusyAsync(
        this IBusyService busyService,
        Func<Task> action,
        BusyDisplayOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        return busyService.RunWithBusyAsync(_ => action(), options, null, null, default);
    }

    public static Task<TResult> RunWithBusyAsync<TResult>(
        this IBusyService busyService,
        Func<Task<TResult>> action,
        BusyDisplayOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        return busyService.RunWithBusyAsync(_ => action(), options, null, null, default);
    }

    private static void ShowCore(IBusyService busyService, BusyDisplayOptions options, int timeout)
    {
        ArgumentNullException.ThrowIfNull(busyService);
        ArgumentNullException.ThrowIfNull(options);

        busyService.Show(options);

        if (timeout > 0)
        {
            ScheduleAutoHide(busyService, TimeSpan.FromMilliseconds(timeout));
        }
    }

    private static void ScheduleAutoHide(IBusyService busyService, TimeSpan timeout)
    {
        _ = AutoHideAsync(busyService, timeout);
    }

    private static async Task AutoHideAsync(IBusyService busyService, TimeSpan timeout)
    {
        try
        {
            await Task.Delay(timeout).ConfigureAwait(false);
            busyService.Hide();
        }
        catch
        {
            // Best effort timeout hide.
        }
    }

    private static async Task<TResult> RunWithBusyInternalAsync<TResult>(
        IBusyService busyService,
        Func<CancellationToken, Task<TResult>> action,
        BusyDisplayOptions? options,
        TimeSpan? timeout,
        Action<TimeSpan>? onTimeout,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(busyService);
        ArgumentNullException.ThrowIfNull(action);

        busyService.Show(options ?? CreateDefaultOptions());

        CancellationTokenSource? timeoutCts = null;
        CancellationTokenSource? linkedCts = null;

        try
        {
            var effectiveToken = cancellationToken;

            if (timeout is { } timeoutValue && timeoutValue > TimeSpan.Zero)
            {
                timeoutCts = new CancellationTokenSource(timeoutValue);

                if (cancellationToken.CanBeCanceled)
                {
                    linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        timeoutCts.Token
                    );
                    effectiveToken = linkedCts.Token;
                }
                else
                {
                    effectiveToken = timeoutCts.Token;
                }
            }

            return await action(effectiveToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
            when (
                timeoutCts is not null
                && timeoutCts.IsCancellationRequested
                && !cancellationToken.IsCancellationRequested
            )
        {
            if (timeout is { } timeoutValue)
            {
                InvokeTimeoutCallback(onTimeout, timeoutValue);
            }

            throw new TimeoutException(
                $"The busy operation timed out after {timeout!.Value.TotalMilliseconds:F0} ms.",
                ex
            );
        }
        finally
        {
            linkedCts?.Dispose();
            timeoutCts?.Dispose();
            busyService.Hide();
        }
    }

    private static BusyDisplayOptions CreateDefaultOptions()
    {
        return new BusyDisplayOptions
        {
            Title = "Loading",
            Message = "Please wait...",
            BlockInput = true
        };
    }

    private static void InvokeTimeoutCallback(Action<TimeSpan>? onTimeout, TimeSpan timeout)
    {
        try
        {
            onTimeout?.Invoke(timeout);
        }
        catch
        {
            // Ignore callback errors to preserve timeout semantics.
        }
    }

}
