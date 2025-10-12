using System.Text.Json.Serialization;

namespace ai_computer.Models.JDUnion;

/// <summary>
/// 商品查询请求
/// </summary>
public class GoodsQueryRequest
{
    /// <summary>
    /// 关键词
    /// </summary>
    [JsonPropertyName("keyword")]
    public string? Keyword { get; set; }

    /// <summary>
    /// 页码
    /// </summary>
    [JsonPropertyName("pageIndex")]
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 每页数量（最大30）
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 场景ID (1:联盟商品, 2:京东主站商品)
    /// </summary>
    [JsonPropertyName("sceneId")]
    public int SceneId { get; set; } = 1;

    /// <summary>
    /// 是否是优惠券商品，1：有优惠券
    /// </summary>
    [JsonPropertyName("isCoupon")]
    public int? IsCoupon { get; set; }

    /// <summary>
    /// 券后价格下限
    /// </summary>
    [JsonPropertyName("pricefrom")]
    public decimal? PriceFrom { get; set; }

    /// <summary>
    /// 券后价格上限
    /// </summary>
    [JsonPropertyName("priceto")]
    public decimal? PriceTo { get; set; }

    /// <summary>
    /// 佣金比例区间开始
    /// </summary>
    [JsonPropertyName("commissionShareStart")]
    public int? CommissionShareStart { get; set; }

    /// <summary>
    /// 佣金比例区间结束
    /// </summary>
    [JsonPropertyName("commissionShareEnd")]
    public int? CommissionShareEnd { get; set; }

    /// <summary>
    /// 排序字段 (price:价格, commissionShare:佣金比例, commission:佣金, inOrderCount30Days:30天引单量)
    /// </summary>
    [JsonPropertyName("sortName")]
    public string? SortName { get; set; }

    /// <summary>
    /// 排序方式 (asc:升序, desc:降序)
    /// </summary>
    [JsonPropertyName("sort")]
    public string? Sort { get; set; }

    /// <summary>
    /// 品牌code
    /// </summary>
    [JsonPropertyName("brandCode")]
    public string? BrandCode { get; set; }

    /// <summary>
    /// 商品类型：自营[g]，POP[p]
    /// </summary>
    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    /// <summary>
    /// 联盟id_应用id_推广位id
    /// </summary>
    [JsonPropertyName("pid")]
    public string? Pid { get; set; }

    /// <summary>
    /// 支持出参数据筛选
    /// </summary>
    [JsonPropertyName("fields")]
    public string? Fields { get; set; }
}
