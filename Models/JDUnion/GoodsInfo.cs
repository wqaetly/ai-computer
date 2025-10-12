using System.Text.Json;
using System.Text.Json.Serialization;

namespace ai_computer.Models.JDUnion;

/// <summary>
/// 商品信息
/// </summary>
public class GoodsInfo
{
    /// <summary>
    /// 商品ID
    /// </summary>
    [JsonPropertyName("skuId")]
    public long SkuId { get; set; }

    /// <summary>
    /// 商品名称
    /// </summary>
    [JsonPropertyName("skuName")]
    public string SkuName { get; set; } = string.Empty;

    /// <summary>
    /// 联盟商品ID
    /// </summary>
    [JsonPropertyName("itemId")]
    public string? ItemId { get; set; }

    /// <summary>
    /// 商品落地页
    /// </summary>
    [JsonPropertyName("materialUrl")]
    public string MaterialUrl { get; set; } = string.Empty;

    /// <summary>
    /// 价格信息
    /// </summary>
    [JsonPropertyName("priceInfo")]
    public PriceInfo? PriceInfo { get; set; }

    /// <summary>
    /// 佣金信息
    /// </summary>
    [JsonPropertyName("commissionInfo")]
    public CommissionInfo? CommissionInfo { get; set; }

    /// <summary>
    /// 图片信息
    /// </summary>
    [JsonPropertyName("imageInfo")]
    public ImageInfo? ImageInfo { get; set; }

    /// <summary>
    /// 优惠券信息
    /// </summary>
    [JsonPropertyName("couponInfo")]
    public CouponInfo? CouponInfo { get; set; }

    /// <summary>
    /// 店铺信息
    /// </summary>
    [JsonPropertyName("shopInfo")]
    public ShopInfo? ShopInfo { get; set; }

    /// <summary>
    /// 30天引单数量
    /// </summary>
    [JsonPropertyName("inOrderCount30Days")]
    public long InOrderCount30Days { get; set; }

    /// <summary>
    /// 商品好评率
    /// </summary>
    [JsonPropertyName("goodCommentsShare")]
    public decimal GoodCommentsShare { get; set; }

    /// <summary>
    /// 评论数
    /// </summary>
    [JsonPropertyName("comments")]
    public long Comments { get; set; }

    /// <summary>
    /// 品牌名
    /// </summary>
    [JsonPropertyName("brandName")]
    public string? BrandName { get; set; }

    /// <summary>
    /// 商品类型：g=自营，p=pop
    /// </summary>
    [JsonPropertyName("owner")]
    public string? Owner { get; set; }
}

/// <summary>
/// 价格信息
/// </summary>
public class PriceInfo
{
    /// <summary>
    /// 商品价格
    /// </summary>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    /// <summary>
    /// 促销价
    /// </summary>
    [JsonPropertyName("lowestPrice")]
    public decimal? LowestPrice { get; set; }

    /// <summary>
    /// 券后价
    /// </summary>
    [JsonPropertyName("lowestCouponPrice")]
    public decimal? LowestCouponPrice { get; set; }
}

/// <summary>
/// 佣金信息
/// </summary>
public class CommissionInfo
{
    /// <summary>
    /// 佣金
    /// </summary>
    [JsonPropertyName("commission")]
    public decimal Commission { get; set; }

    /// <summary>
    /// 佣金比例
    /// </summary>
    [JsonPropertyName("commissionShare")]
    public decimal CommissionShare { get; set; }

    /// <summary>
    /// 券后佣金
    /// </summary>
    [JsonPropertyName("couponCommission")]
    public decimal? CouponCommission { get; set; }
}

/// <summary>
/// 图片信息
/// </summary>
public class ImageInfo
{
    /// <summary>
    /// 图片列表
    /// </summary>
    [JsonPropertyName("imageList")]
    public ImageList? ImageList { get; set; }

    /// <summary>
    /// 白底图
    /// </summary>
    [JsonPropertyName("whiteImage")]
    public string? WhiteImage { get; set; }
}

/// <summary>
/// 图片列表
/// </summary>
public class ImageList
{
    /// <summary>
    /// URL信息（单个或数组）
    /// </summary>
    [JsonPropertyName("urlInfo")]
    public object? UrlInfo { get; set; }

    /// <summary>
    /// 获取第一张图片URL
    /// </summary>
    public string? GetFirstImageUrl()
    {
        if (UrlInfo is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() > 0)
            {
                var firstItem = element[0];
                if (firstItem.TryGetProperty("url", out var urlProp))
                {
                    return urlProp.GetString();
                }
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("url", out var urlProp))
                {
                    return urlProp.GetString();
                }
            }
        }
        return null;
    }
}

/// <summary>
/// 优惠券信息
/// </summary>
public class CouponInfo
{
    /// <summary>
    /// 优惠券列表
    /// </summary>
    [JsonPropertyName("couponList")]
    public CouponList? CouponList { get; set; }
}

/// <summary>
/// 优惠券列表
/// </summary>
public class CouponList
{
    /// <summary>
    /// 优惠券（单个或数组）
    /// </summary>
    [JsonPropertyName("coupon")]
    public object? Coupon { get; set; }
}

/// <summary>
/// 优惠券明细
/// </summary>
public class Coupon
{
    /// <summary>
    /// 券面额
    /// </summary>
    [JsonPropertyName("discount")]
    public decimal Discount { get; set; }

    /// <summary>
    /// 券消费限额
    /// </summary>
    [JsonPropertyName("quota")]
    public decimal Quota { get; set; }

    /// <summary>
    /// 券链接
    /// </summary>
    [JsonPropertyName("link")]
    public string? Link { get; set; }

    /// <summary>
    /// 是否最优优惠券
    /// </summary>
    [JsonPropertyName("isBest")]
    public int IsBest { get; set; }
}

/// <summary>
/// 店铺信息
/// </summary>
public class ShopInfo
{
    /// <summary>
    /// 店铺名称
    /// </summary>
    [JsonPropertyName("shopName")]
    public string ShopName { get; set; } = string.Empty;

    /// <summary>
    /// 店铺ID
    /// </summary>
    [JsonPropertyName("shopId")]
    public long ShopId { get; set; }

    /// <summary>
    /// 店铺等级
    /// </summary>
    [JsonPropertyName("shopLevel")]
    public decimal? ShopLevel { get; set; }
}
