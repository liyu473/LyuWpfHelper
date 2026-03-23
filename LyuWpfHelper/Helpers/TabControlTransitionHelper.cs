using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;
using LyuWpfHelper.Controls;

namespace LyuWpfHelper.Helpers;

/// <summary>
/// Adds transition animation support to TabControl selected content without creating a new control.
/// </summary>
public static class TabControlTransitionHelper
{
    public static readonly DependencyProperty TransitionModeProperty =
        DependencyProperty.RegisterAttached(
            "TransitionMode",
            typeof(TransitionMode),
            typeof(TabControlTransitionHelper),
            new PropertyMetadata(TransitionMode.Custom, OnTransitionSettingChanged)
        );

    public static readonly DependencyProperty TransitionStoryboardProperty =
        DependencyProperty.RegisterAttached(
            "TransitionStoryboard",
            typeof(Storyboard),
            typeof(TabControlTransitionHelper),
            new PropertyMetadata(null, OnTransitionSettingChanged)
        );

    private static readonly DependencyProperty HasSnapshotProperty =
        DependencyProperty.RegisterAttached(
            "HasSnapshot",
            typeof(bool),
            typeof(TabControlTransitionHelper),
            new PropertyMetadata(false)
        );

    private static readonly DependencyProperty OriginalContentTemplateProperty =
        DependencyProperty.RegisterAttached(
            "OriginalContentTemplate",
            typeof(DataTemplate),
            typeof(TabControlTransitionHelper)
        );

    private static readonly DependencyProperty OriginalContentTemplateSelectorProperty =
        DependencyProperty.RegisterAttached(
            "OriginalContentTemplateSelector",
            typeof(DataTemplateSelector),
            typeof(TabControlTransitionHelper)
        );

    private static readonly DependencyProperty OriginalContentStringFormatProperty =
        DependencyProperty.RegisterAttached(
            "OriginalContentStringFormat",
            typeof(string),
            typeof(TabControlTransitionHelper)
        );

    private static readonly DependencyProperty OriginalContentTemplateLocalValueProperty =
        DependencyProperty.RegisterAttached(
            "OriginalContentTemplateLocalValue",
            typeof(object),
            typeof(TabControlTransitionHelper)
        );

    private static readonly DependencyProperty OriginalContentTemplateSelectorLocalValueProperty =
        DependencyProperty.RegisterAttached(
            "OriginalContentTemplateSelectorLocalValue",
            typeof(object),
            typeof(TabControlTransitionHelper)
        );

    private static readonly DependencyProperty OriginalContentStringFormatLocalValueProperty =
        DependencyProperty.RegisterAttached(
            "OriginalContentStringFormatLocalValue",
            typeof(object),
            typeof(TabControlTransitionHelper)
        );

    private static readonly DataTemplate TransitionContentTemplate = CreateTransitionContentTemplate();

    public static TransitionMode GetTransitionMode(DependencyObject obj) =>
        (TransitionMode)obj.GetValue(TransitionModeProperty);

    public static void SetTransitionMode(DependencyObject obj, TransitionMode value) =>
        obj.SetValue(TransitionModeProperty, value);

    public static Storyboard? GetTransitionStoryboard(DependencyObject obj) =>
        (Storyboard?)obj.GetValue(TransitionStoryboardProperty);

    public static void SetTransitionStoryboard(DependencyObject obj, Storyboard? value) =>
        obj.SetValue(TransitionStoryboardProperty, value);

    private static bool GetHasSnapshot(DependencyObject obj) => (bool)obj.GetValue(HasSnapshotProperty);

    private static void SetHasSnapshot(DependencyObject obj, bool value) =>
        obj.SetValue(HasSnapshotProperty, value);

    private static DataTemplate? GetOriginalContentTemplate(DependencyObject obj) =>
        (DataTemplate?)obj.GetValue(OriginalContentTemplateProperty);

    private static void SetOriginalContentTemplate(DependencyObject obj, DataTemplate? value) =>
        obj.SetValue(OriginalContentTemplateProperty, value);

    private static DataTemplateSelector? GetOriginalContentTemplateSelector(DependencyObject obj) =>
        (DataTemplateSelector?)obj.GetValue(OriginalContentTemplateSelectorProperty);

    private static void SetOriginalContentTemplateSelector(
        DependencyObject obj,
        DataTemplateSelector? value
    ) => obj.SetValue(OriginalContentTemplateSelectorProperty, value);

    private static string? GetOriginalContentStringFormat(DependencyObject obj) =>
        (string?)obj.GetValue(OriginalContentStringFormatProperty);

    private static void SetOriginalContentStringFormat(DependencyObject obj, string? value) =>
        obj.SetValue(OriginalContentStringFormatProperty, value);

    private static object GetOriginalContentTemplateLocalValue(DependencyObject obj) =>
        obj.GetValue(OriginalContentTemplateLocalValueProperty);

    private static void SetOriginalContentTemplateLocalValue(DependencyObject obj, object value) =>
        obj.SetValue(OriginalContentTemplateLocalValueProperty, value);

    private static object GetOriginalContentTemplateSelectorLocalValue(DependencyObject obj) =>
        obj.GetValue(OriginalContentTemplateSelectorLocalValueProperty);

    private static void SetOriginalContentTemplateSelectorLocalValue(DependencyObject obj, object value) =>
        obj.SetValue(OriginalContentTemplateSelectorLocalValueProperty, value);

    private static object GetOriginalContentStringFormatLocalValue(DependencyObject obj) =>
        obj.GetValue(OriginalContentStringFormatLocalValueProperty);

