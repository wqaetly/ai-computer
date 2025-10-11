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
    /// 首次请求的系统提示词
    /// </summary>
    private const string SystemPrompt = @"你是""玄机鉴""AI分析师，负责解析电脑硬件配置，为用户提供性能评估、瓶颈提示与优化建议，以及根据用户需求直接提供详尽的装机配置单。

**重要：深度思考要求**
对于每个用户问题，你必须进行深度思考和分析：
- 分析用户的真实需求和使用场景
- 识别需要查询哪些信息（价格、性能、评测等）
- 判断是否需要最新的实时数据

**网络搜索功能 - 重要说明**
当你需要查询最新硬件信息、价格、性能测试数据等实时信息时：

1. 在思考过程中，使用以下格式标记需要搜索的内容：
   [SEARCH:搜索关键词]

2. **关键：输出完所有需要的搜索标记后，立即停止输出，不要继续分析**
   - 因为搜索结果还未返回，你此时无法进行实际分析
   - 系统会自动执行搜索，并将结果嵌入到你的思考内容中
   - 然后你会收到包含搜索结果的完整上下文，继续完成分析

**如果不需要搜索**，则直接给出完整分析和建议。可以在标题前使用 Markdown 表情增加用户体验";

    /// <summary>
    /// 搜索后继续回答的系统提示词
    /// </summary>
    private const string SystemPromptWithSearchResults = @"你是""玄机鉴""AI分析师，负责解析电脑硬件配置，为用户提供性能评估、瓶颈提示与优化建议。

**重要提示：这是继续之前的对话**

在用户消息中，你会看到：
1. 用户的原始问题
2. 标记为[你之前的思考过程（已包含搜索结果）]的内容
3. 该内容包含：
   - 你之前的问题分析
   - 原本的 [SEARCH:xxx] 标记已被替换为实际搜索结果
   - 格式：[搜索结果：关键词]\n实际数据...

**你的任务**：
- 阅读你之前的思考过程和搜索结果
- 不要重新分析问题（已完成）
- 不要再输出 [SEARCH:xxx] 标记
- 直接基于搜索结果给出分析和建议

