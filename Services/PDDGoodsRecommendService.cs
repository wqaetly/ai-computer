using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ai_computer.Models.PDD;

namespace ai_computer.Services;

/// <summary>
/// 拼多多商品推荐服务 - 负责商品搜索、筛选和推广链接生成的完整流程
/// </summary>
public class PDDGoodsRecommendService
{
    private readonly PDDUnionService _pddUnionService;
    private readonly PDDConfig _config;

    public PDDGoodsRecommendService(PDDUnionService pddUnionService)
    {
        _pddUnionService = pddUnionService;
        _config = new PDDConfig();
    }

    /// <summary>
    /// 智能推荐商品（搜索、筛选、生成推广链接一体化）
    /// </summary>
    /// <param name="keyword">搜索关键词</param>
    /// <param name="minPrice">最低价格（元）</param>
    /// <param name="maxPrice">最高价格（元）</param>
    /// <param name="maxCount">返回商品数量</param>
    /// <returns>推荐的商品列表（含推广链接）</returns>
    public async Task<List<PDDRecommendedProduct>> RecommendProductsAsync(
        string keyword,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int maxCount = 3)
    {
        try
        {
            // 1. 构建搜索请求
            var searchRequest = new GoodsSearchRequest
            {
                Keyword = keyword,
                PageSize = 30, // 获取更多商品用于筛选
                SortType = 6, // 按销量降序
                WithCoupon = true, // 优先有优惠券的商品
                Pid = _config.Pid
            };

            // 添加价格范围过滤
            if (minPrice.HasValue || maxPrice.HasValue)
            {
                searchRequest.RangeList = new List<RangeItem>
                {
                    new RangeItem
                    {
                        RangeId = 1, // 券后价
                        RangeFrom = (long)((minPrice ?? 0) * 100), // 转换为分
                        RangeTo = (long)((maxPrice ?? 999999) * 100) // 转换为分
                    }
                };
            }

            // 2. 搜索商品
            var searchResponse = await _pddUnionService.SearchGoodsAsync(searchRequest);

            // 详细日志输出
            Console.WriteLine($"[PDDGoodsRecommend] API Response received");

            if (searchResponse == null)
            {
                Console.WriteLine($"[PDDGoodsRecommend] 错误: 搜索响应为空");
                return new List<PDDRecommendedProduct>();
            }

            if (searchResponse.ErrorResponse != null)
            {
                Console.WriteLine($"[PDDGoodsRecommend] 错误: {searchResponse.ErrorResponse.ErrorMsg}");
                return new List<PDDRecommendedProduct>();
            }

            if (searchResponse.Data?.GoodsList == null ||
                searchResponse.Data.GoodsList.Count == 0)
            {
                Console.WriteLine($"[PDDGoodsRecommend] 警告: 关键词「{keyword}」未找到商品");
                return new List<PDDRecommendedProduct>();
            }

            var goodsList = searchResponse.Data.GoodsList;
            Console.WriteLine($"[PDDGoodsRecommend] 找到 {goodsList.Count} 个商品");

            // 3. 智能筛选和排序商品
            var filteredGoods = FilterAndRankGoods(goodsList, maxCount);

            // 4. 构建推荐商品列表
            var recommendedProducts = new List<PDDRecommendedProduct>();
            foreach (var goods in filteredGoods)
            {
                // 生成推广链接
                var promotionLink = await _pddUnionService.GeneratePromotionLinkAsync(
                    goods.GoodsSign,
                    goods.SearchId ?? searchResponse.Data.SearchId ?? "");

                var recommended = new PDDRecommendedProduct
                {
                    GoodsSign = goods.GoodsSign,
                    GoodsName = goods.GoodsName,
                    Price = goods.GetGroupPrice(),
                    CouponPrice = goods.GetCouponPrice(),
                    CouponDiscount = goods.CouponDiscount.HasValue ? goods.CouponDiscount.Value / 100m : 0,
                    Commission = goods.GetCommissionAmount(),
                    CommissionRate = goods.GetCommissionRate(),
                    ImageUrl = goods.GoodsImageUrl ?? goods.GoodsThumbnailUrl,
                    PromotionUrl = promotionLink ?? $"https://mobile.yangkeduo.com/goods.html?goods_sign={goods.GoodsSign}",
                    ShopName = goods.MallName,
                    Brand = goods.BrandName,
                    SalesCount = ParseSalesCount(goods.SalesTip),
                    HasCoupon = goods.HasCoupon,
                    MerchantType = goods.MerchantType,
                    ServScore = goods.ServTxt,
                    LogisticsScore = goods.LgstTxt,
                    DescScore = goods.DescTxt
                };

                recommendedProducts.Add(recommended);
            }

            Console.WriteLine($"[PDDGoodsRecommend] 成功推荐 {recommendedProducts.Count} 个商品");
            return recommendedProducts;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PDDGoodsRecommend] 推荐商品失败: {ex.Message}");
            Console.WriteLine($"[PDDGoodsRecommend] 异常堆栈: {ex.StackTrace}");
            return new List<PDDRecommendedProduct>();
        }
    }

    /// <summary>
    /// 智能筛选和排序商品
    /// </summary>
    private List<GoodsItem> FilterAndRankGoods(List<GoodsItem> goods, int maxCount)
    {
        return goods
            .Where(g => !string.IsNullOrEmpty(g.GoodsName))
            .Where(g => g.MinGroupPrice > 0) // 确保有有效价格
            .OrderByDescending(g => CalculateScore(g))
            .Take(maxCount)
            .ToList();
    }

    /// <summary>
    /// 计算商品综合评分
    /// </summary>
    private double CalculateScore(GoodsItem goods)
    {
        var score = 0.0;

        // 佣金比例权重 30%
        var commissionRate = (double)goods.PromotionRate / 1000.0;
        score += commissionRate * 30;

        // 销量权重 25%
        var salesCount = ParseSalesCount(goods.SalesTip);
        var salesScore = Math.Min(salesCount / 10000.0, 1.0);
        score += salesScore * 25;

        // 优惠券权重 20%
        if (goods.HasCoupon && goods.CouponDiscount.HasValue)
        {
            var couponValue = goods.CouponDiscount.Value / 100m; // 转换为元
            var couponScore = Math.Min((double)couponValue / 50.0, 1.0); // 最高50元券为满分
            score += couponScore * 20;
        }

        // 店铺类型权重 15% (旗舰店、专卖店、专营店优先)
        var shopScore = goods.MerchantType switch
        {
            3 => 1.0, // 旗舰店
            4 => 0.9, // 专卖店
            5 => 0.8, // 专营店
            2 => 0.6, // 企业
            _ => 0.3  // 个人和普通店
        };
        score += shopScore * 15;

        // 价格合理性权重 10% (券后价越低得分越高，但不能太低)
        var couponPrice = goods.GetCouponPrice();
        if (couponPrice > 0 && couponPrice < 10000) // 价格在合理范围内
        {
            var priceScore = 1.0 - Math.Min((double)couponPrice / 1000.0, 1.0);
            score += priceScore * 10;
        }

        return score;
    }

    /// <summary>
    /// 解析销量字符串
    /// </summary>
    private long ParseSalesCount(string? salesTip)
    {
        if (string.IsNullOrEmpty(salesTip))
            return 0;

        try
        {
            // 移除"已拼"等文字，只保留数字
            var numStr = new string(salesTip.Where(c => char.IsDigit(c) || c == '.').ToArray());

            // 处理"万"的情况
            if (salesTip.Contains("万"))
            {
                if (decimal.TryParse(numStr, out var num))
                {
                    return (long)(num * 10000);
                }
            }
            else
            {
                if (long.TryParse(numStr, out var num))
                {
                    return num;
                }
            }
        }
        catch
        {
            // 解析失败返回0
        }

        return 0;
    }
}

