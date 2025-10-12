using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ai_computer.Models.JDUnion;

namespace ai_computer.Services;

/// <summary>
/// 京东商品推荐服务 - 负责商品搜索、筛选和推广链接生成的完整流程
/// </summary>
public class JDGoodsRecommendService
{
    private readonly JDUnionService _jdUnionService;
    private readonly JDUnionConfig _config;

    public JDGoodsRecommendService(JDUnionService jdUnionService)
    {
        _jdUnionService = jdUnionService;
        _config = new JDUnionConfig();
    }

    /// <summary>
    /// 智能推荐商品（搜索、筛选、生成推广链接一体化）
    /// </summary>
    /// <param name="keyword">搜索关键词</param>
    /// <param name="minPrice">最低价格</param>
    /// <param name="maxPrice">最高价格</param>
    /// <param name="maxCount">返回商品数量</param>
    /// <returns>推荐的商品列表（含推广链接）</returns>
    public async Task<List<RecommendedProduct>> RecommendProductsAsync(
        string keyword,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int maxCount = 3)
    {
        try
        {
            // 1. 搜索商品
            var searchRequest = new GoodsQueryRequest
            {
                Keyword = keyword,
                PageSize = 30, // 获取更多商品用于筛选
                IsCoupon = 1, // 优先有优惠券的商品
                PriceFrom = minPrice,
                PriceTo = maxPrice,
                SortName = "inOrderCount30Days", // 按销量排序
                Sort = "desc",
                Pid = _config.Pid,
                Fields = "videoInfo,hotWords,similar,documentInfo"
            };

            var searchResponse = await _jdUnionService.SearchGoodsAsync(searchRequest);

            // 详细日志输出
            Console.WriteLine($"[JDGoodsRecommend] API Response - Code: {searchResponse?.Code}, Message: {searchResponse?.Message}");
            Console.WriteLine($"[JDGoodsRecommend] Data count: {searchResponse?.Data?.Count ?? 0}");

            if (searchResponse == null)
            {
                Console.WriteLine($"[JDGoodsRecommend] 错误: 搜索响应为空");
                return new List<RecommendedProduct>();
            }

            if (searchResponse.Code != 200)
            {
                Console.WriteLine($"[JDGoodsRecommend] 错误: API返回错误码 {searchResponse.Code}, 消息: {searchResponse.Message}");
                return new List<RecommendedProduct>();
            }

            if (searchResponse.Data == null || searchResponse.Data.Count == 0)
            {
                Console.WriteLine($"[JDGoodsRecommend] 警告: 关键词「{keyword}」未找到商品");
                return new List<RecommendedProduct>();
            }

            // 2. 智能筛选商品
            var filteredGoods = FilterAndRankGoods(searchResponse.Data, maxCount);

            // 3. 为筛选后的商品生成推广链接
            var recommendedProducts = new List<RecommendedProduct>();
            foreach (var goodsData in filteredGoods)
            {
                if (goodsData.GoodsResp == null) continue;

                var goods = goodsData.GoodsResp;

                // 生成推广链接
                var promotionLink = await GeneratePromotionLinkForGoodsAsync(goods);

                // 构建推荐商品对象
                var recommended = new RecommendedProduct
                {
                    SkuId = goods.SkuId,
                    SkuName = goods.SkuName,
                    Price = goods.PriceInfo?.Price ?? 0,
                    LowestPrice = goods.PriceInfo?.LowestPrice,
                    CouponPrice = goods.PriceInfo?.LowestCouponPrice,
                    Commission = goods.CommissionInfo?.Commission ?? 0,
                    CommissionRate = goods.CommissionInfo?.CommissionShare ?? 0,
                    ImageUrl = GetProductImageUrl(goods),
                    PromotionUrl = promotionLink,
                    ShopName = goods.ShopInfo?.ShopName,
                    Brand = goods.BrandName,
                    SalesCount = goods.InOrderCount30Days,
                    GoodRate = goods.GoodCommentsShare,
                    HasCoupon = goods.CouponInfo?.CouponList?.Coupon != null
                };

                recommendedProducts.Add(recommended);
            }

            return recommendedProducts;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"推荐商品失败: {ex.Message}");
            return new List<RecommendedProduct>();
        }
    }

    /// <summary>
    /// 智能筛选和排序商品
    /// </summary>
    private List<GoodsData> FilterAndRankGoods(List<GoodsData> goods, int maxCount)
    {
        return goods
            .Where(g => g.GoodsResp != null)
            .Where(g => g.GoodsResp!.PriceInfo != null && g.GoodsResp.CommissionInfo != null)
            .Where(g => g.GoodsResp!.GoodCommentsShare >= 85) // 好评率>=85%
            .OrderByDescending(g => CalculateScore(g.GoodsResp!))
            .Take(maxCount)
            .ToList();
    }

    /// <summary>
    /// 计算商品综合评分
    /// </summary>
    private double CalculateScore(GoodsInfo goods)
    {
        var score = 0.0;

        // 佣金比例权重 30%
        var commissionRate = (double)(goods.CommissionInfo?.CommissionShare ?? 0);
        score += (commissionRate / 100.0) * 30;

        // 好评率权重 30%
        var goodRate = (double)goods.GoodCommentsShare;
        score += (goodRate / 100.0) * 30;

        // 销量权重 20% (归一化到0-1)
        var salesScore = Math.Min(goods.InOrderCount30Days / 10000.0, 1.0);
        score += salesScore * 20;

        // 优惠券权重 10%
        if (goods.CouponInfo?.CouponList?.Coupon != null)
        {
            score += 10;
        }

        // 价格合理性权重 10% (券后价越接近促销价得分越高)
        if (goods.PriceInfo?.LowestCouponPrice != null && goods.PriceInfo.LowestPrice != null)
        {
            var priceRatio = (double)(goods.PriceInfo.LowestCouponPrice.Value / goods.PriceInfo.LowestPrice.Value);
            score += (1 - priceRatio) * 10;
        }

        return score;
    }

    /// <summary>
    /// 为商品生成推广链接
    /// </summary>
    private async Task<string> GeneratePromotionLinkForGoodsAsync(GoodsInfo goods)
    {
        try
        {
            // 获取最优优惠券链接
            string? couponUrl = null;
            if (goods.CouponInfo?.CouponList?.Coupon != null)
            {
                // 这里需要解析CouponList中的最优券
                // 简化处理：如果有券就使用
                couponUrl = ExtractBestCouponUrl(goods.CouponInfo);
            }

            var promotionRequest = new PromotionRequest
            {
                MaterialId = goods.MaterialUrl,
                SiteId = _config.SiteId,
                PositionId = _config.PositionId,
                CouponUrl = couponUrl,
                SceneId = 1
            };

            var response = await _jdUnionService.GeneratePromotionLinkAsync(promotionRequest);

            if (response != null && response.Code == 200 && response.Data != null)
            {
                return response.Data.ClickUrl;
            }

            // 如果生成失败，返回原始链接
            return $"https://{goods.MaterialUrl}";
        }
        catch
        {
            return $"https://{goods.MaterialUrl}";
        }
    }

    /// <summary>
    /// 提取最优优惠券URL
    /// </summary>
    private string? ExtractBestCouponUrl(CouponInfo couponInfo)
    {
        // TODO: 完善优惠券解析逻辑
        // 这里简化处理，需要根据实际API返回的数据结构来解析
        return null;
    }

    /// <summary>
    /// 获取商品图片URL
    /// </summary>
    private string? GetProductImageUrl(GoodsInfo goods)
    {
        // 优先使用白底图
        if (!string.IsNullOrEmpty(goods.ImageInfo?.WhiteImage))
        {
            return goods.ImageInfo.WhiteImage;
        }

        // 其次使用第一张图片
        var firstImage = goods.ImageInfo?.ImageList?.GetFirstImageUrl();
        if (!string.IsNullOrEmpty(firstImage))
        {
            return firstImage;
        }

        return null;
    }
}

