using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace LyuWpfHelper.Helpers;

/// <summary>
/// Prevents the current WPF application from starting more than once.
/// Keep the created instance alive for the whole application lifetime.
/// </summary>
public sealed class SingleInstanceHelper : IDisposable
{
    private readonly Mutex _mutex;
    private bool _disposed;

    private SingleInstanceHelper(Mutex mutex, string mutexName)
    {
        _mutex = mutex;
        MutexName = mutexName;
    }

    /// <summary>
    /// The final mutex name used by the helper.
    /// </summary>
    public string MutexName { get; }

    /// <summary>
    /// Tries to create a single-instance lock for the current application.
    /// Returns null when another instance is already running.
    /// </summary>
    /// <param name="instanceKey">
    /// Optional custom key. When omitted, a key is generated from the entry assembly.
    /// </param>
    public static SingleInstanceHelper? TryCreate(string? instanceKey = null)
    {
        string mutexName = BuildMutexName(instanceKey);
        var mutex = new Mutex(false, mutexName, out bool createdNew);

        if (!createdNew)
        {
            mutex.Dispose();
            return null;
        }

        return new SingleInstanceHelper(mutex, mutexName);
    }

    /// <summary>
    /// Builds the named mutex identifier used to distinguish application instances.
    /// </summary>
    public static string BuildMutexName(string? instanceKey = null)
    {
        string key = string.IsNullOrWhiteSpace(instanceKey) ? GetDefaultInstanceKey() : instanceKey.Trim();
        return $"Local\\LyuWpfHelper.SingleInstance.{SanitizeKey(key)}";
    }

    /// <summary>
    /// Releases the mutex handle.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _mutex.Dispose();
        _disposed = true;
    }

    private static string GetDefaultInstanceKey()
    {
        Assembly entryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        string appName = entryAssembly.GetName().Name ?? "Application";
        string location = entryAssembly.Location;

        if (string.IsNullOrWhiteSpace(location))
        {
            return appName;
        }

        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(location.ToUpperInvariant()));
        string pathHash = Convert.ToHexString(hashBytes)[..16];
        return $"{appName}.{pathHash}";
    }

    private static string SanitizeKey(string key)
    {
        var builder = new StringBuilder(key.Length);

        foreach (char c in key)
        {
            builder.Append(char.IsLetterOrDigit(c) || c is '.' or '_' or '-' ? c : '_');
        }

        return builder.Length == 0 ? "Application" : builder.ToString();
    }
}
