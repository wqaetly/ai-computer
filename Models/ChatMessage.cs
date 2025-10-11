using System;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveMarkdown.Avalonia;

namespace AiComputer.Models;

/// <summary>
/// 聊天消息角色枚举
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// 系统消息
    /// </summary>
    System,

    /// <summary>
    /// 用户消息
    /// </summary>
    User,

    /// <summary>
    /// AI助手消息
    /// </summary>
    Assistant
}

/// <summary>
/// AI 消息状态枚举
/// </summary>
public enum AiMessageStatus
{
    /// <summary>
    /// 等待响应中
    /// </summary>
    Waiting,

    /// <summary>
    /// 思考中
    /// </summary>
    Thinking,

    /// <summary>
    /// 输出中
    /// </summary>
    Generating,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled,

    /// <summary>
    /// 错误
    /// </summary>
    Error
}

/// <summary>
/// 聊天消息模型
/// </summary>
public partial class ChatMessage : ObservableObject
{
    /// <summary>
    /// 消息内容
    /// </summary>
    [ObservableProperty]
    private string _content = string.Empty;

    /// <summary>
    /// 推理内容（深度思考过程）
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReasoning))]
    private string _reasoningContent = string.Empty;

    /// <summary>
    /// 内容构建器（用于实时 Markdown 渲染）
    /// </summary>
    public ObservableStringBuilder ContentBuilder { get; } = new();

    /// <summary>
    /// 推理内容构建器（用于实时 Markdown 渲染）
    /// </summary>
    public ObservableStringBuilder ReasoningContentBuilder { get; } = new();

    /// <summary>
    /// 消息角色
    /// </summary>
    public MessageRole Role { get; set; }

    /// <summary>
    /// 消息发送时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否正在接收流式内容
    /// </summary>
    [ObservableProperty]
    private bool _isStreaming;

    /// <summary>
    /// 思考内容是否展开
    /// </summary>
    [ObservableProperty]
    private bool _isReasoningExpanded;

    /// <summary>
    /// AI 消息状态
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(ShowStatus))]
    private AiMessageStatus _status = AiMessageStatus.Waiting;

    /// <summary>
    /// 是否有推理内容
    /// </summary>
    public bool HasReasoning => !string.IsNullOrWhiteSpace(ReasoningContent) || ReasoningContentBuilder.Length > 0;

    /// <summary>
    /// 是否为用户消息
    /// </summary>
    public bool IsUser => Role == MessageRole.User;

    /// <summary>
    /// 是否为助手消息
    /// </summary>
    public bool IsAssistant => Role == MessageRole.Assistant;

    /// <summary>
    /// 状态文本
    /// </summary>
    public string StatusText => Status switch
    {
        AiMessageStatus.Waiting => "等待响应中...",
        AiMessageStatus.Thinking => "思考中...",
        AiMessageStatus.Generating => "输出中...",
        AiMessageStatus.Completed => "已完成",
        AiMessageStatus.Cancelled => "已取消",
        AiMessageStatus.Error => "发生错误",
        _ => string.Empty
    };

    /// <summary>
    /// 是否显示状态（非用户消息且正在处理中）
    /// </summary>
    public bool ShowStatus => IsAssistant && Status != AiMessageStatus.Completed;
}
