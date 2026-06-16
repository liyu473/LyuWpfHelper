using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;

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

    public static readonly DependencyProperty IsVisibleProperty =
        DependencyProperty.RegisterAttached(
            "IsVisible",
            typeof(bool),
            typeof(DisplayMask),
            new PropertyMetadata(true, OnMaskPropertyChanged)
        );

    public static string? GetText(DependencyObject obj) =>
        (string?)obj.GetValue(TextProperty);

    public static void SetText(DependencyObject obj, string? value) =>
        obj.SetValue(TextProperty, value);

    public static bool GetIsVisible(DependencyObject obj) =>
        (bool)obj.GetValue(IsVisibleProperty);

    public static void SetIsVisible(DependencyObject obj, bool value) =>
        obj.SetValue(IsVisibleProperty, value);

    private static void OnMaskPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        var state = States.GetValue(element, static target => new DisplayMaskState(target));
        state.Attach();
        state.Refresh();
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
        private Panel? _parentPanel;
        private ContentControl? _parentContentControl;
        private Decorator? _parentDecorator;
        private DependencyPropertyDescriptor? _contentDescriptor;
        private Grid? _wrapperRoot;
        private ContentControl? _contentHost;
        private UIElement? _decoratorChild;
        private UIElement? _blurTarget;
        private FrameworkElement? _wrappedElement;
        private object? _parentContentControlContent;
        private int _parentPanelChildIndex = -1;
        private Border? _overlay;
        private Border? _contentBackground;
        private Border? _contentBorderStroke;
        private Border? _tintLayer;
        private Border? _accentLayer;
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
            if (string.IsNullOrWhiteSpace(text) || !GetIsVisible(_element))
            {
                HideMask();
                return;
            }

            EnsureWrapped();

            if (_overlay is null || _messageText is null)
            {
                return;
            }

            _messageText.Text = text;
            ApplyBlur(true);

            _overlay.IsHitTestVisible = true;
            _overlay.Visibility = Visibility.Visible;
            ApplyAppearance();
        }

        private void HideMask()
        {
            ApplyBlur(false);

            if (_overlay is null)
            {
                return;
            }

            _overlay.IsHitTestVisible = false;
            _overlay.Visibility = Visibility.Collapsed;
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

            if (_wrappedElement is not null && _wrapperRoot is not null)
            {
                RestoreWrappedElement();
            }

            _wrapperRoot = null;
            _contentHost = null;
            _decoratorChild = null;
            _wrappedElement = null;
            _parentPanel = null;
            _parentContentControl = null;
            _parentDecorator = null;
            _parentContentControlContent = null;
            _parentPanelChildIndex = -1;
            _blurTarget = null;
            _overlay = null;
            _contentBackground = null;
            _contentBorderStroke = null;
            _tintLayer = null;
            _accentLayer = null;
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
                return;
            }

            EnsureGeneralElementWrapped();
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
            _blurTarget = contentHost;
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
            _blurTarget = originalChild;
            _wrapperRoot = wrapperRoot;
            _overlay = overlay;

            SetDecoratorChildSafely(wrapperRoot);
        }

        private void EnsureGeneralElementWrapped()
        {
            if (_wrapperRoot is not null && _wrappedElement is not null)
            {
                return;
            }

            if (_element.Parent is not DependencyObject parent)
            {
                return;
            }

            var overlay = CreateOverlay();
            var wrapperRoot = new Grid();

            if (!TryDetachElementFromParent(parent))
            {
                return;
            }

            wrapperRoot.Children.Add(_element);
            wrapperRoot.Children.Add(overlay);

            if (!TryAttachWrapperToParent(wrapperRoot))
            {
                wrapperRoot.Children.Remove(_element);
                TryRestoreElementToParent(_element);
                return;
            }

            _wrappedElement = _element;
            _blurTarget = _element;
            _wrapperRoot = wrapperRoot;
            _overlay = overlay;
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
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
            };

            _contentBackground = new Border
            {
                CornerRadius = new CornerRadius(16),
                Opacity = 0.78,
            };

            _contentBorderStroke = new Border
            {
                CornerRadius = new CornerRadius(16),
                BorderThickness = new Thickness(0),
                Opacity = 0,
            };

            var contentBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 280,
                MaxWidth = 420,
                CornerRadius = new CornerRadius(16),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 28,
                    ShadowDepth = 0,
                    Color = Color.FromArgb(56, 50, 72, 104),
                    Opacity = 0.28,
                },
                Child = new Grid
                {
                    Children =
                    {
                        _contentBackground,
                        _contentBorderStroke,
                        new StackPanel
                        {
                            Margin = new Thickness(28, 20, 28, 20),
                            Children =
                            {
                                _messageText,
                            },
                        },
                    },
                },
            };

            _tintLayer = new Border
            {
                Opacity = 0.32,
                BorderThickness = new Thickness(0),
            };

            _accentLayer = new Border
            {
                Opacity = 0,
                IsHitTestVisible = false,
            };

            var noiseLayer = new Border
            {
                Background = CreateNoiseBrush(),
                Opacity = 0.02,
                IsHitTestVisible = false,
            };

            return new Border
            {
                Visibility = Visibility.Collapsed,
                Background = Brushes.Transparent,
                Child = new Grid
                {
                    Children =
                    {
                        _tintLayer,
                        _accentLayer,
                        noiseLayer,
                        contentBorder,
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

        private bool TryDetachElementFromParent(DependencyObject parent)
        {
            if (parent is Panel panel)
            {
                var childIndex = panel.Children.IndexOf(_element);
                if (childIndex < 0)
                {
                    return false;
                }

                _parentPanel = panel;
                _parentPanelChildIndex = childIndex;
                panel.Children.RemoveAt(childIndex);
                return true;
            }

            if (parent is ContentControl contentControl && ReferenceEquals(contentControl.Content, _element))
            {
                _parentContentControl = contentControl;
                _parentContentControlContent = contentControl.Content;
                contentControl.Content = null;
                return true;
            }

            if (parent is Decorator decorator && ReferenceEquals(decorator.Child, _element))
            {
                _parentDecorator = decorator;
                decorator.Child = null;
                return true;
            }

            return false;
        }

        private bool TryAttachWrapperToParent(Grid wrapperRoot)
        {
            if (_parentPanel is not null)
            {
                var insertIndex = Math.Max(0, Math.Min(_parentPanelChildIndex, _parentPanel.Children.Count));
                _parentPanel.Children.Insert(insertIndex, wrapperRoot);
                CopyLayoutMetadata(_wrappedElement ?? _element, wrapperRoot);
                return true;
            }

            if (_parentContentControl is not null)
            {
                _parentContentControl.Content = wrapperRoot;
                return true;
            }

            if (_parentDecorator is not null)
            {
                _parentDecorator.Child = wrapperRoot;
                return true;
            }

            return false;
        }

        private void RestoreWrappedElement()
        {
            if (_wrappedElement is null || _wrapperRoot is null)
            {
                return;
            }

            _wrapperRoot.Children.Remove(_wrappedElement);
            TryRestoreElementToParent(_wrappedElement);
        }

        private void TryRestoreElementToParent(FrameworkElement element)
        {
            if (_parentPanel is not null)
            {
                var wrapperIndex = _parentPanel.Children.IndexOf(_wrapperRoot!);
                if (wrapperIndex >= 0)
                {
                    _parentPanel.Children.RemoveAt(wrapperIndex);
                }

                var insertIndex = Math.Max(0, Math.Min(_parentPanelChildIndex, _parentPanel.Children.Count));
                _parentPanel.Children.Insert(insertIndex, element);
                return;
            }

            if (_parentContentControl is not null)
            {
                if (ReferenceEquals(_parentContentControl.Content, _wrapperRoot))
                {
                    _parentContentControl.Content = element;
                }
                else if (_parentContentControlContent is not null)
                {
                    _parentContentControl.Content = _parentContentControlContent;
                }

                return;
            }

            if (_parentDecorator is not null)
            {
                if (ReferenceEquals(_parentDecorator.Child, _wrapperRoot))
                {
                    _parentDecorator.Child = element;
                }
            }
        }

        private static void CopyLayoutMetadata(FrameworkElement source, FrameworkElement target)
        {
            target.HorizontalAlignment = source.HorizontalAlignment;
            target.VerticalAlignment = source.VerticalAlignment;
            target.Margin = source.Margin;
            target.Width = source.Width;
            target.Height = source.Height;
            target.MinWidth = source.MinWidth;
            target.MinHeight = source.MinHeight;
            target.MaxWidth = source.MaxWidth;
            target.MaxHeight = source.MaxHeight;

            target.SetValue(Grid.RowProperty, source.GetValue(Grid.RowProperty));
            target.SetValue(Grid.ColumnProperty, source.GetValue(Grid.ColumnProperty));
            target.SetValue(Grid.RowSpanProperty, source.GetValue(Grid.RowSpanProperty));
            target.SetValue(Grid.ColumnSpanProperty, source.GetValue(Grid.ColumnSpanProperty));
            target.SetValue(DockPanel.DockProperty, source.GetValue(DockPanel.DockProperty));
            target.SetValue(Panel.ZIndexProperty, source.GetValue(Panel.ZIndexProperty));
            target.SetValue(Canvas.LeftProperty, source.GetValue(Canvas.LeftProperty));
            target.SetValue(Canvas.TopProperty, source.GetValue(Canvas.TopProperty));
            target.SetValue(Canvas.RightProperty, source.GetValue(Canvas.RightProperty));
            target.SetValue(Canvas.BottomProperty, source.GetValue(Canvas.BottomProperty));
        }

        private void ApplyAppearance()
        {
            ApplyBrush(_messageText, TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(28, 44, 68)));
            ApplyBrush(_contentBackground, Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(248, 250, 252)));
            ApplyBrush(_tintLayer, Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(188, 238, 242, 247)));
            ApplyBrush(_accentLayer, Border.BackgroundProperty, Brushes.Transparent);
        }

        private void ApplyBlur(bool isVisible)
        {
            if (_blurTarget is null)
            {
                return;
            }

            _blurTarget.Effect = isVisible ? new BlurEffect { Radius = 32 } : null;
        }

        private static void ApplyBrush(FrameworkElement? element, DependencyProperty property, Brush brush)
        {
            if (element is null)
            {
                return;
            }

            element.SetValue(property, brush);
        }

        private static Brush CreateNoiseBrush()
        {
            var primaryDot = new GeometryDrawing(
                new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
                null,
                new RectangleGeometry(new Rect(0, 0, 1, 1))
            );
            var secondaryDot = new GeometryDrawing(
                new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                null,
                new RectangleGeometry(new Rect(2, 2, 1, 1))
            );

            var drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(primaryDot);
            drawingGroup.Children.Add(secondaryDot);
            drawingGroup.Freeze();

            var drawingBrush = new DrawingBrush(drawingGroup)
            {
                Stretch = Stretch.None,
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 4, 4),
                ViewportUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(0, 0, 4, 4),
                ViewboxUnits = BrushMappingMode.Absolute,
            };
            drawingBrush.Freeze();
            return drawingBrush;
        }
    }
}
