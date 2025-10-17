using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AiComputer.Models;
using AiComputer.Services;
using ai_computer.Models.JDUnion;

namespace ai_computer.Services;

/// <summary>
/// 统一的电商服务 - 根据配置的供应商调用对应的API
/// </summary>
public class UnifiedECommerceService : IECommerceService
{
    private readonly AppSettingsService _settings;
    private readonly JDGoodsRecommendService _jdService;
    private readonly PDDGoodsRecommendService _pddService;

    public UnifiedECommerceService()
    {
        _settings = AppSettingsService.Instance;

        // 初始化各个平台的服务
        var httpClient = new HttpClient();
        _jdService = new JDGoodsRecommendService(new JDUnionService(httpClient));
        _pddService = new PDDGoodsRecommendService(new PDDUnionService(httpClient));
    }

    /// <summary>
    /// 推荐商品（根据配置的供应商自动选择平台）
    /// </summary>
    public async Task<List<UnifiedProduct>> RecommendProductsAsync(
        string keyword,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int maxCount = 10)
    {
        try
        {
            Console.WriteLine($"[UnifiedECommerce] 当前电商供应商: {_settings.ECommerceProvider}");

            return _settings.ECommerceProvider switch
            {
                ECommerceProvider.JingDong => await RecommendFromJDAsync(keyword, minPrice, maxPrice, maxCount),
                ECommerceProvider.PinDuoDuo => await RecommendFromPDDAsync(keyword, minPrice, maxPrice, maxCount),
                ECommerceProvider.TaoBao => await RecommendFromTaoBaoAsync(keyword, minPrice, maxPrice, maxCount),
                _ => new List<UnifiedProduct>()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UnifiedECommerce] 推荐商品失败: {ex.Message}");
            return new List<UnifiedProduct>();
        }
    }

    /// <summary>
    /// 从京东推荐商品
    /// </summary>
    private async Task<List<UnifiedProduct>> RecommendFromJDAsync(
        string keyword,
        decimal? minPrice,
        decimal? maxPrice,
        int maxCount)
    {
        var jdProducts = await _jdService.RecommendProductsAsync(keyword, minPrice, maxPrice, maxCount);

        return jdProducts.Select(p => new UnifiedProduct
        {
            ProductId = p.SkuId.ToString(),
            ProductName = p.SkuName,
            OriginalPrice = p.Price,
            FinalPrice = p.GetDisplayPrice(),
            DiscountAmount = p.Price - p.GetDisplayPrice(),
            Commission = p.Commission,
            CommissionRate = p.CommissionRate,
            ImageUrl = p.ImageUrl,
            PromotionUrl = p.PromotionUrl,
            ShopName = p.ShopName,
            Brand = p.Brand,
            SalesCount = p.SalesCount,
            HasCoupon = p.HasCoupon,
            Platform = "京东"
        }).ToList();
    }

    /// <summary>
    /// 从拼多多推荐商品
    /// </summary>
    private async Task<List<UnifiedProduct>> RecommendFromPDDAsync(
        string keyword,
        decimal? minPrice,
        decimal? maxPrice,
        int maxCount)
    {
        var pddProducts = await _pddService.RecommendProductsAsync(keyword, minPrice, maxPrice, maxCount);

        return pddProducts.Select(p => new UnifiedProduct
        {
            ProductId = p.GoodsSign,
            ProductName = p.GoodsName,
            OriginalPrice = p.Price,
            FinalPrice = p.CouponPrice,
            DiscountAmount = p.CouponDiscount,
            Commission = p.Commission,
            CommissionRate = p.CommissionRate,
            ImageUrl = p.ImageUrl,
            PromotionUrl = p.PromotionUrl,
            ShopName = p.ShopName,
            Brand = p.Brand,
            SalesCount = p.SalesCount,
            HasCoupon = p.HasCoupon,
            Platform = "拼多多",
            ExtraInfo = $"店铺类型: {p.GetMerchantTypeName()}"
        }).ToList();
    }

    /// <summary>
    /// 从淘宝推荐商品（占位实现）
    /// </summary>
    private async Task<List<UnifiedProduct>> RecommendFromTaoBaoAsync(
        string keyword,
        decimal? minPrice,
        decimal? maxPrice,
        int maxCount)
    {
        // TODO: 实现淘宝API集成
        Console.WriteLine($"[UnifiedECommerce] 淘宝API尚未实现");
        await Task.CompletedTask;
        return new List<UnifiedProduct>();
    }
}
