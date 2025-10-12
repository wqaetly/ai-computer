using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AiComputer.Services.Tools;

/// <summary>
/// 网络搜索工具
/// </summary>
public class WebSearchTool : ITool
{
    private readonly Func<string, Task<string>> _searchFunction;

    public string Name => "web_search";

    public string Description => "Search the web for real-time information. Provide specific keywords (e.g., 'RTX 4060 2025 price'), not placeholders.";

    public JsonDocument InputSchema { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="searchFunction">实际的搜索函数</param>
    public WebSearchTool(Func<string, Task<string>> searchFunction)
    {
        _searchFunction = searchFunction;

        // 定义输入参数的 JSON Schema
        var schemaJson = @"{
  ""type"": ""object"",
  ""properties"": {
    ""query"": {
      ""type"": ""string"",
      ""description"": ""Specific search keywords (e.g., 'RTX 4060 price'), not '关键词' or 'xxx'""
    }
  },
  ""required"": [""query""]
}";
        InputSchema = JsonDocument.Parse(schemaJson);
    }

    /// <summary>
    /// 执行搜索
    /// </summary>
    public async Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        // 提取 query 参数
        if (!arguments.TryGetProperty("query", out var queryElement))
        {
            throw new ArgumentException("Missing required parameter: query");
        }

        var query = queryElement.GetString();
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be empty");
        }

        Console.WriteLine($"[WebSearchTool] Searching for: {query}");

        try
        {
            var result = await _searchFunction(query);
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebSearchTool] Search failed: {ex.Message}");
            throw new Exception($"Web search failed: {ex.Message}");
        }
    }
}
