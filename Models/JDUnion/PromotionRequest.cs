using System.Text.Json.Serialization;

namespace ai_computer.Models.JDUnion;

/// <summary>
/// 推广链接请求
/// </summary>
public class PromotionRequest
{
    /// <summary>
    /// 推广物料url（商品链接、活动链接等）
    /// </summary>
    [JsonPropertyName("materialId")]
    public string MaterialId { get; set; } = string.Empty;

    /// <summary>
    /// 网站ID/APP ID
    /// </summary>
    [JsonPropertyName("siteId")]
    public string SiteId { get; set; } = string.Empty;

    /// <summary>
    /// 推广位id
    /// </summary>
    [JsonPropertyName("positionId")]
    public long? PositionId { get; set; }

    /// <summary>
    /// 子渠道标识
    /// </summary>
    [JsonPropertyName("subUnionId")]
    public string? SubUnionId { get; set; }

    /// <summary>
    /// 优惠券领取链接
    /// </summary>
    [JsonPropertyName("couponUrl")]
    public string? CouponUrl { get; set; }

    /// <summary>
    /// 场景ID (1:联盟商品, 2:京东主站商品)
    /// </summary>
    [JsonPropertyName("sceneId")]
    public int SceneId { get; set; } = 1;

    /// <summary>
    /// 是否生成短口令：1生成
    /// </summary>
    [JsonPropertyName("command")]
    public int? Command { get; set; }
}
