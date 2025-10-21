using System;
using System.Collections.Generic;

namespace AiComputer.Models;

/// <summary>
/// 聊天存档模型 - 用于JSON序列化
/// </summary>
public class ChatArchive
{
    /// <summary>
    /// 存档版本号（用于未来兼容性）
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 存档创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 所有会话列表
    /// </summary>
    public List<ChatSessionData> Sessions { get; set; } = new();
}

/// <summary>
/// 会话数据（可序列化版本）
/// </summary>
public class ChatSessionData
{
    /// <summary>
    /// 会话 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 会话标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// 消息列表
    /// </summary>
    public List<ChatMessageData> Messages { get; set; } = new();
}

/// <summary>
/// 消息数据（可序列化版本）
/// </summary>
public class ChatMessageData
{
    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 推理内容
    /// </summary>
    public string ReasoningContent { get; set; } = string.Empty;

    /// <summary>
    /// 消息角色
    /// </summary>
    public MessageRole Role { get; set; }

    /// <summary>
    /// 消息发送时间
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 工具调用 ID
    /// </summary>
    public string? ToolCallId { get; set; }

    /// <summary>
    /// 工具名称
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// 工具参数
    /// </summary>
    public string? ToolArguments { get; set; }

    /// <summary>
    /// AI 消息状态
    /// </summary>
    public AiMessageStatus Status { get; set; }

    /// <summary>
    /// 思考内容是否展开
    /// </summary>
    public bool IsReasoningExpanded { get; set; }

    /// <summary>
    /// 搜索结果是否展开
    /// </summary>
    public bool IsSearchResultExpanded { get; set; } = false;
}
