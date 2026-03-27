using System.Windows;
using System.Windows.Documents;
using LyuWpfHelper.Adorners;

namespace LyuWpfHelper.Services;

public class BusyService : IBusyService
{
    private readonly object _lock = new();
    private BusyAdorner? _adorner;
    private AdornerLayer? _adornerLayer;
    private Window? _ownerWindow;
    private BusyDisplayOptions? _currentBusy;

    public void SetOwnerWindow(Window owner)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                TrackOwnerWindow(owner, removeAdornerWhenChanged: true);
            }
        });
    }

    public void Show(BusyDisplayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Application.Current?.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                if (!EnsureAdorner())
                {
                    return;
                }

                _currentBusy = options;
                ApplyCurrentBusy();
            }
        });
    }

    public void Hide()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                _currentBusy = null;
                RemoveAdorner();
            }
        });
    }

    private void ApplyCurrentBusy()
    {
        if (_currentBusy == null)
        {
            RemoveAdorner();
            return;
        }

        if (_adorner == null && !EnsureAdorner())
        {
            return;
        }

        if (_adorner == null)
        {
            return;
        }

        _adorner.BusyMask.Title = string.IsNullOrWhiteSpace(_currentBusy.Title)
            ? "Please wait"
            : _currentBusy.Title;

        _adorner.BusyMask.Message = string.IsNullOrWhiteSpace(_currentBusy.Message)
            ? "Processing your request..."
            : _currentBusy.Message;

        _adorner.BusyMask.Content = _currentBusy.Content;
        _adorner.BusyMask.ContentTemplate = _currentBusy.ContentTemplate;
        _adorner.SetInputBlocking(_currentBusy.BlockInput);
        _adorner.BusyMask.PlayShowAnimation();
    }

    private bool EnsureAdorner()
    {
        if (_adorner != null)
        {
            return true;
        }

        var owner = _ownerWindow ?? Application.Current?.MainWindow;
        if (owner?.Content is not UIElement content)
        {
            return false;
        }

        if (_adornerLayer == null || _ownerWindow != owner)
        {
            TrackOwnerWindow(owner, removeAdornerWhenChanged: false);
        }

        if (_adornerLayer == null)
        {
            return false;
        }

        _adorner = new BusyAdorner(content);
        _adornerLayer.Add(_adorner);
        return true;
    }

    private void InitializeAdornerLayer()
    {
        if (_ownerWindow?.Content is not UIElement content)
        {
            _adornerLayer = null;
            return;
        }

        _adornerLayer = AdornerLayer.GetAdornerLayer(content) ?? AdornerLayer.GetAdornerLayer(_ownerWindow);
    }

    private void OnOwnerWindowClosed(object? sender, EventArgs e)
    {
        var closedWindow = _ownerWindow;

        lock (_lock)
        {
            RemoveAdorner();
            _adornerLayer = null;
            _ownerWindow = null;
            _currentBusy = null;
        }

        if (closedWindow != null)
        {
            closedWindow.Closed -= OnOwnerWindowClosed;
        }
    }

    private void RemoveAdorner()
    {
        if (_adorner != null && _adornerLayer != null)
        {
            _adornerLayer.Remove(_adorner);
        }

        _adorner = null;
    }

    private void TrackOwnerWindow(Window owner, bool removeAdornerWhenChanged)
    {
        if (_ownerWindow != null && _ownerWindow != owner)
        {
            _ownerWindow.Closed -= OnOwnerWindowClosed;
            if (removeAdornerWhenChanged)
            {
                RemoveAdorner();
            }
        }

        _ownerWindow = owner;
        InitializeAdornerLayer();
        _ownerWindow.Closed -= OnOwnerWindowClosed;
        _ownerWindow.Closed += OnOwnerWindowClosed;
    }
}

