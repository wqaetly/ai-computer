using System.Text.Json.Serialization;

namespace ai_computer.Models.JDUnion;

/// <summary>
/// 推广链接响应
/// </summary>
public class PromotionResponse
{
    /// <summary>
    /// 返回码
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 返回消息
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 推广数据
    /// </summary>
    [JsonPropertyName("data")]
    public PromotionData? Data { get; set; }
}

/// <summary>
/// 推广数据
/// </summary>
public class PromotionData
{
    /// <summary>
    /// 生成的推广链接
    /// </summary>
    [JsonPropertyName("clickURL")]
    public string ClickUrl { get; set; } = string.Empty;

    /// <summary>
    /// 京口令
    /// </summary>
    [JsonPropertyName("jCommand")]
    public string? JCommand { get; set; }
}

/// <summary>
/// 京东API推广响应包装
/// </summary>
public class JDApiPromotionResponse
{
    /// <summary>
    /// 获取结果
    /// </summary>
    [JsonPropertyName("jd_union_open_promotion_common_get_responce")]
    public JDPromotionResult? GetResult { get; set; }
}

/// <summary>
/// 推广结果
/// </summary>
public class JDPromotionResult
{
    /// <summary>
    /// 结果数据
    /// </summary>
    [JsonPropertyName("getResult")]
    public string? GetResultJson { get; set; }
}
