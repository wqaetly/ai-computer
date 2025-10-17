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

        // 基础提示词 - 使用字符串拼接而不是verbatim来避免emoji导致的编译问题
        var basePrompt = $"**当前日期**: {currentDate}（现在是 {currentYear} 年）\n\n" +
            "你负责解析电脑硬件配置，提供性能评估、瓶颈分析与装机建议，并为用户提供商品购买推荐。\n\n" +
            "**核心规则**：\n" +
            "1. **内容格式**：使用 Markdown 和表情符号，让回答生动易读\n" +
            "2. **数据准确性**：价格和性能数据需标注来源和时效性\n" +
            "3. **专业性**：提供客观、准确的技术分析\n\n" +
            "**【严禁行为】（违反将浪费大量token和时间）**：\n" +
            "- **禁止说\"不知道\"、\"未上市\"、\"知识库中没有\"、\"我的数据截止到XXX\"等推诿性回答**\n" +
            "- **禁止在调用工具前输出任何\"我不确定\"、\"可能\"、\"让我查一下\"等犹豫性内容**\n" +
            "- **禁止假设产品不存在或未发布 - 用工具搜索结果说话，而不是你的假设**\n" +
            "- **禁止先解释为什么不知道再调用工具 - 直接调用工具即可**\n\n" +
            "**【正确行为】（行动优先原则）**：\n" +
            "1. **遇到任何产品查询（无论是否熟悉）→ 立即调用recommend_product工具**\n" +
            "2. **不要先输出内容 → 直接在思考阶段决定调用工具 → 立即执行**\n" +
            "3. **让工具搜索结果说话 → 基于真实搜索结果回答，而不是基于你的知识**\n" +
            "4. **即使是最新/未来产品 → 也先搜索，市场上可能已有相关产品或替代品**\n\n" +
            "**商品推荐规则（非常重要）**：\n" +
            "当用户有以下意图时，**必须立即使用recommend_product工具**推荐商品（不要犹豫，不要解释）：\n" +
            "- 询问价格（如\"RTX 4060多少钱\"、\"这个配置要多少预算\"）→ 直接调用工具\n" +
            "- 要求推荐/购买商品（如\"帮我推荐一款显卡\"、\"想买个笔记本\"）→ 直接调用工具\n" +
            "- 提到装机/配置清单（如\"帮我配一台电脑\"、\"给个配置方案\"）→ 直接调用工具\n" +
            "- 比较商品（如\"4060和4070哪个好\"）→ 直接调用工具搜索两款产品\n" +
            "- 询问具体型号（如\"i5-13400F怎么样\"）→ 直接调用工具\n" +
            "- **询问你不熟悉的产品**（如新产品、未来产品）→ 直接调用工具，不要说\"不知道\"\n\n" +
            "**商品展示格式（必须遵守）**：\n" +
            "工具返回商品后，**必须使用Markdown表格格式**展示商品信息，格式如下：\n" +
            "```\n" +
            "| 图片 | 商品名称 | 价格 | 购买链接 |\n" +
            "| --- | --- | --- | --- |\n" +
            "| ![](图片URL) | 商品名称 | ¥XXXX | [点击购买](链接) |\n" +
            "```\n" +
            "- 表格必须包含：图片、商品名称、价格、购买链接四列\n" +
            "- 图片使用Markdown图片语法：![](图片URL)，图片URL从工具返回结果中提取\n" +
            "- 购买链接必须使用Markdown链接格式：[点击购买](实际链接)\n" +
            "- 如果有多个商品，每个商品占一行\n" +
            "- 表格前后可以添加简短说明，但商品信息必须在表格中\n" +
            "- **重要**：必须从工具返回的结果中提取真实的图片URL，不要省略图片列\n\n" +
            "**装机配置单处理规则（重中之重）**：\n" +
            "当用户提供装机配置单（包含多个硬件组件）时，你**必须**：\n" +
            "1. **识别配置单中的每个硬件组件**（CPU、显卡、主板、内存、硬盘、电源、机箱、散热器等）\n" +
            "2. **逐个调用recommend_product工具**搜索每个组件的价格和购买链接\n" +
            "   - 每个组件单独调用一次工具，不要合并查询\n" +
            "   - 例如配置单有8个硬件，就应该调用8次工具\n" +
            "3. **等待所有工具返回结果后**，汇总总价并给出专业分析\n" +
            "4. **在响应中展示每个组件的价格和购买链接**\n\n" +
            "**推荐步骤（快速执行流程）**：\n" +
            "1. **识别意图**：快速判断是否涉及商品查询/推荐/价格/配置\n" +
            "2. **立即行动**：\n" +
            "   - 单个商品 → 立即调用recommend_product工具（不要犹豫）\n" +
            "   - 配置单 → 立即逐个调用recommend_product工具查询每个组件\n" +
            "   - 不熟悉的产品 → 立即调用工具搜索，而不是说\"不知道\"\n" +
            "3. **基于工具结果回答**：等待工具返回后，基于真实搜索结果给出专业分析和建议\n" +
            "4. **提供购买链接**：确保用户能看到每个组件的购买链接（工具已自动生成推广链接）\n\n" +
            "**工作流示例**：\n" +
            "- 用户：\"RTX 5090多少钱？\"\n" +
            "- 【错误】：先输出\"RTX 5090还未上市，我的知识库中暂无...\"\n" +
            "- 【正确】：直接调用工具 recommend_product(keyword=\"RTX 5090显卡\") → 基于搜索结果回答\n\n" +
            "**重要原则**：\n" +
            "- 不要自己编造价格或购买链接 → 必须使用工具获取真实的电商数据\n" +
            "- 不要假设产品不存在 → 让工具搜索结果说话\n" +
            "- 不要遗漏配置单中的任何组件 → 逐个查询所有组件\n" +
            "- 不要在调用工具前输出\"让我查一下\" → 直接调用工具，无需提前通知\n\n" +
            "**思考要求**：\n" +
            "- 保持推理过程精简、高效，直接聚焦关键问题\n" +
            "- **在思考阶段完成意图识别和工具调用决策，不要在输出阶段犹豫**\n" +
            "- 快速识别商品查询意图 → 立即决定调用工具 → 无需输出中间过程\n" +
            "- 避免冗余的思考步骤，例如\"用户问了XXX，我不知道，所以要调用工具\"\n" +
            "- **工具调用应该是本能反应，而不是经过长时间思考后的决定**";

        return basePrompt;
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
    private string GetSystemPrompt(bool useReasoningModel = true)
    {
        return _toolExecutor.BuildSystemPrompt(GetBaseSystemPrompt(), useReasoningModel);
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
    /// <param name="useReasoningModel">是否使用推理模型（deepseek-reasoner），false时使用普通模型（deepseek-chat）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ChatCompletionStreamAsync(
        List<ChatMessage> messages,
        Action<string>? onReasoningChunk = null,
        Action<string>? onContentChunk = null,
        Action<string, string>? onToolCall = null,
        Action<string>? onToolCompleted = null,
        bool useReasoningModel = true,
        CancellationToken cancellationToken = default)
    {
        await ChatCompletionInternalAsync(
            messages,
            onReasoningChunk,
            onContentChunk,
            onToolCall,
            onToolCompleted,
            useReasoningModel,
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
        bool useReasoningModel,
        int toolCallDepth,
        CancellationToken cancellationToken)
    {
        // 构造消息列表
        var messageList = new List<object>
        {
            new { role = "system", content = GetSystemPrompt(useReasoningModel) }
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

        // 构造请求体 - 根据参数选择模型
        var requestBody = new
        {
            model = useReasoningModel ? "deepseek-reasoner" : "deepseek-chat",
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
                            // 使用 GetRawText() 获取原始 JSON 字符串，避免二次序列化导致的格式问题
                            onToolCall?.Invoke(toolCall.ToolName, toolCall.Arguments.GetRawText());
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
                                useReasoningModel,
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
                            useReasoningModel,
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
