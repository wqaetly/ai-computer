using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AiComputer.Services.Tools;

/// <summary>
/// 京东商品推荐工具 - 用于AI对话中推荐商品并获取推广链接
/// </summary>
public class JDProductRecommendTool : ITool
{
    private readonly Func<string, decimal?, decimal?, int, Task<string>> _recommendFunction;

    public string Name => "recommend_jd_product";

    public string Description =>
        "推荐京东商品并生成推广链接。当用户询问价格、要求推荐商品、想要购买、比较产品、询问具体型号时，必须使用此工具。返回包含图片、价格、购买链接的商品卡片。例如：'RTX 4060多少钱'、'帮我推荐一款显卡'、'给我配一台电脑'等场景都应调用此工具。";

    public JsonDocument InputSchema { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="recommendFunction">
    /// 推荐函数：参数为(keyword, minPrice, maxPrice, count)，返回格式化的商品推荐结果
    /// </param>
    public JDProductRecommendTool(Func<string, decimal?, decimal?, int, Task<string>> recommendFunction)
    {
        _recommendFunction = recommendFunction;

        // 定义输入参数的 JSON Schema
        var schemaJson = @"{
  ""type"": ""object"",
  ""properties"": {
    ""keyword"": {
      ""type"": ""string"",
      ""description"": ""商品搜索关键词，例如：'RTX 4060显卡'、'i5处理器'、'游戏笔记本'、'机械键盘'等。必须是具体的商品类型，不能是'显卡'这种过于宽泛的词。""
    },
    ""min_price"": {
      ""type"": ""number"",
      ""description"": ""最低价格（人民币），可选参数。例如：2000表示2000元以上""
    },
    ""max_price"": {
      ""type"": ""number"",
      ""description"": ""最高价格（人民币），可选参数。例如：5000表示5000元以下""
    },
    ""count"": {
      ""type"": ""integer"",
      ""description"": ""推荐商品数量，默认10个，最多20个"",
      ""default"": 10
    }
  },
  ""required"": [""keyword""]
}";
        InputSchema = JsonDocument.Parse(schemaJson);
    }

    /// <summary>
    /// 执行商品推荐
    /// </summary>
    public async Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        // 提取参数
        if (!arguments.TryGetProperty("keyword", out var keywordElement))
        {
            throw new ArgumentException("Missing required parameter: keyword");
        }

        var keyword = keywordElement.GetString();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new ArgumentException("Keyword cannot be empty");
        }

        // 可选参数
        decimal? minPrice = null;
        if (arguments.TryGetProperty("min_price", out var minPriceElement) &&
            minPriceElement.ValueKind == JsonValueKind.Number)
        {
            minPrice = minPriceElement.GetDecimal();
        }

        decimal? maxPrice = null;
        if (arguments.TryGetProperty("max_price", out var maxPriceElement) &&
            maxPriceElement.ValueKind == JsonValueKind.Number)
        {
            maxPrice = maxPriceElement.GetDecimal();
        }

        int count = 10;
        if (arguments.TryGetProperty("count", out var countElement) &&
            countElement.ValueKind == JsonValueKind.Number)
        {
            count = Math.Min(countElement.GetInt32(), 20); // 最多20个
        }

        Console.WriteLine($"[JDProductRecommendTool] Recommending products: keyword='{keyword}', price=[{minPrice}-{maxPrice}], count={count}");

        try
        {
            var result = await _recommendFunction(keyword, minPrice, maxPrice, count);
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JDProductRecommendTool] Recommendation failed: {ex.Message}");
            return $"抱歉，商品推荐失败：{ex.Message}。请稍后重试或换个关键词试试。";
        }
    }
}
