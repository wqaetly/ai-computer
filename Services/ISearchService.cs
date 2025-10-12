using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiComputer.Services;

/// <summary>
/// 搜索服务接口
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// 执行网络搜索
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="maxResults">最大结果数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索结果列表</returns>
    Task<List<SearchResult>> SearchAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 服务名称
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// 服务是否可用
    /// </summary>
    Task<bool> IsAvailableAsync();
}
