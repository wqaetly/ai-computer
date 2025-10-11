using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AiComputer.Services;

/// <summary>
/// 网络搜索服务 - 使用公共 SearxNG 实例
/// </summary>
public class SearchService
{
    private readonly HttpClient _httpClient;
    private int _currentInstanceIndex = 0;
    private const string Version = "1.0.0";

    /// <summary>
    /// 公共 SearxNG 实例列表（按优先级排序）
    /// </summary>
    private readonly string[] _searxngInstances =
    {
        "https://search.pollorebozado.com",
        "https://searx.tiekoetter.com",
        "https://search.inetol.net",
        "https://searx.hu",
        "https://searx.work"
    };

    /// <summary>
    /// 构造函数
    /// </summary>
    public SearchService()
    {
        // 创建 HttpClientHandler 并配置 SSL
        var handler = new HttpClientHandler
        {
            // 允许所有 SSL 证书（用于公共实例）
            ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
            // 允许自动重定向
            AllowAutoRedirect = true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        // 添加完整的浏览器请求头，模拟真实浏览器访问
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/html, application/xhtml+xml, application/xml;q=0.9, image/avif, image/webp, image/apng, */*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        _httpClient.DefaultRequestHeaders.Add("DNT", "1");
        _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        _httpClient.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
    }

    /// <summary>
    /// 执行网络搜索
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="maxResults">最大结果数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索结果列表</returns>
    public async Task<List<SearchResult>> SearchAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<SearchResult>();

        // 尝试所有实例，直到成功
        for (int attempt = 0; attempt < _searxngInstances.Length; attempt++)
        {
            try
            {
                var instance = _searxngInstances[_currentInstanceIndex];
                var results = await SearchWithInstanceAsync(instance, query, maxResults, cancellationToken);

                if (results.Count > 0)
                {
                    Console.WriteLine($"✓ 使用实例: {instance}");
                    return results;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 实例 {_searxngInstances[_currentInstanceIndex]} 失败: {ex.Message}");
                // 切换到下一个实例
                _currentInstanceIndex = (_currentInstanceIndex + 1) % _searxngInstances.Length;
            }
        }

        // 所有实例都失败，返回空结果
        Console.WriteLine("⚠ 所有搜索实例都不可用");
        return new List<SearchResult>();
    }

    /// <summary>
    /// 使用指定实例进行搜索（模拟浏览器行为）
    /// </summary>
    private async Task<List<SearchResult>> SearchWithInstanceAsync(
        string instance,
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: 先访问主页，模拟真实浏览器行为
            Console.WriteLine($"[SearchService] 正在访问主页: {instance}");
            var homeResponse = await _httpClient.GetAsync(instance, cancellationToken);

            if (!homeResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"[SearchService] 主页访问失败: {homeResponse.StatusCode}");
                throw new HttpRequestException($"Failed to access home page: {homeResponse.StatusCode}");
            }

            var homeHtml = await homeResponse.Content.ReadAsStringAsync(cancellationToken);

            // Step 2: 查找并加载 CSS 文件（可选，但有助于模拟真实浏览器）
            var cssLinkMatch = Regex.Match(homeHtml, @"<link[^>]*rel=[""']stylesheet[""'][^>]*href=[""']([^""']*client[^""']*\.css)[""']", RegexOptions.IgnoreCase);
            if (cssLinkMatch.Success)
            {
                var cssPath = cssLinkMatch.Groups[1].Value;
                var cssUrl = new Uri(new Uri(instance), cssPath).ToString();
                Console.WriteLine($"[SearchService] 加载 CSS: {cssUrl}");

                try
                {
                    var cssRequest = new HttpRequestMessage(HttpMethod.Get, cssUrl);
                    cssRequest.Headers.Add("Referer", instance);
                    await _httpClient.SendAsync(cssRequest, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SearchService] CSS 加载失败（继续）: {ex.Message}");
                }
            }

            // Step 3: 添加随机延迟（10-400ms）
            await Task.Delay(Random.Shared.Next(10, 400), cancellationToken);

            // Step 4: 请求搜索页面（不使用 format=json，获取 HTML）
            var encodedQuery = Uri.EscapeDataString(query);
            var searchUrl = $"{instance}/search?q={encodedQuery}&language=zh-CN&safesearch=0";

            Console.WriteLine($"[SearchService] 正在搜索: {searchUrl}");

            var searchRequest = new HttpRequestMessage(HttpMethod.Get, searchUrl);
            searchRequest.Headers.Add("Referer", instance);

            var searchResponse = await _httpClient.SendAsync(searchRequest, cancellationToken);
            Console.WriteLine($"[SearchService] 响应状态: {(int)searchResponse.StatusCode} {searchResponse.StatusCode}");

            searchResponse.EnsureSuccessStatusCode();

            var html = await searchResponse.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[SearchService] 响应内容长度: {html.Length} 字符");

            // Step 5: 检测是否被重定向到主页
            if (html.Contains("body class=\"index_endpoint\""))
            {
                Console.WriteLine($"[SearchService] 被重定向到主页，等待后重试");
                await Task.Delay(2000, cancellationToken);
                // 递归重试一次
                return await SearchWithInstanceAsync(instance, query, maxResults, cancellationToken);
            }

            // Step 6: 从 HTML 解析搜索结果
            var results = ParseHtmlResults(html, maxResults);
            Console.WriteLine($"[SearchService] 成功解析 {results.Count} 个结果");

            return results;
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine($"[SearchService] HTTP 请求异常: {httpEx.Message}");
            if (httpEx.InnerException != null)
            {
                Console.WriteLine($"[SearchService] 内部异常: {httpEx.InnerException.Message}");
            }
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SearchService] 搜索失败: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 从 HTML 中解析搜索结果
    /// </summary>
    private List<SearchResult> ParseHtmlResults(string html, int maxResults)
    {
        var results = new List<SearchResult>();

        try
        {
            // 匹配 <article class="result"> 标签
            var resultBlockRegex = new Regex(
                @"<article[^>]*class=[""'][^""']*result[^""']*[""'][^>]*>(.*?)</article>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );

            var blockMatches = resultBlockRegex.Matches(html);

            foreach (Match blockMatch in blockMatches)
            {
                if (results.Count >= maxResults)
                    break;

                var blockHtml = blockMatch.Groups[1].Value;

                // 提取 URL (在 class="url_header" 的链接中)
                var urlMatch = Regex.Match(blockHtml,
                    @"<a[^>]*href=[""']([^""']+)[""'][^>]*class=[""'][^""']*url_header[^""']*[""']",
                    RegexOptions.IgnoreCase);

                if (!urlMatch.Success)
                {
                    // 尝试反向匹配 (class 在 href 之前)
                    urlMatch = Regex.Match(blockHtml,
                        @"<a[^>]*class=[""'][^""']*url_header[^""']*[""'][^>]*href=[""']([^""']+)[""']",
                        RegexOptions.IgnoreCase);
                }

                if (!urlMatch.Success)
                    continue;

                var url = urlMatch.Groups[1].Value;

                // 提取标题（通常在 h3 标签中）
                var titleMatch = Regex.Match(blockHtml,
                    @"<h3[^>]*>(.*?)</h3>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var title = titleMatch.Success
                    ? Regex.Replace(titleMatch.Groups[1].Value, @"<[^>]*>", "").Trim()
                    : "无标题";

                // 提取摘要 (在 class="content" 的段落中)
                var summaryMatch = Regex.Match(blockHtml,
                    @"<p[^>]*class=[""'][^""']*content[^""']*[""'][^>]*>(.*?)</p>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var summary = summaryMatch.Success
                    ? Regex.Replace(summaryMatch.Groups[1].Value, @"<[^>]*>", "").Trim()
                    : "无摘要";

                // 提取来源域名
                var source = "未知来源";
                try
                {
                    source = new Uri(url).Host;
                }
                catch { }

                results.Add(new SearchResult
                {
                    Title = title,
                    Url = url,
                    Snippet = summary,
                    Source = source
                });
            }

            if (html.Length > 50 && results.Count == 0)
            {
                Console.WriteLine($"[SearchService] 警告: HTML 内容长度 {html.Length}，但未解析到任何结果");
                // 可以在这里输出部分 HTML 用于调试
                // Console.WriteLine($"HTML 前 500 字符: {html.Substring(0, Math.Min(500, html.Length))}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SearchService] HTML 解析异常: {ex.Message}");
        }

        return results;
    }

    /// <summary>
    /// 格式化搜索结果为文本（用于传递给 AI）
    /// </summary>
    public static string FormatSearchResults(List<SearchResult> results)
    {
        if (results.Count == 0)
            return "未找到相关搜索结果。";

        var formatted = "搜索结果:\n\n";
        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            formatted += $"{i + 1}. **{result.Title}**\n";
            formatted += $"   来源: {result.Source}\n";
            formatted += $"   链接: {result.Url}\n";
            formatted += $"   摘要: {result.Snippet}\n\n";
        }

        return formatted;
    }
}

#region 搜索结果模型

/// <summary>
/// 搜索结果
/// </summary>
public class SearchResult
{
    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 摘要/片段
    /// </summary>
    public string Snippet { get; set; } = string.Empty;

    /// <summary>
    /// 来源网站
    /// </summary>
    public string Source { get; set; } = string.Empty;
}

#endregion
