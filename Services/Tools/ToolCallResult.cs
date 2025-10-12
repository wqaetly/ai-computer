using System.Text.Json;

namespace AiComputer.Services.Tools;

/// <summary>
/// 工具调用解析结果
/// </summary>
public class ToolCall
{
    /// <summary>
    /// 工具调用ID（自动生成）
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 工具名称
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// 工具参数（JSON对象）
    /// </summary>
    public JsonElement Arguments { get; set; }

    /// <summary>
    /// 调用状态
    /// </summary>
    public ToolCallStatus Status { get; set; } = ToolCallStatus.Pending;
}

/// <summary>
/// 工具调用状态
/// </summary>
public enum ToolCallStatus
{
    /// <summary>
    /// 等待执行
    /// </summary>
    Pending,

    /// <summary>
    /// 执行中
    /// </summary>
    Executing,

    /// <summary>
    /// 执行成功
    /// </summary>
    Success,

    /// <summary>
    /// 执行失败
    /// </summary>
    Failed
}

/// <summary>
/// 工具执行结果
/// </summary>
public class ToolExecutionResult
{
    /// <summary>
    /// 工具调用ID
    /// </summary>
    public string ToolCallId { get; set; } = string.Empty;

    /// <summary>
    /// 工具名称
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// 执行结果（成功时）
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// 错误信息（失败时）
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 是否执行出错
    /// </summary>
    public bool IsError => !string.IsNullOrEmpty(Error);
}
