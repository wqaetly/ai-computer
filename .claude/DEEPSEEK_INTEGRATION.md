# DeepSeek AI 集成文档

## 功能概述

本项目已成功集成 DeepSeek AI API，提供以下核心功能：

### ✨ 主要特性

1. **深度思考模式**
   - 使用 `deepseek-reasoner` 模型
   - 显示 AI 的推理过程（reasoning content）
   - 支持思考内容的展开/收起

2. **流式输出**
   - 实时显示 AI 的回答
   - 分别处理推理内容和回答内容
   - 流畅的打字机效果

3. **现代化 UI**
   - 基于 SukiUI 的美观界面
   - 支持亮色/暗色主题
   - 玻璃卡片效果
   - 响应式布局

4. **用户体验**
   - 思考内容自动收起
   - 可点击按钮展开查看思考过程
   - 流式传输进度指示器
   - 支持停止生成功能

## 项目结构

```
ai-computer/
├── Models/
│   └── ChatMessage.cs              # 聊天消息模型（含推理内容）
├── Services/
│   └── DeepSeekService.cs          # DeepSeek API 服务
├── ViewModels/
│   ├── AiChatViewModel.cs          # AI 聊天 ViewModel
│   └── MainWindowViewModel.cs      # 主窗口 ViewModel
├── Views/
│   ├── AiChatView.axaml            # AI 聊天视图
│   ├── AiChatView.axaml.cs         # 视图代码后置
│   └── MainWindow.axaml            # 主窗口
├── Converters/
│   └── ReasoningButtonConverter.cs # 思考按钮文本转换器
└── App.axaml                       # 应用程序资源
```

## 技术实现细节

### 1. DeepSeek API 服务（Services/DeepSeekService.cs）

#### 核心功能
- **流式调用**：使用 SSE (Server-Sent Events) 协议
- **异步处理**：完全异步的 API 调用
- **错误处理**：JSON 解析异常处理
- **取消支持**：支持 CancellationToken

#### 关键代码
```csharp
public async Task ChatCompletionStreamAsync(
    List<ChatMessage> messages,
    Action<string> onReasoningChunk,  // 推理内容回调
    Action<string> onContentChunk,    // 回答内容回调
    CancellationToken cancellationToken = default)
```

#### API 配置
- **API 端点**：`https://api.deepseek.com/chat/completions`
- **模型**：`deepseek-reasoner`
- **流式模式**：`stream: true`

### 2. 消息模型（Models/ChatMessage.cs）

#### 属性
- `Content`：回答内容
- `ReasoningContent`：推理内容（思考过程）
- `Role`：消息角色（System/User/Assistant）
- `IsStreaming`：是否正在接收流式内容
- `IsReasoningExpanded`：思考内容是否展开
- `HasReasoning`：是否有推理内容

#### 特点
- 继承自 `ObservableObject`，支持 MVVM 绑定
- 使用 `[ObservableProperty]` 自动生成属性
- 实时更新 UI

### 3. ViewModel（ViewModels/AiChatViewModel.cs）

#### 核心命令
- `SendMessageCommand`：发送消息
- `StopGenerationCommand`：停止生成
- `ClearMessagesCommand`：清空对话
- `ToggleReasoningCommand`：切换思考内容展开/收起

#### 流式内容处理
```csharp
await _deepSeekService.ChatCompletionStreamAsync(
    messageHistory,
    reasoningChunk =>
    {
        // 在 UI 线程更新推理内容
        Dispatcher.UIThread.Post(() =>
        {
            assistantMsg.ReasoningContent += reasoningChunk;
        });
    },
    contentChunk =>
    {
        // 在 UI 线程更新回答内容
        Dispatcher.UIThread.Post(() =>
        {
            assistantMsg.Content += contentChunk;
        });
    },
    _cancellationTokenSource.Token
);
```

### 4. 用户界面（Views/AiChatView.axaml）

#### 布局结构
```
Grid (3行)
├── 标题栏（Row 0）
│   ├── 标题和副标题
│   └── 清空对话按钮
├── 消息列表（Row 1）
│   ├── 用户消息（右对齐，蓝色气泡）
│   └── AI 消息（左对齐，玻璃卡片）
│       ├── 推理内容区域（可展开/收起）
│       ├── 回答内容
│       └── 流式传输指示器
└── 输入框（Row 2）
    ├── 多行文本输入
    ├── 停止按钮（仅在生成时显示）
    └── 发送按钮
```

