using System;
using System.Text;
using System.Threading.Tasks;
using ai_computer.Services;

namespace AiComputer.Services;

/// <summary>
/// 京东商品推荐工具辅助类 - 格式化推荐结果供AI使用
/// </summary>
public class JDRecommendToolHelper
{
    private readonly JDGoodsRecommendService _recommendService;

    public JDRecommendToolHelper(JDGoodsRecommendService recommendService)
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
        sb.AppendLine($"为您找到 {products.Count} 款「{keyword}」商品：\n");

        for (int i = 0; i < products.Count; i++)
        {
            var product = products[i];
            sb.AppendLine($"### 商品 {i + 1}: {product.SkuName}");
            sb.AppendLine();

            // 价格信息
            sb.AppendLine($"**价格**: {product.GetPriceLabel()}");
            if (product.Price > product.GetDisplayPrice())
            {
                sb.AppendLine($"原价: ¥{product.Price:F2}");
            }

            // 购买链接
            sb.AppendLine($"**购买**: [立即购买]({product.PromotionUrl})");

            // 图片URL（重要：供AI生成表格使用）
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                sb.AppendLine($"**图片**: {product.ImageUrl}");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

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
