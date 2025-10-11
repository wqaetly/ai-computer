using System;
using System.Collections.Generic;
using System.IO;
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
- 评估硬件配置的合理性、性能瓶颈、兼容性问题
- 对比不同硬件方案的优劣
- 分析性价比和预算分配策略
- 考虑未来升级空间和长期使用成本

请在思考过程中展示你的推理步骤，包括：
1. 问题分析：理解用户的核心需求
2. 数据收集：需要哪些信息来回答问题
3. 方案对比：评估不同的解决方案
4. 综合判断：权衡各种因素得出结论

**网络搜索功能**
当你需要查询最新硬件信息、价格、性能测试数据等实时信息时，可以在你的思考过程中使用以下格式触发搜索：
[SEARCH:搜索关键词]

例如：
- [SEARCH:RTX 4090 京东价格]
- [SEARCH:AMD Ryzen 9 7950X 性能评测]
- [SEARCH:DDR5 6000MHz 内存 2024]

系统会自动执行搜索，并将结果返回给你继续分析。你可以在一次思考中使用多次搜索。

**价格查询要求**：
- 必须查询京东、淘宝的当前价格
- 给出价格区间（最低价-最高价）
- 标注价格来源和查询时间
- 说明是否促销价格
- 提供多个购买渠道的对比

以上内容要形成清晰、结构化的回答，最好能在每个标题前增加一些Markdown可用的表情，以增加用户体验。";

    /// <summary>
    /// 搜索后继续回答的系统提示词
    /// </summary>
    private const string SystemPromptWithSearchResults = @"你是""玄机鉴""AI分析师，负责解析电脑硬件配置，为用户提供性能评估、瓶颈提示与优化建议，以及根据用户需求直接提供详尽的装机配置单。

**重要提示**：以下消息包含了你之前请求的搜索结果。请基于这些搜索结果，继续完成你的回答。

**严格禁止**：
- 不要再次使用 [SEARCH:xxx] 标记
- 不要重新开始思考整个问题
- 直接基于已有的搜索结果继续分析和回答

**回答要求**：
- 综合分析搜索结果中的信息
- 提供性能评估、价格对比、购买建议
- 给出价格区间（最低价-最高价）
- 标注价格来源
- 说明是否促销价格
- 提供多个购买渠道的对比
- 形成清晰、结构化的回答
- 可以在标题前使用 Markdown 表情增加用户体验";

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
                        var searchMatches = System.Text.RegularExpressions.Regex.Matches(reasoningText, @"\[SEARCH:([^\]]+)\]");

                        if (searchMatches.Count > 0)
                        {
                            // 收集所有搜索关键词（同一轮内去重）
                            var queries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            foreach (System.Text.RegularExpressions.Match match in searchMatches)
                            {
                                var query = match.Groups[1].Value.Trim();
                                if (!string.IsNullOrWhiteSpace(query))
                                {
                                    queries.Add(query);
                                }
                            }

                            // 执行所有搜索
                            if (queries.Count > 0)
                            {
                                var searchResultsBuilder = new StringBuilder();
                                searchResultsBuilder.AppendLine("以下是你请求的搜索结果：\n");

                                foreach (var query in queries)
                                {
                                    try
                                    {
                                        var searchResult = await onSearch(query);
                                        searchResultsBuilder.AppendLine($"【搜索关键词：{query}】");
                                        searchResultsBuilder.AppendLine(searchResult);
                                        searchResultsBuilder.AppendLine();
                                    }
                                    catch (Exception ex)
                                    {
                                        searchResultsBuilder.AppendLine($"【搜索关键词：{query}】");
                                        searchResultsBuilder.AppendLine($"搜索失败：{ex.Message}");
                                        searchResultsBuilder.AppendLine();
                                    }
                                }

                                // 将搜索结果添加到对话历史
                                messages.Add(new ChatMessage
                                {
                                    Role = MessageRole.User,
                                    Content = searchResultsBuilder.ToString()
                                });

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
