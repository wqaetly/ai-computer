using System;

namespace AiComputer.Models;

/// <summary>
/// 聊天消息角色枚举
/// </summary>
public enum MessageRole
{
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
/// 聊天消息模型
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 消息角色
    /// </summary>
    public MessageRole Role { get; set; }

    /// <summary>
    /// 消息发送时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否为用户消息
    /// </summary>
    public bool IsUser => Role == MessageRole.User;

    /// <summary>
    /// 是否为助手消息
    /// </summary>
    public bool IsAssistant => Role == MessageRole.Assistant;
}
