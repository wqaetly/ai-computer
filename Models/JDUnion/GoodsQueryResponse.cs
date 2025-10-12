using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ai_computer.Models.JDUnion;

/// <summary>
/// 商品查询响应
/// </summary>
public class GoodsQueryResponse
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
    /// 商品总数
    /// </summary>
    [JsonPropertyName("totalCount")]
    public long TotalCount { get; set; }

    /// <summary>
    /// 商品列表
    /// </summary>
    [JsonPropertyName("data")]
    public List<GoodsData>? Data { get; set; }
}

/// <summary>
/// 商品数据包装
/// </summary>
public class GoodsData
{
    /// <summary>
    /// 商品响应信息
    /// </summary>
    [JsonPropertyName("goodsResp")]
    public GoodsInfo? GoodsResp { get; set; }
}

/// <summary>
/// 京东API外层响应包装
/// </summary>
public class JDApiGoodsResponse
{
    /// <summary>
    /// 查询结果
    /// </summary>
    [JsonPropertyName("jd_union_open_goods_query_responce")]
    public JDGoodsQueryResult? QueryResult { get; set; }
}

/// <summary>
/// 查询结果
/// </summary>
public class JDGoodsQueryResult
{
    /// <summary>
    /// 结果数据
    /// </summary>
    [JsonPropertyName("queryResult")]
    public string? QueryResultJson { get; set; }
}
