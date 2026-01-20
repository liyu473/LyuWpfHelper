# LyuWpfHelper

一个功能丰富的 WPF 辅助库，提供常用的控件、行为、面板和值转换器，帮助你更高效地开发 WPF 应用程序。

## 特性

- ✅ **统一命名空间** - 只需引入一次命名空间即可访问所有功能
- ✅ **多目标框架** - 支持 .NET 8.0、9.0、10.0
- ✅ **开箱即用** - 无需额外配置，直接使用
- ✅ **MVVM 友好** - 完美支持 MVVM 模式

## 安装

```bash
# 通过 NuGet 安装（待发布）
Install-Package LyuWpfHelper
```

或者直接引用项目 DLL。

## 快速开始

在 XAML 中引入命名空间：

```xml
<Window xmlns:lyu="http://schemas.lyuwpfhelper.com/winfx/xaml">
    <!-- 使用所有控件和转换器 -->
</Window>
```

## 功能列表

### 📦 控件 (Controls)

#### SelectableTextBlock
可选择和复制文本的 TextBlock，比普通 TextBlock 更实用。

```xml
<lyu:SelectableTextBlock Text="用户可以选中并复制这段文本" />
```

#### CopyableTextBox
带一键复制按钮的文本框，支持复制成功提示。

```xml
<lyu:CopyableTextBox Text="点击按钮快速复制" 
                     CopyButtonText="复制"
                     CopiedText="已复制" />
```

**属性：**
- `Text` - 文本内容
- `IsReadOnly` - 是否只读（默认：true）
- `CopyButtonText` - 复制按钮文本（默认："复制"）
- `CopiedText` - 复制成功提示文本（默认："已复制"）

### 🎨 面板 (Panels)

#### SimpleStackPanel
支持间距的堆叠面板，比原生 StackPanel 更灵活。

```xml
<lyu:SimpleStackPanel Orientation="Vertical" Spacing="10">
    <Button Content="按钮 1"/>
    <Button Content="按钮 2"/>
    <Button Content="按钮 3"/>
</lyu:SimpleStackPanel>
```

**属性：**
- `Orientation` - 方向（Vertical/Horizontal）
- `Spacing` - 子元素间距

### 🎯 行为 (Behaviors)

#### DataGridSelectedItemsBehavior
DataGrid 多选绑定辅助，完美支持 MVVM 模式。

```xml
<DataGrid ItemsSource="{Binding AllItems}"
          SelectionMode="Extended"
          lyu:DataGridSelectedItemsBehavior.SelectedItems="{Binding SelectedItems}" />
```

**ViewModel 示例：**
```csharp
public class MyViewModel
{
    public ObservableCollection<Item> AllItems { get; set; }
    public ObservableCollection<Item> SelectedItems { get; set; }
}
```

### 🔄 转换器 (Converters)

#### BooleanToVisibilityConverter
布尔值到可见性转换器。

```xml
<!-- True -> Visible, False -> Collapsed -->
<TextBlock Visibility="{Binding IsVisible, Converter={lyu:BooleanToVisibilityConverter}}" />

<!-- 反转逻辑：True -> Collapsed, False -> Visible -->
<TextBlock Visibility="{Binding IsHidden, Converter={lyu:BooleanToVisibilityConverter IsInverted=True}}" />

<!-- 使用 Hidden 而不是 Collapsed -->
<TextBlock Visibility="{Binding IsVisible, Converter={lyu:BooleanToVisibilityConverter UseHidden=True}}" />
```

#### InverseBooleanConverter
布尔值取反转换器。

```xml
<CheckBox IsEnabled="{Binding IsDisabled, Converter={lyu:InverseBooleanConverter}}" />
```

#### NullToVisibilityConverter
空值到可见性转换器。

```xml
<!-- 有值时显示，空值时隐藏 -->
<TextBlock Text="{Binding UserName}" 
           Visibility="{Binding UserName, Converter={lyu:NullToVisibilityConverter}}" />

<!-- 反转：空值时显示 -->
<TextBlock Text="暂无数据" 
           Visibility="{Binding Data, Converter={lyu:NullToVisibilityConverter IsInverted=True}}" />
```

#### StringEmptyToVisibilityConverter
字符串空值到可见性转换器。

```xml
<!-- 字符串非空时显示 -->
<TextBlock Text="{Binding Message}" 
           Visibility="{Binding Message, Converter={lyu:StringEmptyToVisibilityConverter}}" />

<!-- 字符串为空时显示占位符 -->
<TextBlock Text="请输入内容" 
           Visibility="{Binding InputText, Converter={lyu:StringEmptyToVisibilityConverter IsInverted=True}}" />
```

#### MathConverter
数学运算转换器，支持加减乘除。

```xml
<!-- 宽度减去 20 -->
<Rectangle Width="{Binding ActualWidth, Converter={lyu:MathConverter}, ConverterParameter=-20}" />

<!-- 高度乘以 2 -->
<Rectangle Height="{Binding BaseHeight, Converter={lyu:MathConverter}, ConverterParameter=*2}" />

<!-- 值加 10 -->
<TextBlock FontSize="{Binding BaseFontSize, Converter={lyu:MathConverter}, ConverterParameter=+10}" />

<!-- 值除以 3 -->
<Grid Width="{Binding TotalWidth, Converter={lyu:MathConverter}, ConverterParameter=/3}" />
```

#### ColorToBrushConverter
颜色到画刷转换器。

```xml
<!-- 将 Color 转换为 Brush -->
<Rectangle Fill="{Binding ThemeColor, Converter={lyu:ColorToBrushConverter}}" />
```

#### CollectionElementIndexConverter
集合元素索引转换器，用于获取元素在集合中的位置。

```xml
<ItemsControl ItemsSource="{Binding Items}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <TextBlock>
                <TextBlock.Text>
                    <MultiBinding Converter="{lyu:CollectionElementIndexConverter}">
                        <Binding />
                        <Binding RelativeSource="{RelativeSource AncestorType=ItemsControl}" Path="ItemsSource" />
                    </MultiBinding>
                </TextBlock.Text>
            </TextBlock>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

**属性：**
- `ZeroBased` - 是否从0开始计数（默认：false，从1开始）

#### ValueConverterGroup
转换器组合，允许将多个转换器串联使用。

```xml
<TextBlock.Visibility>
    <Binding Path="Value">
        <Binding.Converter>
            <lyu:ValueConverterGroup>
                <lyu:MathConverter />
                <lyu:BooleanToVisibilityConverter />
            </lyu:ValueConverterGroup>
        </Binding.Converter>
    </Binding>
</TextBlock.Visibility>
```

## 项目结构

```
LyuWpfHelper/
├── Behaviors/              # 行为类
│   └── DataGridSelectedItemsBehavior.cs
├── Controls/               # 自定义控件
│   ├── CopyableTextBox.cs
│   └── SelectableTextBlock.cs
├── Converters/             # 值转换器
│   ├── BooleanToVisibilityConverter.cs
│   ├── InverseBooleanConverter.cs
│   ├── NullToVisibilityConverter.cs
│   ├── StringEmptyToVisibilityConverter.cs
│   ├── MathConverter.cs
│   ├── ColorToBrushConverter.cs
│   ├── CollectionElementIndexConverter.cs
│   └── ValueConverterGroup.cs
├── Panels/                 # 面板控件
│   └── SimpleStackPanel.cs
├── Themes/                 # 默认样式
│   └── Generic.xaml
└── XmlnsDefinition.cs      # 命名空间映射
```

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request！

## 更新日志

### v1.0.0
- 初始版本发布
- 添加基础控件、面板、行为和转换器
