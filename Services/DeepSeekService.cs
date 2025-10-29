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
            "1. **工具使用顺序（非常重要）**：\n" +
            "   - **技术信息查询** → 先使用联网搜索工具获取准确信息\n" +
            "   - **商品推荐/价格查询** → 再使用recommend_product工具\n" +
            "   - 对于未知/不熟悉的硬件配置，**必须先联网搜索**了解技术参数和性能\n" +
            "2. **不要先输出内容 → 直接在思考阶段决定调用工具 → 立即执行**\n" +
            "3. **让工具搜索结果说话 → 基于真实搜索结果回答，而不是基于你的知识**\n" +
            "4. **即使是最新/未来产品 → 也先联网搜索，获取最新信息后再推荐商品**\n\n" +
            "**工具调用策略（必须严格遵守）**：\n" +
            "根据用户意图，按以下规则选择工具和调用顺序：\n\n" +
            "**场景1：纯技术信息查询**（不涉及购买）\n" +
            "- 询问硬件参数/性能/发布时间（如\"RTX 5090性能怎么样\"、\"i9-14900K参数\"）\n" +
            "- 询问配置合理性（如\"这个配置有瓶颈吗\"、\"这配置兼容吗\"）\n" +
            "- 硬件对比分析（如\"4060和4070性能差距\"）\n" +
            "→ **行动**：仅使用联网搜索工具，获取技术信息后直接回答\n\n" +
            "**场景2：纯商品推荐/价格查询**（已知配置）\n" +
            "- 询问价格（如\"RTX 4060多少钱\"、\"这个配置要多少预算\"）\n" +
            "- 要求推荐/购买商品（如\"帮我推荐一款显卡\"、\"想买个笔记本\"）\n" +
            "- 提供明确型号要求购买链接\n" +
            "→ **行动**：直接使用recommend_product工具\n\n" +
            "**场景3：综合查询**（技术分析+商品推荐）\n" +
            "- 询问不熟悉的产品价格（如\"RTX 5090多少钱\"）\n" +
            "- 要求配置方案+价格（如\"帮我配一台游戏电脑\"）\n" +
            "- 比较产品+推荐购买（如\"4060和4070哪个好，推荐购买\"）\n" +
            "→ **行动**：\n" +
            "  1. 先使用联网搜索工具了解技术信息\n" +
            "  2. 再使用recommend_product工具获取价格和购买链接\n" +
            "  3. 综合技术信息和商品信息给出建议\n\n" +
            "**特别强调**：\n" +
            "- 对于未知/不熟悉的硬件，**必须先联网搜索**，不要直接调用recommend_product\n" +
            "- 装机配置单场景：先联网搜索了解配置合理性，再逐个查询组件价格\n" +
            "- 不要跳过联网搜索步骤，技术信息的准确性比速度更重要\n\n" +
            "**商品展示格式（必须遵守）**：\n" +
            "工具返回商品后，**必须使用Markdown表格格式**展示商品信息，格式如下：\n" +
            "```\n" +
            "| 图片 | 商品名称 | 价格 | 购买链接 |\n" +
            "| --- | --- | --- | --- |\n" +
            "| ![商品图](图片URL) | 商品名称 | ¥XXXX | [购买](链接) |\n" +
            "```\n" +
            "- 表格必须包含：图片、商品名称、价格、购买链接四列\n" +
            "- 图片使用Markdown图片语法：![商品图](图片URL)，图片URL从工具返回结果的\"图片\"字段提取\n" +
            "- 价格从工具返回结果的\"价格\"字段提取\n" +
            "- 购买链接从工具返回结果的\"购买\"字段提取，必须使用Markdown链接格式\n" +
            "- 如果有多个商品，每个商品占一行\n" +
            "- 表格前后可以添加简短说明，但商品信息必须在表格中\n" +
            "- **重要**：必须从工具返回的结果中提取真实的图片URL、价格和购买链接，不要省略或修改\n\n" +
            "**装机配置单处理规则（重中之重）**：\n" +
            "当用户提供装机配置单（包含多个硬件组件）时，你**必须**：\n" +
            "1. **识别配置单中的每个硬件组件**（CPU、显卡、主板、内存、硬盘、电源、机箱、散热器等）\n" +
            "2. **逐个调用recommend_product工具**搜索每个组件的价格和购买链接\n" +
            "   - 每个组件单独调用一次工具，不要合并查询\n" +
            "   - 例如配置单有8个硬件，就应该调用8次工具\n" +
            "3. **等待所有工具返回结果后**，汇总总价并给出专业分析\n" +
            "4. **在响应中展示每个组件的价格和购买链接**\n\n" +
            "**标准工作流程（必须按顺序执行）**：\n" +
            "1. **识别场景**：判断属于上述哪个场景（技术查询/商品推荐/综合查询）\n" +
            "2. **选择工具顺序**：\n" +
            "   - 场景1（纯技术）→ 仅联网搜索\n" +
            "   - 场景2（已知产品价格/购买）→ 仅recommend_product\n" +
            "   - 场景3（未知产品/综合）→ 先联网搜索，后recommend_product\n" +
            "3. **执行工具调用**：\n" +
            "   - 不熟悉的产品/配置 → 必须先联网搜索了解技术信息\n" +
            "   - 需要价格/购买链接 → 调用recommend_product\n" +
            "   - 配置单 → 先整体分析（联网搜索），再逐个查询价格\n" +
            "4. **基于工具结果回答**：综合所有工具结果，给出专业分析和建议\n" +
            "5. **提供购买链接**：如有商品推荐，展示价格和购买链接\n\n" +
            "**工作流示例**：\n\n" +
            "示例1 - 未知产品价格查询（场景3）：\n" +
            "- 用户：\"RTX 5090多少钱？\"\n" +
            "- 【错误】：直接调用recommend_product → 可能没有准确的技术信息\n" +
            "- 【正确】：\n" +
            "  1. 先联网搜索\"RTX 5090 性能参数 发布时间\" → 了解产品基本信息\n" +
            "  2. 再调用recommend_product(\"RTX 5090显卡\") → 获取价格和购买链接\n" +
            "  3. 综合回答：性能特点 + 价格 + 购买建议\n\n" +
            "示例2 - 已知产品价格查询（场景2）：\n" +
            "- 用户：\"i5-13400F多少钱？\"\n" +
            "- 【正确】：直接调用recommend_product(\"i5-13400F\") → 这是已知产品，直接查价格\n\n" +
            "示例3 - 纯技术查询（场景1）：\n" +
            "- 用户：\"RTX 5090性能怎么样？\"\n" +
            "- 【正确】：仅联网搜索\"RTX 5090 性能评测\" → 获取技术信息后回答\n\n" +
            "示例4 - 装机配置（场景3）：\n" +
            "- 用户：\"帮我配一台5000元游戏电脑\"\n" +
            "- 【正确】：\n" +
            "  1. 先联网搜索\"2025年5000元游戏电脑配置推荐\" → 了解当前市场主流配置\n" +
            "  2. 基于搜索结果确定配置方案\n" +
            "  3. 逐个调用recommend_product查询每个组件的价格\n" +
            "  4. 给出完整配置单和购买链接\n\n" +
            "**重要原则**：\n" +
            "- 不要自己编造价格或购买链接 → 必须使用工具获取真实的电商数据\n" +
            "- 不要假设产品不存在 → 让工具搜索结果说话\n" +
            "- 不要遗漏配置单中的任何组件 → 逐个查询所有组件\n" +
            "- 不要在调用工具前输出\"让我查一下\" → 直接调用工具，无需提前通知\n\n" +
            "**思考要求**：\n" +
            "- 保持推理过程精简、高效，直接聚焦关键问题\n" +
            "- **在思考阶段完成以下判断**：\n" +
            "  1. 识别用户意图（技术查询/商品推荐/综合查询）\n" +
            "  2. 判断产品熟悉度（已知/未知）\n" +
            "  3. 确定工具调用顺序（联网搜索优先 or 直接推荐商品）\n" +
            "- **关键规则**：\n" +
            "  - 未知产品 → 必须先联网搜索\n" +
            "  - 已知产品纯价格查询 → 直接recommend_product\n" +
            "  - 综合性问题 → 先联网搜索，后recommend_product\n" +
            "- 避免冗余的思考步骤，快速做出决策\n" +
            "- **不要在输出阶段犹豫，在思考阶段就明确工具调用计划**";

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
