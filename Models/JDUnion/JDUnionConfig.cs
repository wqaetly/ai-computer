namespace ai_computer.Models.JDUnion;

/// <summary>
/// 京东联盟配置
/// </summary>
public class JDUnionConfig
{
    /// <summary>
    /// 应用Key
    /// </summary>
    public string AppKey { get; set; } = "82ff2b606522d15c058a0abd411a0775";

    /// <summary>
    /// 应用密钥
    /// </summary>
    public string SecretKey { get; set; } = "da96033c11c34660b86e1427d26171bc";

    /// <summary>
    /// 推广位ID (PID格式: 联盟ID_应用ID_推广位ID)
    /// </summary>
    public string Pid { get; set; } = "2037427043_4102168306_3102458907";

    /// <summary>
    /// 网站ID
    /// </summary>
    public string SiteId { get; set; } = "4102168306";

    /// <summary>
    /// 推广位ID
    /// </summary>
    public long PositionId { get; set; } = 3102458907;

    /// <summary>
    /// API基础URL
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.jd.com/routerjson";
}
