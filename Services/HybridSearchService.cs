using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AiComputer.Models;

namespace AiComputer.Services;

/// <summary>
/// 混合搜索服务 - 根据用户配置选择搜索服务,失败时降级到 SearxNG
/// </summary>
public class HybridSearchService : ISearchService, IDisposable
{
    private readonly BrowserSearchService _browserSearch;
    private readonly SearxngSearchService _searxngSearch;
    private readonly AppSettingsService _appSettings;
    private bool _browserAvailable = true;
    private int _browserFailureCount = 0;
    private const int MaxBrowserFailures = 3; // 连续失败3次后暂时禁用浏览器搜索
    private DateTime _lastBrowserCheck = DateTime.MinValue;
    private readonly TimeSpan _browserCheckInterval = TimeSpan.FromMinutes(5); // 5分钟后重试浏览器搜索

    public string ServiceName => "Hybrid Search (Configurable)";

    /// <summary>
    /// 当前使用的搜索引擎（用于浏览器搜索）
    /// </summary>
    public BrowserSearchService.SearchEngine CurrentEngine
    {
        get => _browserSearch.CurrentEngine;
        set => _browserSearch.CurrentEngine = value;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public HybridSearchService()
    {
        _browserSearch = new BrowserSearchService();
        _searxngSearch = new SearxngSearchService();
        _appSettings = AppSettingsService.Instance;
    }

    /// <summary>
    /// 检查服务是否可用
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        // 只要有一个服务可用即可
        var browserAvailable = await _browserSearch.IsAvailableAsync();
        var searxngAvailable = await _searxngSearch.IsAvailableAsync();
        return browserAvailable || searxngAvailable;
    }

    /// <summary>
    /// 执行搜索 - 根据用户配置选择搜索服务,失败时降级到 SearxNG
    /// </summary>
    public async Task<List<SearchResult>> SearchAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<SearchResult>();

        // 获取用户配置的搜索服务商
        var provider = _appSettings.SearchProvider;
        Console.WriteLine($"[HybridSearchService] 使用配置的搜索服务: {provider}");

        // 如果配置的是 SearxNG，直接使用
        if (provider == SearchProvider.SearxNG)
        {
            return await SearchWithSearxngAsync(query, maxResults, cancellationToken);
        }

        // 如果配置的是浏览器搜索（Bing/Baidu）
        // 先设置浏览器搜索引擎
        _browserSearch.CurrentEngine = provider switch
        {
            SearchProvider.Bing => BrowserSearchService.SearchEngine.Bing,
            SearchProvider.Baidu => BrowserSearchService.SearchEngine.Baidu,
            _ => BrowserSearchService.SearchEngine.Baidu // 默认百度
        };

        // 检查是否应该重试浏览器搜索
        if (!_browserAvailable && DateTime.UtcNow - _lastBrowserCheck > _browserCheckInterval)
        {
            Console.WriteLine("[HybridSearchService] 5分钟已过，重新启用浏览器搜索");
            _browserAvailable = true;
            _browserFailureCount = 0;
        }

        // 尝试浏览器搜索
        if (_browserAvailable)
        {
            try
            {
                Console.WriteLine($"[HybridSearchService] 尝试使用浏览器搜索 ({_browserSearch.CurrentEngine})...");
                var results = await _browserSearch.SearchAsync(query, maxResults, cancellationToken);

                if (results.Count > 0)
                {
                    // 搜索成功，重置失败计数
                    _browserFailureCount = 0;
                    Console.WriteLine($"[HybridSearchService] ✓ 浏览器搜索成功，返回 {results.Count} 个结果");
                    return results;
                }

                // 结果为空，视为失败
                _browserFailureCount++;
                Console.WriteLine($"[HybridSearchService] ⚠ 浏览器搜索返回空结果 (失败 {_browserFailureCount}/{MaxBrowserFailures})");
            }
            catch (Exception ex)
            {
                _browserFailureCount++;
                Console.WriteLine($"[HybridSearchService] ✗ 浏览器搜索异常: {ex.Message} (失败 {_browserFailureCount}/{MaxBrowserFailures})");
            }

            // 检查是否需要暂时禁用浏览器搜索
            if (_browserFailureCount >= MaxBrowserFailures)
            {
                _browserAvailable = false;
                _lastBrowserCheck = DateTime.UtcNow;
                Console.WriteLine($"[HybridSearchService] ⚠ 浏览器搜索连续失败 {MaxBrowserFailures} 次，暂时禁用，{_browserCheckInterval.TotalMinutes} 分钟后重试");
            }
        }

        // 降级到 SearxNG
        Console.WriteLine("[HybridSearchService] 降级到 SearxNG 搜索...");
        return await SearchWithSearxngAsync(query, maxResults, cancellationToken);
    }

    /// <summary>
    /// 使用 SearxNG 搜索
    /// </summary>
    private async Task<List<SearchResult>> SearchWithSearxngAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = await _searxngSearch.SearchAsync(query, maxResults, cancellationToken);

            if (results.Count > 0)
            {
                Console.WriteLine($"[HybridSearchService] ✓ SearxNG 搜索成功，返回 {results.Count} 个结果");
                return results;
            }

            Console.WriteLine("[HybridSearchService] ⚠ SearxNG 搜索返回空结果");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HybridSearchService] ✗ SearxNG 搜索失败: {ex.Message}");
        }

        return new List<SearchResult>();
    }

    /// <summary>
    /// 获取当前状态信息
    /// </summary>
    public string GetStatusInfo()
    {
        var status = $"浏览器搜索: {(_browserAvailable ? "可用" : "暂时禁用")}";
        if (!_browserAvailable)
        {
            var remainingTime = _browserCheckInterval - (DateTime.UtcNow - _lastBrowserCheck);
            if (remainingTime.TotalSeconds > 0)
            {
                status += $" (将在 {remainingTime.TotalMinutes:F1} 分钟后重试)";
            }
        }
        status += $"\n当前搜索引擎: {_browserSearch.CurrentEngine}";
        status += $"\n失败计数: {_browserFailureCount}/{MaxBrowserFailures}";
        return status;
    }

    /// <summary>
    /// 手动重置浏览器搜索状态
    /// </summary>
    public void ResetBrowserStatus()
    {
        _browserAvailable = true;
        _browserFailureCount = 0;
        _lastBrowserCheck = DateTime.MinValue;
        Console.WriteLine("[HybridSearchService] 浏览器搜索状态已手动重置");
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _browserSearch?.Dispose();
        GC.SuppressFinalize(this);
    }
}