/// <summary>
/// 拼多多推荐商品信息（包含推广链接）
/// </summary>
public class PDDRecommendedProduct
{
    /// <summary>
    /// 商品标识
    /// </summary>
    public string GoodsSign { get; set; } = string.Empty;

    /// <summary>
    /// 商品名称
    /// </summary>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>
    /// 拼团价（元）
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 券后价（元）
    /// </summary>
    public decimal CouponPrice { get; set; }

    /// <summary>
    /// 优惠券金额（元）
    /// </summary>
    public decimal CouponDiscount { get; set; }

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
    /// 店铺类型
    /// </summary>
    public int MerchantType { get; set; }

    /// <summary>
    /// 服务分
    /// </summary>
    public string? ServScore { get; set; }

    /// <summary>
    /// 物流分
    /// </summary>
    public string? LogisticsScore { get; set; }

    /// <summary>
    /// 描述分
    /// </summary>
    public string? DescScore { get; set; }

    /// <summary>
    /// 获取显示价格（券后价）
    /// </summary>
    public decimal GetDisplayPrice()
    {
        return CouponPrice;
    }

    /// <summary>
    /// 获取价格标签文本
    /// </summary>
    public string GetPriceLabel()
    {
        if (HasCoupon && CouponDiscount > 0)
        {
            return $"券后价 ¥{CouponPrice:F2} (券{CouponDiscount:F0}元)";
        }

        return $"拼团价 ¥{Price:F2}";
    }

    /// <summary>
    /// 获取店铺类型名称
    /// </summary>
    public string GetMerchantTypeName()
    {
        return MerchantType switch
        {
            1 => "个人店",
            2 => "企业店",
            3 => "旗舰店",
            4 => "专卖店",
            5 => "专营店",
            6 => "普通店",
            _ => "未知"
        };
    }
}
