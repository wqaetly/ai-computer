# AI Computer - Avalonia 应用开发指南

## 项目概述

本项目是一个基于 Avalonia UI 框架的跨平台桌面应用程序，使用 MVVM 架构模式，采用 JetBrains Rider 作为主要开发工具。

### 技术栈
- **UI 框架**: Avalonia 11.3.6
- **目标框架**: .NET 9.0
- **架构模式**: MVVM (Model-View-ViewModel)
- **MVVM 工具包**: CommunityToolkit.Mvvm 8.2.1
- **主题**: Fluent Design
- **开发工具**: JetBrains Rider

## 项目结构

```
ai-computer/
├── Models/           # 数据模型和业务逻辑
├── ViewModels/       # 视图模型层
│   ├── ViewModelBase.cs       # ViewModel 基类
│   └── MainWindowViewModel.cs # 主窗口 ViewModel
├── Views/            # XAML 视图层
│   ├── MainWindow.axaml       # 主窗口 XAML
│   └── MainWindow.axaml.cs    # 主窗口代码后置
├── Assets/           # 静态资源（图片、图标等）
├── App.axaml         # 应用程序 XAML（主题、样式）
├── App.axaml.cs      # 应用程序启动逻辑
├── Program.cs        # 程序入口点
└── ViewLocator.cs    # 视图定位器（ViewModel 到 View 映射）
```

## Avalonia 开发规范

### 1. MVVM 模式最佳实践

#### ViewModel 实现
```csharp
// 使用 CommunityToolkit.Mvvm 的特性简化 ViewModel
[ObservableObject]
public partial class MyViewModel : ViewModelBase
{
    // 使用 [ObservableProperty] 自动生成属性
    [ObservableProperty]
    private string _title = string.Empty;

    // 使用 [RelayCommand] 自动生成命令
    [RelayCommand]
    private void DoSomething()
    {
        // 命令逻辑
    }

    // 带参数的命令
    [RelayCommand]
    private async Task LoadDataAsync(string parameter)
    {
        // 异步命令逻辑
    }
}
```

#### 数据绑定规范
```xml
<!-- 使用编译绑定（已在项目中启用 AvaloniaUseCompiledBindingsByDefault） -->
<Window xmlns="https://github.com/avaloniaui"
        x:Class="AiComputer.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel">

    <!-- 文本绑定 -->
    <TextBlock Text="{Binding Title}" />

    <!-- 命令绑定 -->
    <Button Content="执行" Command="{Binding DoSomethingCommand}" />

    <!-- 双向绑定 -->
    <TextBox Text="{Binding InputText, Mode=TwoWay}" />
</Window>
```

### 2. XAML 编码规范

#### 命名空间和引用
```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:vm="using:AiComputer.ViewModels"
        mc:Ignorable="d"
        d:DesignWidth="800"
        d:DesignHeight="450"
        x:Class="AiComputer.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel">
</Window>
```

#### 设计时数据上下文
```xml
<!-- 为设计器提供数据上下文，方便预览 -->
<Design.DataContext>
    <vm:MainWindowViewModel />
</Design.DataContext>
```

### 3. 样式和主题

#### 定义样式
```xml
<!-- 在 App.axaml 中定义全局样式 -->
<Application.Styles>
    <FluentTheme />

    <!-- 自定义样式 -->
    <Style Selector="Button.primary">
        <Setter Property="Background" Value="#0078D4" />
        <Setter Property="Foreground" Value="White" />
    </Style>
</Application.Styles>
```

#### 使用样式类
```xml
<!-- 在视图中应用样式类 -->
<Button Classes="primary" Content="主要按钮" />

<!-- 条件样式类（基于绑定） -->
<Button Classes.accent="{Binding IsSpecial}" Content="按钮" />
```

### 4. 控件和布局

#### 常用布局容器
```xml
<!-- StackPanel - 垂直或水平堆叠 -->
<StackPanel Orientation="Vertical" Spacing="10">
    <TextBlock Text="项目 1" />
    <TextBlock Text="项目 2" />
</StackPanel>

<!-- Grid - 网格布局 -->
<Grid ColumnDefinitions="Auto,*" RowDefinitions="Auto,*">
    <TextBlock Grid.Row="0" Grid.Column="0" Text="标签:" />
    <TextBox Grid.Row="0" Grid.Column="1" />
</Grid>

<!-- DockPanel - 停靠布局 -->
<DockPanel>
    <Menu DockPanel.Dock="Top" />
    <StatusBar DockPanel.Dock="Bottom" />
    <ContentControl />
</DockPanel>
```