    private static void SetOriginalContentStringFormatLocalValue(DependencyObject obj, object value) =>
        obj.SetValue(OriginalContentStringFormatLocalValueProperty, value);

    private static void OnTransitionSettingChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not TabControl tabControl)
            return;

        UpdateTransitionState(tabControl);
    }

    private static void OnTabControlLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TabControl tabControl)
            return;

        UpdateTransitionState(tabControl);
    }

    private static void UpdateTransitionState(TabControl tabControl)
    {
        if (ShouldEnableTransition(tabControl))
        {
            if (tabControl.IsLoaded)
            {
                EnableTransition(tabControl);
            }
            else
            {
                tabControl.Loaded -= OnTabControlLoaded;
                tabControl.Loaded += OnTabControlLoaded;
            }
        }
        else
        {
            tabControl.Loaded -= OnTabControlLoaded;
            DisableTransition(tabControl);
        }
    }

    private static bool ShouldEnableTransition(TabControl tabControl)
    {
        return HasTransitionConfiguration(tabControl);
    }

    private static bool HasTransitionConfiguration(TabControl tabControl)
    {
        return GetTransitionStoryboard(tabControl) != null
            || GetTransitionMode(tabControl) != TransitionMode.Custom;
    }

    private static void EnableTransition(TabControl tabControl)
    {
        if (GetHasSnapshot(tabControl))
            return;

        CaptureOriginalContentState(tabControl);
        ApplyTransitionTemplate(tabControl);
    }

    private static void DisableTransition(TabControl tabControl)
    {
        if (!GetHasSnapshot(tabControl))
            return;

        RestoreLocalValue(
            tabControl,
            TabControl.ContentTemplateProperty,
            GetOriginalContentTemplateLocalValue(tabControl)
        );
        RestoreLocalValue(
            tabControl,
            TabControl.ContentTemplateSelectorProperty,
            GetOriginalContentTemplateSelectorLocalValue(tabControl)
        );
        RestoreLocalValue(
            tabControl,
            TabControl.ContentStringFormatProperty,
            GetOriginalContentStringFormatLocalValue(tabControl)
        );

        SetOriginalContentTemplate(tabControl, null);
        SetOriginalContentTemplateSelector(tabControl, null);
        SetOriginalContentStringFormat(tabControl, null);
        SetOriginalContentTemplateLocalValue(tabControl, DependencyProperty.UnsetValue);
        SetOriginalContentTemplateSelectorLocalValue(tabControl, DependencyProperty.UnsetValue);
        SetOriginalContentStringFormatLocalValue(tabControl, DependencyProperty.UnsetValue);
        SetHasSnapshot(tabControl, false);
    }

    private static void CaptureOriginalContentState(TabControl tabControl)
    {
        SetOriginalContentTemplate(tabControl, tabControl.ContentTemplate);
        SetOriginalContentTemplateSelector(tabControl, tabControl.ContentTemplateSelector);
        SetOriginalContentStringFormat(tabControl, tabControl.ContentStringFormat);
        SetOriginalContentTemplateLocalValue(
            tabControl,
            tabControl.ReadLocalValue(TabControl.ContentTemplateProperty)
        );
        SetOriginalContentTemplateSelectorLocalValue(
            tabControl,
            tabControl.ReadLocalValue(TabControl.ContentTemplateSelectorProperty)
        );
        SetOriginalContentStringFormatLocalValue(
            tabControl,
            tabControl.ReadLocalValue(TabControl.ContentStringFormatProperty)
        );
        SetHasSnapshot(tabControl, true);
    }

    private static void ApplyTransitionTemplate(TabControl tabControl)
    {
        tabControl.ContentTemplate = TransitionContentTemplate;
        tabControl.ContentTemplateSelector = null;
        tabControl.ContentStringFormat = null;
    }

    private static void RestoreLocalValue(
        DependencyObject target,
        DependencyProperty property,
        object value
    )
    {
        if (ReferenceEquals(value, DependencyProperty.UnsetValue))
        {
            target.ClearValue(property);
            return;
        }

        target.SetValue(property, value);
    }

    private static DataTemplate CreateTransitionContentTemplate()
    {
        var transitionControlFactory = new FrameworkElementFactory(typeof(TransitioningContentControl));

        // Keep TabItem content and data context aligned with TabControl's native behavior.
        transitionControlFactory.SetBinding(
            FrameworkElement.DataContextProperty,
            CreateTabControlBinding(TabControl.DataContextProperty)
        );
        transitionControlFactory.SetBinding(
            ContentControl.ContentProperty,
            CreateTabControlBinding(TabControl.SelectedContentProperty)
        );
        transitionControlFactory.SetBinding(
            TransitioningContentControl.TransitionModeProperty,
            CreateTabControlBinding(TransitionModeProperty)
        );
        transitionControlFactory.SetBinding(
            TransitioningContentControl.TransitionStoryboardProperty,
            CreateTabControlBinding(TransitionStoryboardProperty)
        );
        transitionControlFactory.SetBinding(
            ContentControl.ContentTemplateProperty,
            CreateTabControlBinding(OriginalContentTemplateProperty)
        );
        transitionControlFactory.SetBinding(
            ContentControl.ContentTemplateSelectorProperty,
            CreateTabControlBinding(OriginalContentTemplateSelectorProperty)
        );
        transitionControlFactory.SetBinding(
            ContentControl.ContentStringFormatProperty,
            CreateTabControlBinding(OriginalContentStringFormatProperty)
        );

        return new DataTemplate { VisualTree = transitionControlFactory };
    }

    private static Binding CreateTabControlBinding(DependencyProperty property)
    {
        return new Binding
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(TabControl), 1),
            Path = new PropertyPath("(0)", property)
        };
    }
}
