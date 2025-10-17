namespace ai_computer.Models.PDD;

/// <summary>
/// 拼多多联盟配置
/// </summary>
public class PDDConfig
{
    /// <summary>
    /// 客户端ID
    /// </summary>
    public string ClientId { get; set; } = "8de7a099abc743348a31184daed8fc96";

    /// <summary>
    /// 客户端密钥
    /// </summary>
    public string ClientSecret { get; set; } = "3e0f19916a96583d3ae3602a39c7283bece69644";

    /// <summary>
    /// 访问令牌
    /// </summary>
    public string AccessToken { get; set; } = "44b763d20422472f802fdf3ec84b6f2134127550";

    /// <summary>
    /// 推广位ID
    /// </summary>
    public string Pid { get; set; } = "43592911_311190797";

    /// <summary>
    /// API基础URL
    /// </summary>
    public string ApiBaseUrl { get; set; } = "http://gw-api.pinduoduo.com/api/router";
}