#### 关键特性
- **推理内容展开/收起**：使用 `IsReasoningExpanded` 绑定
- **条件显示**：用户消息和 AI 消息通过 `IsVisible` 绑定
- **快捷键**：`Ctrl+Enter` 发送消息
- **最大高度**：思考内容最大高度 300px，支持滚动

## 使用说明

### 运行应用

```bash
cd I:\ai-computer\ai-computer
dotnet run
```

### 功能测试

1. **基础对话**
   - 在输入框输入问题
   - 点击"发送"或按 `Ctrl+Enter`
   - 观察 AI 的实时回答

2. **深度思考**
   - 提出需要推理的问题（如数学题、逻辑题）
   - AI 会先进行思考（reasoning）
   - 点击"🧠 查看思考过程"按钮展开思考内容
   - 再次点击"🧠 收起思考过程"隐藏

3. **流式输出**
   - 发送消息后观察打字机效果
   - AI 的推理和回答会实时显示
   - 可点击"停止"按钮中断生成

4. **清空对话**
   - 点击右上角"清空对话"按钮
   - 会重置对话并显示欢迎消息

### 示例问题

#### 测试推理能力
```
9.11 和 9.8 哪个更大？
```

#### 测试深度思考
```
如何用三步将 [3,1,4,2] 排序成 [1,2,3,4]？
```

#### 测试长文本生成
```
请详细解释什么是深度学习，包括原理和应用。
```

## API 配置

### 当前配置
- **API Key**：`sk-e8ec7e0c860d4b7d98ffc4212ab2c138`
- **位置**：`AiChatViewModel.cs:35`

### 更改 API Key
在 `ViewModels/AiChatViewModel.cs` 中修改：
```csharp
public AiChatViewModel()
{
    _deepSeekService = new DeepSeekService("你的API-Key");
}
```

### 更改模型
在 `Services/DeepSeekService.cs` 中修改：
```csharp
var requestBody = new
{
    model = "deepseek-chat", // 或其他模型
    messages = ...,
    stream = true
};
```

## 主题切换

应用支持 SukiUI 主题系统：

### 在 App.axaml 中修改主题颜色
```xml
<suki:SukiTheme ThemeColor="Blue" />
<!-- 可选：Red, Orange, Yellow, Green, Blue, Purple, Pink -->
```

### 切换亮色/暗色模式
```xml
<Application RequestedThemeVariant="Dark">
<!-- 可选：Light, Dark, Default（跟随系统） -->
```

## 性能优化

1. **编译绑定**：项目已启用 `AvaloniaUseCompiledBindingsByDefault`
2. **异步操作**：所有 API 调用都是异步的
3. **UI 线程调度**：使用 `Dispatcher.UIThread.Post()` 更新 UI
4. **取消令牌**：支持取消长时间运行的操作

## 已知问题和改进建议

### 当前状态
✅ 深度思考功能完整实现
✅ 流式输出正常工作
✅ UI 响应流畅
✅ 思考内容展开/收起功能正常

### 未来改进
- [ ] 添加对话历史持久化
- [ ] 支持多会话管理
- [ ] 添加代码高亮显示
- [ ] 支持 Markdown 渲染
- [ ] 添加语音输入/输出
- [ ] 实现对话导出功能

## 故障排查

### 编译错误
```bash
# 如果遇到文件被锁定的问题
powershell -Command "Get-Process dotnet | Stop-Process -Force"
dotnet clean
dotnet build
```

### API 调用失败
- 检查 API Key 是否正确
- 检查网络连接
- 查看控制台日志获取详细错误信息

### UI 不更新
- 确保使用 `Dispatcher.UIThread.Post()` 更新 UI 属性
- 检查数据绑定的 `x:DataType` 是否正确

## 技术栈总结

- **UI 框架**：Avalonia 11.3.6
- **UI 库**：SukiUI 6.0.4
- **MVVM 工具**：CommunityToolkit.Mvvm 8.2.1
- **目标框架**：.NET 9.0
- **HTTP 客户端**：System.Net.Http
- **JSON 处理**：System.Text.Json

## 联系和支持

如有问题或建议，请在项目中提交 Issue。
