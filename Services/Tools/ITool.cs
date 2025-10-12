using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AiComputer.Services.Tools;

/// <summary>
/// 工具接口
/// </summary>
public interface ITool
{
    /// <summary>
    /// 工具名称（用于AI调用）
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 工具描述
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 参数的 JSON Schema
    /// </summary>
    JsonDocument InputSchema { get; }

    /// <summary>
    /// 执行工具
    /// </summary>
    /// <param name="arguments">工具参数（JSON对象）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>工具执行结果</returns>
    Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken = default);
}
