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
using AiComputer.Services.Tools;

namespace AiComputer.Services;

/// <summary>
/// DeepSeek API 服务（集成通用工具调用功能）
/// </summary>
public class DeepSeekService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://api.deepseek.com";

    /// <summary>
    /// 工具执行器
    /// </summary>
    private readonly ToolExecutor _toolExecutor;

    /// <summary>
    /// 获取基础系统提示词（不包含工具说明，但包含当前日期）
    /// </summary>
    private string GetBaseSystemPrompt()
    {
        var now = DateTime.Now;
        var currentDate = now.ToString("yyyy年MM月dd日");
        var currentYear = now.Year;

        return $@"**当前日期**: {currentDate}（现在是 {currentYear} 年）

你是""玄机鉴""AI分析师，负责解析电脑硬件配置，提供性能评估、瓶颈分析与装机建议。

**核心规则**：
1. **内容格式**：使用 Markdown 和表情符号，让回答生动易读
2. **数据准确性**：价格和性能数据需标注来源和时效性
3. **专业性**：提供客观、准确的技术分析

**思考要求**：
- 保持推理过程精简、高效
- 直接聚焦关键问题点
- 避免冗余的思考步骤
- 快速识别需要搜索的信息";
    }

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

        _toolExecutor = new ToolExecutor();
    }

    /// <summary>
    /// 注册工具
    /// </summary>
    public void RegisterTool(ITool tool)
    {
        _toolExecutor.RegisterTool(tool);
    }

    /// <summary>
    /// 获取完整的系统提示词（包含工具使用说明）
    /// </summary>
    private string GetSystemPrompt()
    {
        return _toolExecutor.BuildSystemPrompt(GetBaseSystemPrompt());
    }

    /// <summary>
    /// 最大工具调用递归深度（防止无限循环）
    /// </summary>
    private const int MaxToolCallDepth = 1;

    /// <summary>
    /// 流式聊天完成（支持工具调用）
    /// </summary>
    /// <param name="messages">消息历史</param>
    /// <param name="onReasoningChunk">推理内容回调</param>
    /// <param name="onContentChunk">回答内容回调</param>
    /// <param name="onToolCall">工具调用回调（通知UI开始工具调用）</param>
    /// <param name="onToolCompleted">工具完成回调（通知UI工具执行完成，传递工具结果）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ChatCompletionStreamAsync(
        List<ChatMessage> messages,
        Action<string>? onReasoningChunk = null,
        Action<string>? onContentChunk = null,
        Action<string, string>? onToolCall = null,
        Action<string>? onToolCompleted = null,
        CancellationToken cancellationToken = default)
    {
        await ChatCompletionInternalAsync(
            messages,
            onReasoningChunk,
            onContentChunk,
            onToolCall,
            onToolCompleted,
            toolCallDepth: 0,
            cancellationToken);
    }

    /// <summary>
    /// 内部聊天完成方法（支持递归调用）
    /// </summary>
    private async Task ChatCompletionInternalAsync(
        List<ChatMessage> messages,
        Action<string>? onReasoningChunk,
        Action<string>? onContentChunk,
        Action<string, string>? onToolCall,
        Action<string>? onToolCompleted,
        int toolCallDepth,
        CancellationToken cancellationToken)
    {
        // 构造消息列表
        var messageList = new List<object>
        {
            new { role = "system", content = GetSystemPrompt() }
        };

        // 添加用户消息历史
        foreach (var m in messages)
        {
            var role = m.Role.ToString().ToLower();
            messageList.Add(new
            {
                role = role,
                content = m.Content
            });
        }

        // 构造请求体
        var requestBody = new
        {
            model = "deepseek-reasoner",
            messages = messageList.ToArray(),
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

        // 累积推理内容和回答内容
        var reasoningBuffer = new StringBuilder();
        var contentBuffer = new StringBuilder();
        var hasCheckedForTools = false;

        // 创建TagExtractor用于实时过滤content中的工具调用标签
        var tagExtractor = new TagExtractor("<tool_use>", "</tool_use>");

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                continue;

            var data = line.Substring(6);

            if (data == "[DONE]")
                break;

            try
            {
                var chunk = JsonSerializer.Deserialize<StreamChunk>(data);
                if (chunk?.Choices == null || chunk.Choices.Length == 0)
                    continue;

                var choice = chunk.Choices[0];
                var delta = choice.Delta;

                // 处理推理内容（推理内容始终正常输出，不检测工具调用）
                if (!string.IsNullOrEmpty(delta?.ReasoningContent))
                {
                    reasoningBuffer.Append(delta.ReasoningContent);
                    onReasoningChunk?.Invoke(delta.ReasoningContent);
                }

                // 处理回答内容（使用TagExtractor过滤工具调用标签）
                if (!string.IsNullOrEmpty(delta?.Content))
                {
                    // 累积完整内容（包含工具调用标签）
                    contentBuffer.Append(delta.Content);

                    // 使用TagExtractor过滤工具调用标签，只输出非工具调用的内容
                    var filteredContent = tagExtractor.ProcessChunk(delta.Content);
                    if (!string.IsNullOrEmpty(filteredContent))
                    {
                        onContentChunk?.Invoke(filteredContent);
                    }
                }

                // 当流结束时，检查是否有工具调用
                if (choice.FinishReason == "stop" && !hasCheckedForTools)
                {
                    hasCheckedForTools = true;

                    // 输出TagExtractor中剩余的非工具调用内容
                    var remainingContent = tagExtractor.Flush();
                    if (!string.IsNullOrEmpty(remainingContent))
                    {
                        onContentChunk?.Invoke(remainingContent);
                    }

                    // 从回答内容解析工具调用（新方案：工具调用在content中）
                    var toolCalls = _toolExecutor.ParseToolCalls(contentBuffer.ToString());

                    if (toolCalls.Count > 0)
                    {
                        Console.WriteLine($"[DeepSeek] Found {toolCalls.Count} tool calls at depth {toolCallDepth}");

                        // 通知 UI 开始工具调用
                        foreach (var toolCall in toolCalls)
                        {
                            onToolCall?.Invoke(toolCall.ToolName, JsonSerializer.Serialize(toolCall.Arguments));
                        }

                        // 执行工具
                        var executionResults = await _toolExecutor.ExecuteToolsAsync(toolCalls, cancellationToken);

                        // 格式化工具结果
                        var toolResultsText = _toolExecutor.FormatToolResults(executionResults);

                        Console.WriteLine($"[DeepSeek] Tool results:\n{toolResultsText}");

                        // 通知UI工具执行完成
                        onToolCompleted?.Invoke(toolResultsText);

                        // 将工具结果添加到消息历史
                        messages.Add(new ChatMessage
                        {
                            Role = MessageRole.User,
                            Content = toolResultsText
                        });

                        // 检查递归深度限制
                        if (toolCallDepth >= MaxToolCallDepth)
                        {
                            Console.WriteLine($"[DeepSeek] Reached max tool call depth ({MaxToolCallDepth}), forcing final answer");

                            // 添加强制指令，要求给出最终答案
                            messages.Add(new ChatMessage
                            {
                                Role = MessageRole.User,
                                Content = "Based on the tool results above, provide your final answer now. Do NOT make any more tool calls."
                            });

                            // 最后一次递归，不再允许工具调用
                            await ChatCompletionInternalAsync(
                                messages,
                                onReasoningChunk,
                                onContentChunk,
                                onToolCall,
                                onToolCompleted,
                                toolCallDepth: MaxToolCallDepth + 1, // 超过最大深度，确保不再递归
                                cancellationToken);

                            return;
                        }

                        // 递归调用，继续对话（深度+1）
                        await ChatCompletionInternalAsync(
                            messages,
                            onReasoningChunk,
                            onContentChunk,
                            onToolCall,
                            onToolCompleted,
                            toolCallDepth: toolCallDepth + 1,
                            cancellationToken);

                        return; // 重要：立即返回，不继续处理当前流
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[DeepSeek] JSON parse error: {ex.Message}");
                // 忽略解析错误，继续处理下一行
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
    [JsonPropertyName("choices")]
    public Choice[]? Choices { get; set; }
}

/// <summary>
/// 选择项
/// </summary>
internal class Choice
{
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
}

#endregion
