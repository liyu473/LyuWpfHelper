using System.Windows;
using System.Windows.Controls;

namespace LyuWpfHelper.Controls;

/// <summary>
/// 可选择复制文本的 TextBlock
/// </summary>
public class SelectableTextBlock : TextBlock
{
    static SelectableTextBlock()
    {
        FocusableProperty.OverrideMetadata(typeof(SelectableTextBlock), new FrameworkPropertyMetadata(true));
        TextEditorWrapper.RegisterCommandHandlers(typeof(SelectableTextBlock), true, true, true);
    }

    private readonly TextEditorWrapper _editor;

    public SelectableTextBlock()
    {
        _editor = TextEditorWrapper.CreateFor(this);
    }
}

internal class TextEditorWrapper
{
    private static readonly Type TextEditorType = Type.GetType("System.Windows.Documents.TextEditor, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")!;
    private static readonly System.Reflection.PropertyInfo IsReadOnlyProp = TextEditorType.GetProperty("IsReadOnly", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
    private static readonly System.Reflection.PropertyInfo TextViewProp = TextEditorType.GetProperty("TextView", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
    private static readonly System.Reflection.MethodInfo RegisterMethod = TextEditorType.GetMethod("RegisterCommandHandlers", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, [typeof(Type), typeof(bool), typeof(bool), typeof(bool)], null)!;

    private readonly object _editor;

    private TextEditorWrapper(object editor)
    {
        _editor = editor;
    }

    public static void RegisterCommandHandlers(Type controlType, bool acceptsRichContent, bool readOnly, bool registerEventListeners)
    {
        RegisterMethod.Invoke(null, [controlType, acceptsRichContent, readOnly, registerEventListeners]);
    }

    public static TextEditorWrapper CreateFor(TextBlock textBlock)
    {
        var textContainer = textBlock.GetType()
            .GetProperty("TextContainer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(textBlock);

        var editor = Activator.CreateInstance(
            TextEditorType,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.CreateInstance,
            null,
            [textContainer, textBlock, false],
            null)!;

        IsReadOnlyProp.SetValue(editor, true);
        TextViewProp.SetValue(editor, textContainer!.GetType()
            .GetProperty("TextView", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(textContainer));

        return new TextEditorWrapper(editor);
    }
}
