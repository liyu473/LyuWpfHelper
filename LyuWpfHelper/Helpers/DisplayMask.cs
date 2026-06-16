using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace LyuWpfHelper.Helpers;

public static class DisplayMask
{
    private static readonly ConditionalWeakTable<FrameworkElement, DisplayMaskState> States = new();

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached(
            "Text",
            typeof(string),
            typeof(DisplayMask),
            new PropertyMetadata(null, OnMaskPropertyChanged)
        );

    public static string? GetText(DependencyObject obj) =>
        (string?)obj.GetValue(TextProperty);

    public static void SetText(DependencyObject obj, string? value) =>
        obj.SetValue(TextProperty, value);

    private static void OnMaskPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(GetText(element)))
        {
            var state = States.GetValue(element, static target => new DisplayMaskState(target));
            state.Attach();
            state.Refresh();
            return;
        }

        if (States.TryGetValue(element, out var stateToRemove))
        {
            stateToRemove.Detach();
            States.Remove(element);
        }
    }

    private sealed class DisplayMaskState
    {
        private readonly FrameworkElement _element;
        private readonly RoutedEventHandler _loadedHandler;
        private readonly RoutedEventHandler _unloadedHandler;

        private bool _isAttached;
        private bool _isUpdatingContent;
        private ContentControl? _contentControl;
        private Decorator? _decorator;
        private DependencyPropertyDescriptor? _contentDescriptor;
        private Grid? _wrapperRoot;
        private ContentControl? _contentHost;
        private UIElement? _decoratorChild;
        private Border? _overlay;
        private TextBlock? _messageText;

        public DisplayMaskState(FrameworkElement element)
        {
            _element = element;
            _loadedHandler = (_, _) => Refresh();
            _unloadedHandler = (_, _) => Refresh();
        }

        public void Attach()
        {
            if (_isAttached)
            {
                return;
            }

            if (_element is ContentControl contentControl)
            {
                _contentControl = contentControl;
                _contentDescriptor = DependencyPropertyDescriptor.FromProperty(
                    ContentControl.ContentProperty,
                    typeof(ContentControl)
                );
                _contentDescriptor?.AddValueChanged(_contentControl, OnContentChanged);
            }
            else if (_element is Decorator decorator)
            {
                _decorator = decorator;
                _contentDescriptor = DependencyPropertyDescriptor.FromName(
                    nameof(Decorator.Child),
                    typeof(Decorator),
                    typeof(Decorator)
                );
                _contentDescriptor?.AddValueChanged(_decorator, OnDecoratorChildChanged);
            }

            _element.Loaded += _loadedHandler;
            _element.Unloaded += _unloadedHandler;
            _isAttached = true;
        }

        public void Detach()
        {
            if (!_isAttached)
            {
                return;
            }

            if (_contentControl is not null)
            {
                _contentDescriptor?.RemoveValueChanged(_contentControl, OnContentChanged);
            }

            if (_decorator is not null)
            {
                _contentDescriptor?.RemoveValueChanged(_decorator, OnDecoratorChildChanged);
            }

            _element.Loaded -= _loadedHandler;
            _element.Unloaded -= _unloadedHandler;
            RemoveMask();
            _contentDescriptor = null;
            _contentControl = null;
            _decorator = null;
            _isAttached = false;
        }

        public void Refresh()
        {
            var text = GetText(_element);
            if (string.IsNullOrWhiteSpace(text))
            {
                RemoveMask();
                return;
            }

            if (_contentControl is null && _decorator is null)
            {
                return;
            }

            EnsureWrapped();

            if (_overlay is null || _messageText is null)
            {
                return;
            }

            _messageText.Text = text;
            _overlay.IsHitTestVisible = true;
        }

        private void RemoveMask()
        {
            if (_contentControl is not null && _wrapperRoot is not null && ReferenceEquals(_contentControl.Content, _wrapperRoot))
            {
                SetContentSafely(_contentHost?.Content);
            }

            if (_decorator is not null && _wrapperRoot is not null && ReferenceEquals(_decorator.Child, _wrapperRoot))
            {
                SetDecoratorChildSafely(_decoratorChild);
            }

            _wrapperRoot = null;
            _contentHost = null;
            _decoratorChild = null;
            _overlay = null;
            _messageText = null;
        }

        private void EnsureWrapped()
        {
            if (_contentControl is not null)
            {
                EnsureContentControlWrapped();
                return;
            }

            if (_decorator is not null)
            {
                EnsureDecoratorWrapped();
            }
        }

        private void EnsureContentControlWrapped()
        {
            if (_contentControl is null)
            {
                return;
            }

            if (_wrapperRoot is not null && ReferenceEquals(_contentControl.Content, _wrapperRoot))
            {
                return;
            }

            var originalContent = _contentControl.Content;
            if (ReferenceEquals(originalContent, _wrapperRoot))
            {
                return;
            }

            var contentHost = CreateContentHost();
            contentHost.Content = originalContent;

            var overlay = CreateOverlay();
            var wrapperRoot = new Grid();
            wrapperRoot.Children.Add(contentHost);
            wrapperRoot.Children.Add(overlay);

            _contentHost = contentHost;
            _wrapperRoot = wrapperRoot;
            _overlay = overlay;

            SetContentSafely(wrapperRoot);
        }

        private void EnsureDecoratorWrapped()
        {
            if (_decorator is null)
            {
                return;
            }

            if (_wrapperRoot is not null && ReferenceEquals(_decorator.Child, _wrapperRoot))
            {
                return;
            }

            var originalChild = _decorator.Child;
            if (ReferenceEquals(originalChild, _wrapperRoot))
            {
                return;
            }

            var overlay = CreateOverlay();
            var wrapperRoot = new Grid();

            SetDecoratorChildSafely(null);

            if (originalChild is not null)
            {
                wrapperRoot.Children.Add(originalChild);
            }

            wrapperRoot.Children.Add(overlay);

            _decoratorChild = originalChild;
            _wrapperRoot = wrapperRoot;
            _overlay = overlay;

            SetDecoratorChildSafely(wrapperRoot);
        }

        private ContentControl CreateContentHost()
        {
            var contentHost = new ContentControl();

            if (_contentControl is null)
            {
                return contentHost;
            }

            CopyBinding(_contentControl, contentHost, ContentControl.ContentTemplateProperty);
            CopyBinding(_contentControl, contentHost, ContentControl.ContentTemplateSelectorProperty);
            CopyBinding(_contentControl, contentHost, ContentControl.ContentStringFormatProperty);
            CopyBinding(_contentControl, contentHost, Control.HorizontalContentAlignmentProperty);
            CopyBinding(_contentControl, contentHost, Control.VerticalContentAlignmentProperty);

            return contentHost;
        }

        private static void CopyBinding(DependencyObject source, DependencyObject target, DependencyProperty property)
        {
            BindingOperations.SetBinding(
                target,
                property,
                new Binding
                {
                    Source = source,
                    Path = new PropertyPath(property),
                    Mode = BindingMode.OneWay,
                }
            );
        }

        private Border CreateOverlay()
        {
            _messageText = new TextBlock
            {
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(40, 54, 78)),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
            };

            var messageCard = new Border
            {
                Padding = new Thickness(28, 18, 28, 18),
                MinWidth = 220,
                MaxWidth = 420,
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromArgb(236, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(56, 92, 118, 148)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = _messageText,
            };

            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(168, 244, 247, 250)),
                Child = new Grid
                {
                    Children =
                    {
                        messageCard,
                    },
                },
            };
        }

        private void OnContentChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingContent || _contentControl is null || _wrapperRoot is null)
            {
                return;
            }

            var currentContent = _contentControl.Content;
            if (ReferenceEquals(currentContent, _wrapperRoot))
            {
                return;
            }

            if (_contentHost is not null)
            {
                _contentHost.Content = currentContent;
                SetContentSafely(_wrapperRoot);
            }
        }

        private void OnDecoratorChildChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingContent || _decorator is null || _wrapperRoot is null || _overlay is null)
            {
                return;
            }

            var currentChild = _decorator.Child;
            if (ReferenceEquals(currentChild, _wrapperRoot))
            {
                return;
            }

            SetDecoratorChildSafely(null);
            _wrapperRoot.Children.Clear();

            if (currentChild is not null)
            {
                _wrapperRoot.Children.Add(currentChild);
            }

            _wrapperRoot.Children.Add(_overlay);
            _decoratorChild = currentChild;
            SetDecoratorChildSafely(_wrapperRoot);
        }

        private void SetContentSafely(object? content)
        {
            if (_contentControl is null)
            {
                return;
            }

            _isUpdatingContent = true;
            try
            {
                _contentControl.Content = content;
            }
            finally
            {
                _isUpdatingContent = false;
            }
        }

        private void SetDecoratorChildSafely(UIElement? child)
        {
            if (_decorator is null)
            {
                return;
            }

            _isUpdatingContent = true;
            try
            {
                _decorator.Child = child;
            }
            finally
            {
                _isUpdatingContent = false;
            }
        }
    }
}
