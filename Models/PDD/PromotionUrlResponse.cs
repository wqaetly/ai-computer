using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ai_computer.Models.PDD;

/// <summary>
/// 推广链接生成响应
/// </summary>
public class PromotionUrlResponse
{
    /// <summary>
    /// 推广链接响应数据
    /// </summary>
    [JsonPropertyName("goods_promotion_url_generate_response")]
    public PromotionUrlGenerateResponse? GoodsPromotionUrlGenerateResponse { get; set; }

    /// <summary>
    /// 错误响应
    /// </summary>
    [JsonPropertyName("error_response")]
    public ErrorResponse? ErrorResponse { get; set; }
}

/// <summary>
/// 推广链接生成响应数据
/// </summary>
public class PromotionUrlGenerateResponse
{
    /// <summary>
    /// 推广链接对象列表
    /// </summary>
    [JsonPropertyName("goods_promotion_url_list")]
    public List<GoodsPromotionUrl>? GoodsPromotionUrlList { get; set; }
}

/// <summary>
/// 商品推广链接对象
/// </summary>
public class GoodsPromotionUrl
{
    /// <summary>
    /// 普通长链
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// 短链接
    /// </summary>
    [JsonPropertyName("short_url")]
    public string? ShortUrl { get; set; }

    /// <summary>
    /// 手机端长链
    /// </summary>
    [JsonPropertyName("mobile_url")]
    public string? MobileUrl { get; set; }

    /// <summary>
    /// 手机端短链
    /// </summary>
    [JsonPropertyName("mobile_short_url")]
    public string? MobileShortUrl { get; set; }

    /// <summary>
    /// Schema URL（唤起APP）
    /// </summary>
    [JsonPropertyName("schema_url")]
    public string? SchemaUrl { get; set; }

    /// <summary>
    /// 商品推广分享图
    /// </summary>
    [JsonPropertyName("share_image_url")]
    public string? ShareImageUrl { get; set; }

    /// <summary>
    /// 微信小程序信息
    /// </summary>
    [JsonPropertyName("we_app_info")]
    public WeAppInfo? WeAppInfo { get; set; }

    /// <summary>
    /// 微信小程序码
    /// </summary>
    [JsonPropertyName("weixin_code")]
    public string? WeixinCode { get; set; }

    /// <summary>
    /// 微信小程序短链
    /// </summary>
    [JsonPropertyName("weixin_short_link")]
    public string? WeixinShortLink { get; set; }

    /// <summary>
    /// 微信小程序schema长链
    /// </summary>
    [JsonPropertyName("weixin_long_link")]
    public string? WeixinLongLink { get; set; }
}

/// <summary>
/// 微信小程序信息
/// </summary>
public class WeAppInfo
{
    /// <summary>
    /// 小程序id
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// Banner图
    /// </summary>
    [JsonPropertyName("banner_url")]
    public string? BannerUrl { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [JsonPropertyName("desc")]
    public string? Desc { get; set; }

    /// <summary>
    /// 小程序path值
    /// </summary>
    [JsonPropertyName("page_path")]
    public string? PagePath { get; set; }

    /// <summary>
    /// 来源名
    /// </summary>
    [JsonPropertyName("source_display_name")]
    public string? SourceDisplayName { get; set; }

    /// <summary>
    /// 小程序标题
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }

    /// <summary>
    /// 小程序图片
    /// </summary>
    [JsonPropertyName("we_app_icon_url")]
    public string? WeAppIconUrl { get; set; }
}
