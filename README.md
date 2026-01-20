# LyuWpfHelper
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)



## 🚀 快速开始

在 XAML 中引入命名空间：

```xml
<Window xmlns:lyu="http://schemas.lyuwpfhelper.com/winfx/xaml">
    <!-- 使用所有控件、转换器和行为 -->
</Window>
```

## 📚 功能列表

### 📦 控件 (Controls)

#### SelectableTextBlock
可选择和复制文本的 TextBlock，比普通 TextBlock 更实用。

```xml
<lyu:SelectableTextBlock Text="用户可以选中并复制这段文本" />
```

**特性：**
- 支持文本选择和复制
- 保持 TextBlock 的轻量级特性
- 完美替代只读 TextBox

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

### 🛠️ 辅助类 (Helpers)

#### GridHelper
Grid 辅助类，提供简化的行列定义语法，让 Grid 布局更简洁易读。

**基本用法：**
```xml
<!-- 传统方式 -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
        <RowDefinition Height="2*"/>
        <RowDefinition Height="100"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
</Grid>

<!-- 使用 GridHelper 简化 -->
<Grid lyu:GridHelper.RowDefinitions="Auto,*,2*,100"
      lyu:GridHelper.ColumnDefinitions="Auto,*">
    <!-- 内容 -->
</Grid>
```

**支持的格式：**
- `Auto` - 自动大小（GridLength.Auto）
- `*` - 比例大小 1*（GridLength(1, Star)）
- `2*` - 比例大小 2*（GridLength(2, Star)）
- `100` - 固定像素值（GridLength(100, Pixel)）

**实际示例：**
```xml
<!-- 经典的头部-内容-底部布局 -->
<Grid lyu:GridHelper.RowDefinitions="Auto,*,Auto">
    <TextBlock Grid.Row="0" Text="头部"/>
    <ScrollViewer Grid.Row="1">
        <!-- 主要内容 -->
    </ScrollViewer>
    <StackPanel Grid.Row="2">
        <!-- 底部按钮 -->
    </StackPanel>
</Grid>

<!-- 侧边栏布局 -->
<Grid lyu:GridHelper.ColumnDefinitions="200,*">
    <ListBox Grid.Column="0"/>
    <ContentControl Grid.Column="1"/>
</Grid>

<!-- 复杂布局 -->
<Grid lyu:GridHelper.RowDefinitions="50,*,2*,Auto"
      lyu:GridHelper.ColumnDefinitions="Auto,*,200">
    <!-- 网格内容 -->
</Grid>
```

**优势：**
- 代码更简洁，减少 XAML 冗余
- 一行定义多个行/列
- 易于阅读和维护
- 支持所有标准 GridLength 类型

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

#### DataGridRowNumberBehavior
DataGrid 行序号辅助，自动为每一行生成序号，并在数据变化时自动刷新。

```xml
<!-- 基本用法 -->
<DataGrid ItemsSource="{Binding Items}"
          lyu:DataGridRowNumberBehavior.IsEnabled="True" />

<!-- 自定义序号列标题 -->
<DataGrid ItemsSource="{Binding Items}"
          lyu:DataGridRowNumberBehavior.IsEnabled="True"
          lyu:DataGridRowNumberBehavior.HeaderText="No." />

<!-- 从0开始计数 -->
<DataGrid ItemsSource="{Binding Items}"
          lyu:DataGridRowNumberBehavior.IsEnabled="True"
          lyu:DataGridRowNumberBehavior.StartIndex="0" />
```

**属性：**
- `IsEnabled` - 是否启用行序号（默认：false）
- `HeaderText` - 序号列标题（默认："序号"）
- `StartIndex` - 序号起始值（默认：1）

**特性：**
- 自动为每一行生成序号
- 数据源变化时自动刷新序号
- 支持自定义起始索引
- 支持自定义列标题
- **自动启用行头显示**（无需手动设置 HeadersVisibility）

**注意：**
序号显示在 DataGrid 的行头（RowHeader）中。启用此行为后，会自动设置 `HeadersVisibility` 属性以显示行头。

### 🏗️ ViewModel 基类 (ViewModels)

#### ViewModelBase
集成了 Messenger 信使功能的 ViewModel 基类，继承自 CommunityToolkit.Mvvm 的 ObservableObject。

**基本用法：**
```csharp
using LyuWpfHelper.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        // 注册消息接收器
        Register<UserLoginMessage>(OnUserLogin);
    }

    private void OnUserLogin(object recipient, UserLoginMessage message)
    {
        // 处理用户登录消息
        Console.WriteLine($"用户 {message.UserName} 已登录");
    }

    public void Login(string userName)
    {
        // 发送消息
        Send(new UserLoginMessage(userName));
    }
}

// 消息定义
public record UserLoginMessage(string UserName);
```

**高级用法 - 频道消息：**
```csharp
public class ChatViewModel : ViewModelBase
{
    public ChatViewModel()
    {
        // 注册指定频道的消息
        Register<ChatMessage, string>("Channel1", OnChatMessage);
    }

    private void OnChatMessage(object recipient, ChatMessage message)
    {
        // 处理聊天消息
    }

    public void SendMessage(string content)
    {
        // 发送到指定频道
        Send(new ChatMessage(content), "Channel1");
    }
}
```

**可用方法：**
- `Send<TMessage>(message)` - 发送消息
- `Send<TMessage, TToken>(message, token)` - 发送消息到指定频道
- `Register<TMessage>(handler)` - 注册消息接收器
- `Register<TMessage, TToken>(token, handler)` - 注册指定频道的消息接收器
- `Unregister<TMessage>()` - 取消注册指定类型的消息
- `UnregisterAll()` - 取消注册所有消息

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
