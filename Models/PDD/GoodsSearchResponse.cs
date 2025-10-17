using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ai_computer.Models.PDD;

/// <summary>
/// 拼多多商品搜索响应
/// </summary>
public class GoodsSearchResponse
{
    /// <summary>
    /// 响应数据
    /// </summary>
    [JsonPropertyName("goods_search_response")]
    public GoodsSearchData? Data { get; set; }

    /// <summary>
    /// 错误响应
    /// </summary>
    [JsonPropertyName("error_response")]
    public ErrorResponse? ErrorResponse { get; set; }
}

/// <summary>
/// 商品搜索数据
/// </summary>
public class GoodsSearchData
{
    /// <summary>
    /// 商品列表
    /// </summary>
    [JsonPropertyName("goods_list")]
    public List<GoodsItem>? GoodsList { get; set; }

    /// <summary>
    /// 搜索id
    /// </summary>
    [JsonPropertyName("search_id")]
    public string? SearchId { get; set; }

    /// <summary>
    /// 翻页id
    /// </summary>
    [JsonPropertyName("list_id")]
    public string? ListId { get; set; }

    /// <summary>
    /// 返回商品总数
    /// </summary>
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
}

/// <summary>
/// 商品项
/// </summary>
public class GoodsItem
{
    /// <summary>
    /// 商品名称
    /// </summary>
    [JsonPropertyName("goods_name")]
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>
    /// 商品goodsSign
    /// </summary>
    [JsonPropertyName("goods_sign")]
    public string GoodsSign { get; set; } = string.Empty;

    /// <summary>
    /// 商品主图
    /// </summary>
    [JsonPropertyName("goods_image_url")]
    public string? GoodsImageUrl { get; set; }

    /// <summary>
    /// 商品缩略图
    /// </summary>
    [JsonPropertyName("goods_thumbnail_url")]
    public string? GoodsThumbnailUrl { get; set; }

    /// <summary>
    /// 最小拼团价（单位为分）
    /// </summary>
    [JsonPropertyName("min_group_price")]
    public long MinGroupPrice { get; set; }

    /// <summary>
    /// 最小单买价格（单位为分）
    /// </summary>
    [JsonPropertyName("min_normal_price")]
    public long MinNormalPrice { get; set; }

    /// <summary>
    /// 优惠券面额，单位为分
    /// </summary>
    [JsonPropertyName("coupon_discount")]
    public long? CouponDiscount { get; set; }

    /// <summary>
    /// 优惠券门槛价格，单位为分
    /// </summary>
    [JsonPropertyName("coupon_min_order_amount")]
    public long? CouponMinOrderAmount { get; set; }

    /// <summary>
    /// 优惠券剩余数量
    /// </summary>
    [JsonPropertyName("coupon_remain_quantity")]
    public long? CouponRemainQuantity { get; set; }

    /// <summary>
    /// 商品是否有优惠券
    /// </summary>
    [JsonPropertyName("has_coupon")]
    public bool HasCoupon { get; set; }

    /// <summary>
    /// 佣金比例，千分比
    /// </summary>
    [JsonPropertyName("promotion_rate")]
    public long PromotionRate { get; set; }

    /// <summary>
    /// 已售卖件数
    /// </summary>
    [JsonPropertyName("sales_tip")]
    public string? SalesTip { get; set; }

    /// <summary>
    /// 店铺名字
    /// </summary>
    [JsonPropertyName("mall_name")]
    public string? MallName { get; set; }

    /// <summary>
    /// 店铺类型，1-个人，2-企业，3-旗舰店，4-专卖店，5-专营店，6-普通店
    /// </summary>
    [JsonPropertyName("merchant_type")]
    public int MerchantType { get; set; }

    /// <summary>
    /// 商品品牌词信息
    /// </summary>
    [JsonPropertyName("brand_name")]
    public string? BrandName { get; set; }

    /// <summary>
    /// 搜索id
    /// </summary>
    [JsonPropertyName("search_id")]
    public string? SearchId { get; set; }

    /// <summary>
    /// 商品描述
    /// </summary>
    [JsonPropertyName("goods_desc")]
    public string? GoodsDesc { get; set; }

    /// <summary>
    /// 服务分
    /// </summary>
    [JsonPropertyName("serv_txt")]
    public string? ServTxt { get; set; }

    /// <summary>
    /// 物流分
    /// </summary>
    [JsonPropertyName("lgst_txt")]
    public string? LgstTxt { get; set; }

    /// <summary>
    /// 描述分
    /// </summary>
    [JsonPropertyName("desc_txt")]
    public string? DescTxt { get; set; }

    /// <summary>
    /// 店铺id
    /// </summary>
    [JsonPropertyName("mall_id")]
    public long? MallId { get; set; }

    /// <summary>
    /// 商品活动标记数组
    /// </summary>
    [JsonPropertyName("activity_tags")]
    public List<int>? ActivityTags { get; set; }

    /// <summary>
    /// 优惠标签列表
    /// </summary>
    [JsonPropertyName("unified_tags")]
    public List<string>? UnifiedTags { get; set; }

    /// <summary>
    /// 计算券后价（单位：元）
    /// </summary>
    public decimal GetCouponPrice()
    {
        var groupPrice = MinGroupPrice / 100m;
        if (HasCoupon && CouponDiscount.HasValue)
        {
            return groupPrice - (CouponDiscount.Value / 100m);
        }
        return groupPrice;
    }

    /// <summary>
    /// 获取拼团价（单位：元）
    /// </summary>
    public decimal GetGroupPrice()
    {
        return MinGroupPrice / 100m;
    }

    /// <summary>
    /// 计算佣金金额（单位：元）
    /// </summary>
    public decimal GetCommissionAmount()
    {
        var price = GetCouponPrice();
        return price * (PromotionRate / 1000m);
    }

    /// <summary>
    /// 获取佣金比例（百分比）
    /// </summary>
    public decimal GetCommissionRate()
    {
        return PromotionRate / 10m; // 千分比转百分比
    }
}

/// <summary>
/// 错误响应
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// 错误消息
    /// </summary>
    [JsonPropertyName("error_msg")]
    public string? ErrorMsg { get; set; }

    /// <summary>
    /// 错误码
    /// </summary>
    [JsonPropertyName("error_code")]
    public int ErrorCode { get; set; }

    /// <summary>
    /// 子错误消息
    /// </summary>
    [JsonPropertyName("sub_msg")]
    public string? SubMsg { get; set; }

    /// <summary>
    /// 子错误码
    /// </summary>
    [JsonPropertyName("sub_code")]
    public string? SubCode { get; set; }

    /// <summary>
    /// 请求ID
    /// </summary>
    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }
}