**回答要求**：
- 综合分析搜索结果
- 提供性能评估、价格对比、购买建议
- 给出价格区间和来源
- 考虑性价比、兼容性、未来升级
- 清晰结构化，可用Markdown表情";

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
    /// <param name="onSearch">搜索回调，返回搜索结果</param>
    /// <param name="isSearchFollowUp">是否是搜索后的继续请求（true=禁止再次搜索，直接基于搜索结果回答）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ChatCompletionStreamAsync(
        List<ChatMessage> messages,
        Action<string> onReasoningChunk,
        Action<string> onContentChunk,
        Func<string, Task<string>>? onSearch = null,
        bool isSearchFollowUp = false,
        CancellationToken cancellationToken = default)
    {
        // 根据是否是搜索后继续请求，选择不同的系统提示词
        var systemPrompt = isSearchFollowUp ? SystemPromptWithSearchResults : SystemPrompt;

        // 构造消息列表，自动在开头添加系统提示词
        var messageList = new List<object>
        {
            new { role = "system", content = systemPrompt }
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

        // 调试：打印消息数量
        Console.WriteLine($"[DeepSeek] isSearchFollowUp={isSearchFollowUp}, messages count={messages.Count}, messageList count={messageList.Count}");
        if (isSearchFollowUp && messages.Count > 0)
        {
            var lastMsg = messages.Last();
            Console.WriteLine($"[DeepSeek] Second request - last message role={lastMsg.Role}, content length={lastMsg.Content?.Length ?? 0}");
            Console.WriteLine($"[DeepSeek] Last message preview: {lastMsg.Content?.Substring(0, Math.Min(200, lastMsg.Content?.Length ?? 0))}...");
        }

        // 构造请求体
        var requestBody = new
        {
            model = "deepseek-reasoner",
            messages = messageList.ToArray(),
            stream = true
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        Console.WriteLine($"[DeepSeek] Request body preview: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}...");
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/chat/completions")
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        // 累积推理内容，用于检测搜索标记
        var reasoningBuffer = new StringBuilder();
        var contentBuffer = new StringBuilder();
        var hasCheckedForSearch = false; // 标记是否已经检查过搜索

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

                // 处理推理内容
                if (!string.IsNullOrEmpty(delta?.ReasoningContent))
                {
                    reasoningBuffer.Append(delta.ReasoningContent);
                    onReasoningChunk?.Invoke(delta.ReasoningContent);
                }

                // 当开始接收正式回答内容时，检查推理中的搜索标记
                if (!string.IsNullOrEmpty(delta?.Content))
                {
                    // 第一次收到 content，说明推理已经结束，检查是否需要搜索
                    // 注意：如果是搜索后的继续请求，则不再检查搜索标记
                    if (!hasCheckedForSearch && !isSearchFollowUp && onSearch != null && reasoningBuffer.Length > 0)
                    {
                        hasCheckedForSearch = true;
                        var reasoningText = reasoningBuffer.ToString();

                        // 提取所有SEARCH标记
                        var searchMatches = System.Text.RegularExpressions.Regex.Matches(reasoningText, @"\[SEARCH:([^\]]+)\]");

                        if (searchMatches.Count > 0)
                        {
                            Console.WriteLine($"[DeepSeek] Found {searchMatches.Count} SEARCH tags in reasoning");

                            // 收集所有搜索关键词（HashSet自动去重）
                            var queries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            foreach (System.Text.RegularExpressions.Match match in searchMatches)
                            {
                                var query = match.Groups[1].Value.Trim();
                                if (!string.IsNullOrWhiteSpace(query))
                                {
                                    queries.Add(query);
                                }
                            }

                            Console.WriteLine($"[DeepSeek] After deduplication: {queries.Count} unique queries");

                            // 执行所有搜索
                            if (queries.Count > 0)
                            {
                                // 执行搜索并收集结果
                                var searchResultsMap = new Dictionary<string, string>();
                                foreach (var query in queries)
                                {
                                    try
                                    {
                                        var searchResult = await onSearch(query);
                                        searchResultsMap[query] = searchResult;
                                    }
                                    catch (Exception ex)
                                    {
                                        searchResultsMap[query] = $"搜索失败：{ex.Message}";
                                    }
                                }

                                // 关键改进：将搜索结果嵌入到推理内容中
                                // 替换所有出现的搜索标记（自动去重，同一个关键词只搜索一次）
                                var enrichedReasoning = reasoningText;
                                foreach (var kvp in searchResultsMap)
                                {
                                    var searchTag = $"[SEARCH:{kvp.Key}]";
                                    var replacement = $"[搜索结果：{kvp.Key}]\n{kvp.Value}\n";
                                    // 替换所有出现的该标记
                                    enrichedReasoning = enrichedReasoning.Replace(searchTag, replacement);
                                }

                                Console.WriteLine($"[DeepSeek] Enriched reasoning length: {enrichedReasoning.Length}");

                                // 关键修复：将enriched reasoning附加到最后一条user消息中
                                // 因为DeepSeek-Reasoner的reasoning阶段可能不参考assistant历史
                                var lastUserMessage = messages.Last();
                                lastUserMessage.Content = $"{lastUserMessage.Content}\n\n[你之前的思考过程（已包含搜索结果）]\n{enrichedReasoning}";

                                // 递归调用继续对话，标记为搜索后的继续请求
                                await ChatCompletionStreamAsync(
                                    messages,
                                    onReasoningChunk,
                                    onContentChunk,
                                    onSearch,
                                    isSearchFollowUp: true, // 关键：标记为搜索后的继续请求
                                    cancellationToken);
                                return; // 重要：立即返回，不继续处理当前流
                            }
                        }
                    }

                    // 没有搜索或搜索已处理，正常输出内容
                    contentBuffer.Append(delta.Content);
                    onContentChunk?.Invoke(delta.Content);
                }
            }
            catch (JsonException)
            {
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
