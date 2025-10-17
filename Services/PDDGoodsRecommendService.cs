using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        int maxCount = 10)
    {
        try
        {
            // 1. 优化搜索关键词
            var optimizedKeywords = OptimizeKeyword(keyword);
            Console.WriteLine($"[PDDGoodsRecommend] 原始关键词: {keyword}");
            Console.WriteLine($"[PDDGoodsRecommend] 优化后关键词: {string.Join(", ", optimizedKeywords)}");

            // 尝试不同的关键词变体进行搜索
            List<PDDRecommendedProduct>? results = null;
            foreach (var searchKeyword in optimizedKeywords)
            {
                results = await SearchWithKeywordAsync(searchKeyword, minPrice, maxPrice, maxCount, keyword);
                if (results != null && results.Count > 0)
                {
                    Console.WriteLine($"[PDDGoodsRecommend] 使用关键词「{searchKeyword}」找到 {results.Count} 个商品");
                    break;
                }
                Console.WriteLine($"[PDDGoodsRecommend] 关键词「{searchKeyword}」未找到商品，尝试下一个变体");
            }

            return results ?? new List<PDDRecommendedProduct>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PDDGoodsRecommend] 推荐商品失败: {ex.Message}");
            Console.WriteLine($"[PDDGoodsRecommend] 异常堆栈: {ex.StackTrace}");
            return new List<PDDRecommendedProduct>();
        }
    }

    /// <summary>
    /// 使用指定关键词进行搜索
    /// </summary>
    private async Task<List<PDDRecommendedProduct>?> SearchWithKeywordAsync(
        string searchKeyword,
        decimal? minPrice,
        decimal? maxPrice,
        int maxCount,
        string originalKeyword)
    {
        try
        {
            // 1. 构建搜索请求
            var searchRequest = new GoodsSearchRequest
            {
                Keyword = searchKeyword,
                PageSize = 30, // 获取更多商品用于筛选
                SortType = 0, // 综合排序（获取更全面的商品）
                WithCoupon = false, // 不限制优惠券，获取更多商品
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

            if (searchResponse == null)
            {
                Console.WriteLine($"[PDDGoodsRecommend] 错误: 搜索响应为空");
                return null;
            }

            if (searchResponse.ErrorResponse != null)
            {
                Console.WriteLine($"[PDDGoodsRecommend] 错误: {searchResponse.ErrorResponse.ErrorMsg}");
                return null;
            }

            if (searchResponse.Data?.GoodsList == null ||
                searchResponse.Data.GoodsList.Count == 0)
            {
                Console.WriteLine($"[PDDGoodsRecommend] 警告: 关键词「{searchKeyword}」未找到商品");
                return null;
            }

            var goodsList = searchResponse.Data.GoodsList;
            Console.WriteLine($"[PDDGoodsRecommend] 找到 {goodsList.Count} 个原始商品");

            // 3. 直接使用平台返回的商品列表，不做任何过滤
            var finalGoods = goodsList.Take(maxCount).ToList();
            Console.WriteLine($"[PDDGoodsRecommend] 返回前 {finalGoods.Count} 个商品");

            // 4. 构建推荐商品列表
            var recommendedProducts = new List<PDDRecommendedProduct>();
            foreach (var goods in finalGoods)
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
            Console.WriteLine($"[PDDGoodsRecommend] 搜索异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 优化搜索关键词，生成多个变体
    /// </summary>
    private List<string> OptimizeKeyword(string keyword)
    {
        var keywords = new List<string> { keyword }; // 首先尝试原始关键词

        // 针对显卡的特殊优化
        if (keyword.Contains("显卡") || keyword.Contains("GPU"))
        {
            // RTX 5080 显卡 -> 多个变体
            if (keyword.Contains("5080"))
            {
                keywords.Add("RTX 5080");
                keywords.Add("5080");
                keywords.Add("RTX5080");
                keywords.Add("显卡 5080");
                // 如果搜索不到5080，尝试搜索4090（已上市的高端显卡）
                keywords.Add("RTX 4090 显卡");
                keywords.Add("显卡");  // 最后尝试通用关键词
            }
            else if (keyword.Contains("4090"))
            {
                keywords.Add("RTX 4090");
                keywords.Add("4090");
                keywords.Add("RTX4090");
            }
            else if (keyword.Contains("4080"))
            {
                keywords.Add("RTX 4080");
                keywords.Add("4080");
                keywords.Add("RTX4080");
            }
        }

        // 移除空字符串
        keywords.RemoveAll(string.IsNullOrWhiteSpace);

        return keywords;
    }

    /// <summary>
    /// 智能筛选和排序商品
    /// </summary>
    private List<GoodsItem> FilterAndRankGoods(List<GoodsItem> goods, int maxCount, string? keyword = null)
    {
        var filtered = goods
            .Where(g => !string.IsNullOrEmpty(g.GoodsName))
            .Where(g => g.MinGroupPrice > 0); // 确保有有效价格

        // 直接返回，不做任何额外过滤
        return filtered
            .OrderByDescending(g => CalculateScore(g, keyword))
            .Take(maxCount)
            .ToList();
    }

    /// <summary>
    /// 计算商品综合评分
    /// </summary>
    private double CalculateScore(GoodsItem goods, string? keyword = null)
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

    /// <summary>
    /// 检测是否为多规格商品（挂羊头卖狗肉）
    /// </summary>
    /// <param name="goodsName">商品标题</param>
    /// <param name="keyword">搜索关键词</param>
    /// <returns>true表示是多规格商品</returns>
    private bool IsMultiSpecProduct(string goodsName, string keyword)
    {
        if (string.IsNullOrEmpty(goodsName) || string.IsNullOrEmpty(keyword))
            return false;

        var lowerGoodsName = goodsName.ToLower();
        var lowerKeyword = keyword.ToLower();

        // 提取目标核心型号
        var targetModel = ExtractCoreModel(lowerKeyword);
        if (string.IsNullOrEmpty(targetModel))
            return false; // 没有明确型号，不进行多规格检测

        // 常见的多规格分隔符
        var separators = new[] { "/", "或", " 或 ", "|" };

        // 检测是否包含多个显卡型号
        foreach (var separator in separators)
        {
            if (lowerGoodsName.Contains(separator))
            {
                // 检查分隔符前后是否都包含显卡型号
                var parts = lowerGoodsName.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var modelCount = 0;
                    foreach (var part in parts)
                    {
                        if (ContainsGpuModel(part))
                        {
                            modelCount++;
                        }
                    }

                    // 如果分隔符两边都有显卡型号，则认为是多规格商品
                    if (modelCount >= 2)
                    {
                        return true;
                    }
                }
            }
        }

        // 特殊情况：针对带后缀的型号（如5070ti）
        // 如果搜索"5070ti"，但标题同时包含"5070"和其他分隔符，可能是多规格
        if (targetModel.Contains("ti") || targetModel.Contains("super"))
        {
            var baseModel = targetModel.Replace("ti", "").Replace("super", "");
            // 检查是否有"5070 "或"5070/"这样的模式（后面不是ti/super）
            foreach (var separator in separators)
            {
                var pattern = baseModel + separator;
                if (lowerGoodsName.Contains(pattern))
                {
                    // 检查分隔符后面是否跟着另一个型号
                    var index = lowerGoodsName.IndexOf(pattern);
                    if (index >= 0)
                    {
                        var afterSeparator = lowerGoodsName.Substring(index + pattern.Length);
                        if (ContainsGpuModel(afterSeparator))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        // 通用多规格标识词检测
        var multiSpecKeywords = new[] { "多规格", "多款", "多种规格" };
        foreach (var word in multiSpecKeywords)
        {
            if (lowerGoodsName.Contains(word))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 检测字符串是否包含显卡型号
    /// </summary>
    private bool ContainsGpuModel(string text)
    {
        var gpuModels = new[] { "4060", "4070", "4080", "4090", "5060", "5070", "5080", "5090" };
        return gpuModels.Any(model => text.Contains(model));
    }

    /// <summary>
    /// 检查商品标题是否包含核心型号（更宽松的匹配）
    /// </summary>
    /// <param name="goodsName">商品标题</param>
    /// <param name="keyword">搜索关键词</param>
    /// <returns>true表示包含核心型号</returns>
    private bool ContainsCoreModel(string goodsName, string keyword)
    {
        if (string.IsNullOrEmpty(goodsName) || string.IsNullOrEmpty(keyword))
            return true; // 如果没有关键词限制，默认通过

        var lowerGoodsName = goodsName.ToLower();
        var lowerKeyword = keyword.ToLower();

        // 提取核心型号
        var coreModel = ExtractCoreModel(lowerKeyword);
        if (string.IsNullOrEmpty(coreModel))
            return true; // 如果提取不到核心型号，默认通过

        // 检查标题是否包含核心型号
        // 针对带后缀的型号（如5070ti），必须完整匹配
        if (coreModel.Contains("ti") || coreModel.Contains("super"))
        {
            // 必须包含完整的型号（如"5070ti"）
            if (!lowerGoodsName.Contains(coreModel))
                return false;
        }
        else
        {
            // 普通型号只需包含数字部分即可（如"5070"）
            if (!lowerGoodsName.Contains(coreModel))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 提取关键词中的核心型号（单个）
    /// </summary>
    private string? ExtractCoreModel(string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
            return null;

        var lowerKeyword = keyword.ToLower();

        // 显卡型号提取（按优先级从长到短匹配，避免"5070ti"被识别为"5070"）
        var gpuPatterns = new[] {
            "5090ti", "5090super", "5090",
            "5080ti", "5080super", "5080",
            "5070ti", "5070super", "5070",
            "5060ti", "5060super", "5060",
            "4090ti", "4090super", "4090",
            "4080ti", "4080super", "4080",
            "4070ti", "4070super", "4070",
            "4060ti", "4060super", "4060"
        };

        foreach (var pattern in gpuPatterns)
        {
            if (lowerKeyword.Contains(pattern))
            {
                return pattern;
            }
        }

        // 如果没有找到GPU型号，返回null（表示不限制型号）
        return null;
    }

    /// <summary>
    /// 获取特定产品的最低合理价格（只过滤明显异常的低价，防止引流欺诈）
    /// </summary>
    /// <param name="keyword">搜索关键词</param>
    /// <returns>最低合理价格（元），如果没有限制则返回null</returns>
    private decimal? GetMinimumReasonablePrice(string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
            return null;

        // 提取核心型号
        var coreModel = ExtractCoreModel(keyword);
        if (string.IsNullOrEmpty(coreModel))
            return null;

        // 显卡最低价格（设置为市场价的30%，只过滤明显的引流商品）
        // 这些价格都是非常保守的底线，正常商品不会低于这个价格
        return coreModel switch
        {
            var m when m.Contains("5090") => 5000m,   // 5090最低5000元
            var m when m.Contains("5080") => 3000m,   // 5080最低3000元
            var m when m.Contains("5070ti") => 2000m, // 5070ti最低2000元
            var m when m.Contains("5070") => 1500m,   // 5070最低1500元
            var m when m.Contains("5060") => 1000m,   // 5060最低1000元
            var m when m.Contains("4090") => 4000m,   // 4090最低4000元
            var m when m.Contains("4080") => 2500m,   // 4080最低2500元
            var m when m.Contains("4070") => 1500m,   // 4070最低1500元
            var m when m.Contains("4060") => 1000m,   // 4060最低1000元
            _ => null // 其他产品不设限制
        };
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
