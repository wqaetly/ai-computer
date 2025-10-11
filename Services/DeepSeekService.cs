using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AiComputer.Models;

namespace AiComputer.Services;

/// <summary>
/// DeepSeek API 服务
/// </summary>
public class DeepSeekService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://api.deepseek.com";

    /// <summary>
    /// 系统提示词
    /// </summary>
    private const string SystemPrompt = @"你是""玄机鉴""AI分析师，负责解析电脑硬件配置，为用户提供性能评估、瓶颈提示与优化建议，以及根据用户需求直接提供详尽的装机配置单。

你具备联网搜索能力，当需要查询最新硬件信息、价格、性能测试数据时，可以自动联网获取实时信息。当需要查询硬件信息时：

1. **价格查询要求**：
   - 必须查询京东、淘宝的当前价格
   - 给出价格区间（最低价-最高价）
   - 标注价格来源和查询时间
   - 说明是否促销价格

2. 提供多个购买渠道的对比

以上内容要形成清晰、结构化的回答，最好能在每个标题前增加一些Markdown可用的表情，以增加用户体验。";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="apiKey">DeepSeek API Key</param>
    public DeepSeekService(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromMinutes(5)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    /// <summary>
    /// 流式聊天完成
    /// </summary>
    /// <param name="messages">消息历史</param>
    /// <param name="onReasoningChunk">推理内容回调</param>
    /// <param name="onContentChunk">回答内容回调</param>
    /// <param name="onToolCall">工具调用回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ChatCompletionStreamAsync(
        List<ChatMessage> messages,
        Action<string> onReasoningChunk,
        Action<string> onContentChunk,
        Func<string, string, Task<string>>? onToolCall = null,
        CancellationToken cancellationToken = default)
    {
        // 构造消息列表，自动在开头添加系统提示词
        var messageList = new List<object>
        {
            new { role = "system", content = SystemPrompt }
        };

        // 添加用户消息历史，根据消息类型和工具调用信息使用不同格式
        foreach (var m in messages)
        {
            var role = m.Role.ToString().ToLower();

            // 处理 assistant 消息（有 tool_calls）
            if (m.Role == MessageRole.Assistant && !string.IsNullOrEmpty(m.ToolCallId))
            {
                messageList.Add(new
                {
                    role = "assistant",
                    content = (string?)null, // tool_calls 时 content 可以为 null
                    tool_calls = new[]
                    {
                        new
                        {
                            id = m.ToolCallId,
                            type = "function",
                            function = new
                            {
                                name = m.ToolName,
                                arguments = JsonSerializer.Serialize(new { query = m.ToolArguments })
                            }
                        }
                    }
                });
            }
            // 处理 tool 角色消息
            else if (m.Role == MessageRole.Tool)
            {
                messageList.Add(new
                {
                    role = "tool",
                    tool_call_id = m.ToolCallId,
                    content = m.Content
                });
            }
            // 处理普通消息（user/assistant）
            else
            {
                messageList.Add(new
                {
                    role = role,
                    content = m.Content
                });
            }
        }

        // 定义可用的工具
        var tools = new[]
        {
            new
            {
                type = "function",
                function = new
                {
                    name = "web_search",
                    description = "搜索网络获取最新信息，如硬件价格、性能评测、产品参数等。适用于需要实时数据或最新信息的查询。",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            query = new
                            {
                                type = "string",
                                description = "搜索关键词，例如：'RTX 4090 京东价格'、'AMD Ryzen 9 7950X 性能评测'"
                            }
                        },
                        required = new[] { "query" }
                    }
                }
            }
        };

        var requestBody = new
        {
            model = "deepseek-reasoner",
            messages = messageList.ToArray(),
            tools = tools,
            stream = true
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/chat/completions")
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        // 用于累积 tool call 信息
        string? toolCallId = null;
        string? toolName = null;
        var toolArguments = new StringBuilder();

        while (!reader.EndOfStream)
        {
            // 主动检查取消令牌
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                continue;

            var data = line.Substring(6); // 移除 "data: " 前缀

            if (data == "[DONE]")
                break;

            try
            {
                var chunk = JsonSerializer.Deserialize<StreamChunk>(data);
                if (chunk?.Choices == null || chunk.Choices.Length == 0)
                    continue;

                var choice = chunk.Choices[0];
                var delta = choice.Delta;

                // 处理工具调用
                if (delta?.ToolCalls != null && delta.ToolCalls.Length > 0)
                {
                    var toolCall = delta.ToolCalls[0];

                    // 累积 tool call 信息
                    if (!string.IsNullOrEmpty(toolCall.Id))
                        toolCallId = toolCall.Id;

                    if (!string.IsNullOrEmpty(toolCall.Function?.Name))
                        toolName = toolCall.Function.Name;

                    if (!string.IsNullOrEmpty(toolCall.Function?.Arguments))
                        toolArguments.Append(toolCall.Function.Arguments);
                }

                // 处理推理内容
                if (!string.IsNullOrEmpty(delta?.ReasoningContent))
                {
                    onReasoningChunk?.Invoke(delta.ReasoningContent);
                }

                // 处理回答内容
                if (!string.IsNullOrEmpty(delta?.Content))
                {
                    onContentChunk?.Invoke(delta.Content);
                }

                // 检查是否完成工具调用
                if (choice.FinishReason == "tool_calls" && onToolCall != null)
                {
                    if (!string.IsNullOrEmpty(toolName) && toolArguments.Length > 0)
                    {
                        // 解析工具参数
                        var argsJson = toolArguments.ToString();
                        var argsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argsJson);
                        var query = argsDict?["query"].GetString() ?? string.Empty;

                        if (!string.IsNullOrEmpty(query))
                        {
                            // 执行工具调用
                            var toolResult = await onToolCall(toolName, query);

                            // 将工具结果添加到消息历史并继续对话
                            messages.Add(new ChatMessage
                            {
                                Role = MessageRole.Assistant,
                                Content = string.Empty,
                                ToolCallId = toolCallId,
                                ToolName = toolName,
                                ToolArguments = query
                            });

                            messages.Add(new ChatMessage
                            {
                                Role = MessageRole.Tool,
                                Content = toolResult,
                                ToolCallId = toolCallId
                            });

                            // 递归调用继续对话（传递相同的回调）
                            await ChatCompletionStreamAsync(
                                messages,
                                onReasoningChunk ?? (_ => { }),
                                onContentChunk ?? (_ => { }),
                                onToolCall,
                                cancellationToken);
                            return;
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON 解析错误: {ex.Message}");
                // 继续处理下一行
            }
        }
    }
}

#region JSON 响应模型

/// <summary>
/// 流式响应块
/// </summary>
internal class StreamChunk
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("choices")]
    public Choice[]? Choices { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }
}

/// <summary>
/// 选择项
/// </summary>
internal class Choice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("delta")]
    public Delta? Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

/// <summary>
/// 增量内容
/// </summary>
internal class Delta
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("reasoning_content")]
    public string? ReasoningContent { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("tool_calls")]
    public ToolCall[]? ToolCalls { get; set; }
}

/// <summary>
/// 工具调用
/// </summary>
internal class ToolCall
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("function")]
    public FunctionCall? Function { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }
}

/// <summary>
/// 函数调用
/// </summary>
internal class FunctionCall
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }
}

#endregion
