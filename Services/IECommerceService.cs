using System.Collections.Generic;
using System.Threading.Tasks;

namespace ai_computer.Services;

/// <summary>
/// 电商平台服务统一接口
/// </summary>
public interface IECommerceService
{
    /// <summary>
    /// 推荐商品
    /// </summary>
    /// <param name="keyword">搜索关键词</param>
    /// <param name="minPrice">最低价格（元）</param>
    /// <param name="maxPrice">最高价格（元）</param>
    /// <param name="maxCount">返回商品数量</param>
    /// <returns>推荐的商品列表</returns>
    Task<List<UnifiedProduct>> RecommendProductsAsync(
        string keyword,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int maxCount = 3);
}

/// <summary>
/// 统一商品信息模型
/// </summary>
public class UnifiedProduct
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// 商品名称
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 原价（元）
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// 最终价格（券后价/促销价，元）
    /// </summary>
    public decimal FinalPrice { get; set; }

    /// <summary>
    /// 优惠金额（元）
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// 佣金金额（元）
    /// </summary>
    public decimal Commission { get; set; }

    /// <summary>
    /// 佣金比例（百分比）
    /// </summary>
    public decimal CommissionRate { get; set; }

    /// <summary>
    /// 商品图片URL
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// 推广链接
    /// </summary>
    public string PromotionUrl { get; set; } = string.Empty;

    /// <summary>
    /// 店铺名称
    /// </summary>
    public string? ShopName { get; set; }

    /// <summary>
    /// 品牌
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// 销量
    /// </summary>
    public long SalesCount { get; set; }

    /// <summary>
    /// 是否有优惠券
    /// </summary>
    public bool HasCoupon { get; set; }

    /// <summary>
    /// 电商平台
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// 额外信息（JSON格式，存储平台特有的信息）
    /// </summary>
    public string? ExtraInfo { get; set; }

    /// <summary>
    /// 获取价格显示文本
    /// </summary>
    public string GetPriceDisplay()
    {
        if (HasCoupon && DiscountAmount > 0)
        {
            return $"券后价 ¥{FinalPrice:F2} (省{DiscountAmount:F2}元)";
        }
        else if (FinalPrice < OriginalPrice)
        {
            return $"促销价 ¥{FinalPrice:F2} (省{OriginalPrice - FinalPrice:F2}元)";
        }
        return $"¥{FinalPrice:F2}";
    }
}
