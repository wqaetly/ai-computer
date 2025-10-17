using System;
using System.Text;
using System.Threading.Tasks;
using ai_computer.Services;

namespace AiComputer.Services;

/// <summary>
/// 拼多多商品推荐工具辅助类 - 格式化推荐结果供AI使用
/// </summary>
public class PDDRecommendToolHelper
{
    private readonly PDDGoodsRecommendService _recommendService;

    public PDDRecommendToolHelper(PDDGoodsRecommendService recommendService)
    {
        _recommendService = recommendService;
    }

    /// <summary>
    /// 执行商品推荐并格式化结果
    /// </summary>
    public async Task<string> RecommendAndFormatAsync(
        string keyword,
        decimal? minPrice,
        decimal? maxPrice,
        int count)
    {
        var products = await _recommendService.RecommendProductsAsync(keyword, minPrice, maxPrice, count);

        if (products.Count == 0)
        {
            return $"抱歉，没有找到符合条件的「{keyword}」商品。建议：\n" +
                   "1. 尝试更通用的关键词\n" +
                   "2. 调整价格范围\n" +
                   "3. 检查关键词拼写";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"为您找到 {products.Count} 款「{keyword}」推荐商品（拼多多）：\n");

        for (int i = 0; i < products.Count; i++)
        {
            var product = products[i];
            sb.AppendLine($"### 商品 {i + 1}: {product.GoodsName}");
            sb.AppendLine();

            // 价格信息
            sb.AppendLine($"**{product.GetPriceLabel()}**");
            if (product.HasCoupon && product.Price > product.CouponPrice)
            {
                sb.AppendLine($"原价: ¥{product.Price:F2}");
            }

            // 商品属性
            if (!string.IsNullOrEmpty(product.Brand))
            {
                sb.AppendLine($"品牌: {product.Brand}");
            }

            if (!string.IsNullOrEmpty(product.ShopName))
            {
                sb.AppendLine($"店铺: {product.ShopName} ({product.GetMerchantTypeName()})");
            }

            // 销售信息
            sb.AppendLine($"销量: {FormatNumber(product.SalesCount)}");

            // 店铺评分
            if (!string.IsNullOrEmpty(product.ServScore))
            {
                sb.AppendLine($"服务: {product.ServScore}");
            }
            if (!string.IsNullOrEmpty(product.LogisticsScore))
            {
                sb.AppendLine($"物流: {product.LogisticsScore}");
            }
            if (!string.IsNullOrEmpty(product.DescScore))
            {
                sb.AppendLine($"描述: {product.DescScore}");
            }

            if (product.HasCoupon)
            {
                sb.AppendLine("✅ 有优惠券");
            }

            // 佣金信息（可选：是否显示给用户）
            sb.AppendLine($"预估佣金: ¥{product.Commission:F2} ({product.CommissionRate:F1}%)");

            // 购买链接（使用特殊标记，便于UI识别和渲染）
            sb.AppendLine($"\n📦 [立即购买]({product.PromotionUrl})");
            sb.AppendLine($"🔗 推广链接: {product.PromotionUrl}");

            // 图片URL（使用特殊标记）
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                sb.AppendLine($"🖼️ 图片: {product.ImageUrl}");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        sb.AppendLine("💡 **购物提示**：点击\"立即购买\"即可跳转拼多多购买，支持联盟返佣。");

        return sb.ToString();
    }

    /// <summary>
    /// 格式化数字（万、千）
    /// </summary>
    private string FormatNumber(long number)
    {
        if (number >= 10000)
        {
            return $"{number / 10000.0:F1}万";
        }

        if (number >= 1000)
        {
            return $"{number / 1000.0:F1}千";
        }

        return number.ToString();
    }
}