#### 数据模板
```xml
<ListBox ItemsSource="{Binding Items}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal" Spacing="10">
                <TextBlock Text="{Binding Name}" FontWeight="Bold" />
                <TextBlock Text="{Binding Description}" />
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### 5. 响应式编程（ReactiveUI）

虽然项目使用 CommunityToolkit.Mvvm，但 Avalonia 与 ReactiveUI 深度集成：

```csharp
// ViewModelBase 继承自 ReactiveObject
public class ViewModelBase : ReactiveObject
{
}

// 使用 ReactiveCommand
public class MyViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    public MyViewModel()
    {
        // 创建带条件的命令
        var canSave = this.WhenAnyValue(
            x => x.Name,
            x => !string.IsNullOrWhiteSpace(x));

        SaveCommand = ReactiveCommand.Create(Save, canSave);
    }

    private void Save()
    {
        // 保存逻辑
    }
}
```

## JetBrains Rider 配置建议

### 1. 推荐插件
- Avalonia for Rider
- XAML Styler

### 2. 代码格式化
- C# 文件：使用 Rider 默认的 C# 代码风格
- XAML 文件：保持一致的缩进和属性排序

### 3. 快捷操作
- `Alt + Enter`: 快速修复和重构
- `Ctrl + Space`: IntelliSense 代码补全
- `F12`: 转到定义
- `Shift + F12`: 查找所有引用

## 开发工作流

### 1. 创建新视图
1. 在 `Views` 文件夹中创建 `.axaml` 文件
2. 在 `ViewModels` 文件夹中创建对应的 ViewModel
3. 在 XAML 中设置 `x:DataType` 指向 ViewModel
4. 在 `ViewLocator.cs` 中注册映射关系（如果需要）

### 2. 添加新功能
1. 在 Model 层定义数据结构
2. 在 ViewModel 中实现业务逻辑和命令
3. 在 View (XAML) 中绑定数据和命令
4. 测试功能

### 3. 样式和主题
- 全局样式定义在 `App.axaml`
- 控件特定样式可以定义在视图的 `Window.Styles` 或 `UserControl.Styles` 中

## 调试和测试

### 调试工具
- Avalonia DevTools: 在 Debug 模式下按 `F12` 打开
- 可以实时查看可视化树、样式、绑定状态

### 热重载
- Avalonia 支持 XAML 热重载
- Rider 中保存 XAML 文件后会自动更新运行中的应用

## 性能优化建议

1. **使用编译绑定**: 项目已启用 `AvaloniaUseCompiledBindingsByDefault`，确保所有绑定都指定 `x:DataType`
2. **虚拟化**: 对于长列表使用 `VirtualizingStackPanel`
3. **异步操作**: 耗时操作使用 `async/await`
4. **资源管理**: 及时释放资源，实现 `IDisposable`

## 常见模式和示例

### 对话框
```csharp
// ViewModel 中
[RelayCommand]
private async Task ShowDialogAsync()
{
    var dialog = new MyDialog
    {
        DataContext = new MyDialogViewModel()
    };

    var result = await dialog.ShowDialog<bool>(MainWindow);
    if (result)
    {
        // 处理确认操作
    }
}
```

### 导航
```csharp
// ViewModel 中管理当前视图
[ObservableProperty]
private ViewModelBase _currentView;

[RelayCommand]
private void NavigateToSettings()
{
    CurrentView = new SettingsViewModel();
}
```

## 资源和文档

- [Avalonia 官方文档](https://docs.avaloniaui.net/)
- [Avalonia GitHub](https://github.com/AvaloniaUI/Avalonia)
- [CommunityToolkit.Mvvm 文档](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [Fluent Design System](https://www.microsoft.com/design/fluent/)

## 注意事项

1. **空安全**: 项目启用了 `Nullable` 引用类型，注意处理可空性
2. **跨平台**: 避免使用平台特定 API，或使用条件编译
3. **资源路径**: 使用 `avares://` 协议引用嵌入资源
4. **绑定错误**: 开启 Avalonia 日志查看绑定错误

## 编码规范总结

- 遵循 C# 命名约定（PascalCase for 类型和方法，camelCase for 私有字段）
- XAML 属性按字母顺序或逻辑分组排列
- 每个 ViewModel 对应一个 View
- 使用异步方法处理 I/O 和长时间运行的操作
- 保持 ViewModel 的可测试性，避免直接引用 UI 控件
