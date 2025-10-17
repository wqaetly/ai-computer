using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ai_computer.Models.PDD;

/// <summary>
/// 推广链接生成请求
/// </summary>
public class PromotionUrlRequest
{
    /// <summary>
    /// 推广位ID (必填)
    /// </summary>
    [JsonPropertyName("p_id")]
    public string PId { get; set; } = string.Empty;

    /// <summary>
    /// 商品goodsSign列表
    /// </summary>
    [JsonPropertyName("goods_sign_list")]
    public List<string>? GoodsSignList { get; set; }

    /// <summary>
    /// 搜索id，建议填写，提高收益
    /// </summary>
    [JsonPropertyName("search_id")]
    public string? SearchId { get; set; }

    /// <summary>
    /// 是否生成短链接
    /// </summary>
    [JsonPropertyName("generate_short_url")]
    public bool? GenerateShortUrl { get; set; }

    /// <summary>
    /// 是否生成拼多多福利券微信小程序推广信息
    /// </summary>
    [JsonPropertyName("generate_we_app")]
    public bool? GenerateWeApp { get; set; }

    /// <summary>
    /// 多人团推广链接，true-多人团，false-单人团
    /// </summary>
    [JsonPropertyName("multi_group")]
    public bool? MultiGroup { get; set; }

    /// <summary>
    /// 自定义参数
    /// </summary>
    [JsonPropertyName("custom_parameters")]
    public string? CustomParameters { get; set; }

    /// <summary>
    /// 是否生成商品推广分享图
    /// </summary>
    [JsonPropertyName("generate_share_image")]
    public bool? GenerateShareImage { get; set; }

    /// <summary>
    /// 是否生成微信小程序码
    /// </summary>
    [JsonPropertyName("generate_weixin_code")]
    public bool? GenerateWeixinCode { get; set; }
}
