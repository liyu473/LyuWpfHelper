using System.Windows.Markup;

// 将所有命名空间映射到统一的 XML 命名空间
[assembly: XmlnsDefinition("http://schemas.lyuwpfhelper.com/winfx/xaml", "LyuWpfHelper.Panels")]
[assembly: XmlnsDefinition("http://schemas.lyuwpfhelper.com/winfx/xaml", "LyuWpfHelper.Behaviors")]
[assembly: XmlnsDefinition("http://schemas.lyuwpfhelper.com/winfx/xaml", "LyuWpfHelper.Controls")]
[assembly: XmlnsDefinition("http://schemas.lyuwpfhelper.com/winfx/xaml", "LyuWpfHelper.Converters")]
[assembly: XmlnsDefinition("http://schemas.lyuwpfhelper.com/winfx/xaml", "LyuWpfHelper.ViewModels")]

// 添加一个更短的别名
[assembly: XmlnsPrefix("http://schemas.lyuwpfhelper.com/winfx/xaml", "lyu")]
