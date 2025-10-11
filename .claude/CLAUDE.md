# AI Computer - Avalonia 应用开发指南

## 项目概述

本项目是一个基于 Avalonia UI 框架的跨平台桌面应用程序，使用 MVVM 架构模式，采用 JetBrains Rider 作为主要开发工具。

### 技术栈
- **UI 框架**: Avalonia 11.3.6
- **UI 库**: SukiUI 6.0.4-nightly20250930
- **目标框架**: .NET 9.0
- **架构模式**: MVVM (Model-View-ViewModel)
- **MVVM 工具包**: CommunityToolkit.Mvvm 8.2.1
- **主题**: SukiUI Theme (支持亮色/暗色主题切换)
- **开发工具**: JetBrains Rider

## 项目结构

```
ai-computer/
├── Models/           # 数据模型和业务逻辑
├── ViewModels/       # 视图模型层
├── Views/            # XAML 视图层
├── Assets/           # 静态资源（图片、图标等）
├── App.axaml         # 应用程序 XAML（主题、样式）
├── App.axaml.cs      # 应用程序启动逻辑
├── Program.cs        # 程序入口点
└── ViewLocator.cs    # 视图定位器（ViewModel 到 View 映射）
```

## 开发规范要点
判定为复杂问题，或者多次要求修复的问题，则尝试调用 sequential-thinking MCP来增强思考能力

### MVVM 模式
- ViewModel 使用 `[ObservableObject]` 特性标记
- 属性使用 `[ObservableProperty]` 自动生成
- 命令使用 `[RelayCommand]` 自动生成
- 所有绑定必须在 XAML 中指定 `x:DataType`（已启用编译绑定）
- 为设计器提供 `Design.DataContext` 方便预览

### XAML 编写
- 始终声明必需的命名空间（avaloniaui、x、d、mc、vm 等）
- 全局样式定义在 `App.axaml`
- 控件特定样式可定义在视图的 `Styles` 中
- 使用 `avares://` 协议引用嵌入资源

### ReactiveUI
- ViewModelBase 继承自 ReactiveObject
- 支持使用 ReactiveCommand 和 WhenAnyValue 进行响应式编程

## 开发工作流

### 创建新视图
1. 在 `Views` 文件夹创建 `.axaml` 文件
2. 在 `ViewModels` 文件夹创建对应 ViewModel
3. XAML 中设置 `x:DataType` 指向 ViewModel
4. 必要时在 `ViewLocator.cs` 中注册映射

### 添加新功能
1. Model 层定义数据结构
2. ViewModel 实现业务逻辑和命令
3. View (XAML) 绑定数据和命令
4. 测试功能

## 性能优化要点

- **编译绑定**: 所有绑定都指定 `x:DataType`
- **虚拟化**: 长列表使用 `VirtualizingStackPanel`
- **异步操作**: 耗时操作使用 `async/await`
- **资源管理**: 及时释放资源，实现 `IDisposable`

## SukiUI 使用指南

**重要**: 项目使用 SukiUI 作为主要 UI 库，提供现代化的控件和主题支持。

### 开发参考
- **官方示例代码**: `../SukiUI` 目录（开发时的主要参考）
- **使用文档**: `../SukiUI/docs`
- 在实现新功能时，应优先参考示例项目中的实现方式

### 示例内容包括
- 各种控件的使用方法
- 布局和样式最佳实践
- 主题切换实现
- 对话框和通知系统

## IconPacks.Avalonia 使用

项目已引入 IconPacks.Avalonia 图标库，包含几乎所有类型的图标，所以项目中所有用到图标的地方（除了自定义图片资源外）都应使用这个图标库里的图标。

- **使用文档**: `.claude/IconPacks_Avalonia_README.md`
- 提供多种图标包（Material Design、FontAwesome 等）

## JetBrains Rider 配置

### 推荐插件
- Avalonia for Rider
- XAML Styler

## 资源和文档

### 在线文档
- [Avalonia 官方文档](https://docs.avaloniaui.net/)
- [Avalonia GitHub](https://github.com/AvaloniaUI/Avalonia)
- [CommunityToolkit.Mvvm 文档](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [Fluent Design System](https://www.microsoft.com/design/fluent/)

### 本地资源
- **SukiUI 文档**: `../SukiUI/docs`
- **SukiUI 官方示例**: `../SukiUI` 目录
- **IconPacks.Avalonia 文档**: `.claude/IconPacks_Avalonia_README.md`

## 注意事项

- **空安全**: 项目启用了 `Nullable` 引用类型，注意处理可空性
- **跨平台**: 避免使用平台特定 API，或使用条件编译
- **资源路径**: 使用 `avares://` 协议引用嵌入资源
- **绑定错误**: 开启 Avalonia 日志查看绑定错误

## 编码规范

- 遵循 C# 命名约定（PascalCase for 类型和方法，camelCase for 私有字段）
- XAML 属性按字母顺序或逻辑分组排列
- 每个 ViewModel 对应一个 View
- 使用异步方法处理 I/O 和长时间运行的操作
- 保持 ViewModel 的可测试性，避免直接引用 UI 控件