/// <summary>
/// 推荐商品信息（包含推广链接）
/// </summary>
public class RecommendedProduct
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public long SkuId { get; set; }

    /// <summary>
    /// 商品名称
    /// </summary>
    public string SkuName { get; set; } = string.Empty;

    /// <summary>
    /// 原价
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 促销价
    /// </summary>
    public decimal? LowestPrice { get; set; }

    /// <summary>
    /// 券后价
    /// </summary>
    public decimal? CouponPrice { get; set; }

    /// <summary>
    /// 佣金
    /// </summary>
    public decimal Commission { get; set; }

    /// <summary>
    /// 佣金比例
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
    /// 30天销量
    /// </summary>
    public long SalesCount { get; set; }

    /// <summary>
    /// 好评率
    /// </summary>
    public decimal GoodRate { get; set; }

    /// <summary>
    /// 是否有优惠券
    /// </summary>
    public bool HasCoupon { get; set; }

    /// <summary>
    /// 获取显示价格（券后价 > 促销价 > 原价）
    /// </summary>
    public decimal GetDisplayPrice()
    {
        return CouponPrice ?? LowestPrice ?? Price;
    }

    /// <summary>
    /// 获取价格标签文本
    /// </summary>
    public string GetPriceLabel()
    {
        if (CouponPrice.HasValue)
        {
            return $"券后价 ¥{CouponPrice:F2}";
        }

        if (LowestPrice.HasValue)
        {
            return $"促销价 ¥{LowestPrice:F2}";
        }

        return $"¥{Price:F2}";
    }
}
