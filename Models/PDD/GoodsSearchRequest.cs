using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ai_computer.Models.PDD;

/// <summary>
/// 拼多多商品搜索请求
/// </summary>
public class GoodsSearchRequest
{
    /// <summary>
    /// 商品关键词
    /// </summary>
    [JsonPropertyName("keyword")]
    public string? Keyword { get; set; }

    /// <summary>
    /// 默认值1，商品分页数
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// 默认100，每页商品数量
    /// </summary>
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 排序方式
    /// 0-综合排序;1-按佣金比率升序;2-按佣金比例降序;3-按价格升序;4-按价格降序;
    /// 5-按销量升序;6-按销量降序;9-券后价升序排序;10-券后价降序排序;
    /// 13-按佣金金额升序排序;14-按佣金金额降序排序
    /// </summary>
    [JsonPropertyName("sort_type")]
    public int SortType { get; set; } = 2; // 默认按佣金比例降序

    /// <summary>
    /// 是否只返回优惠券的商品
    /// </summary>
    [JsonPropertyName("with_coupon")]
    public bool? WithCoupon { get; set; }

    /// <summary>
    /// 筛选范围列表
    /// </summary>
    [JsonPropertyName("range_list")]
    public List<RangeItem>? RangeList { get; set; }

    /// <summary>
    /// 推广位id
    /// </summary>
    [JsonPropertyName("pid")]
    public string? Pid { get; set; }

    /// <summary>
    /// 商品标签类目ID
    /// </summary>
    [JsonPropertyName("opt_id")]
    public long? OptId { get; set; }

    /// <summary>
    /// 商品类目ID
    /// </summary>
    [JsonPropertyName("cat_id")]
    public long? CatId { get; set; }

    /// <summary>
    /// 店铺类型，1-个人，2-企业，3-旗舰店，4-专卖店，5-专营店，6-普通店
    /// </summary>
    [JsonPropertyName("merchant_type")]
    public int? MerchantType { get; set; }

    /// <summary>
    /// 是否为品牌商品
    /// </summary>
    [JsonPropertyName("is_brand_goods")]
    public bool? IsBrandGoods { get; set; }
}

/// <summary>
/// 筛选范围项
/// </summary>
public class RangeItem
{
    /// <summary>
    /// 区间ID
    /// 0-最小成团价, 1-券后价, 2-佣金比例, 3-优惠券价格, 5-销量, 6-佣金金额
    /// </summary>
    [JsonPropertyName("range_id")]
    public int RangeId { get; set; }

    /// <summary>
    /// 区间的开始值
    /// </summary>
    [JsonPropertyName("range_from")]
    public long RangeFrom { get; set; }

    /// <summary>
    /// 区间的结束值
    /// </summary>
    [JsonPropertyName("range_to")]
    public long RangeTo { get; set; }
}
