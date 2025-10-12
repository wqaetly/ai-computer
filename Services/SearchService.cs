using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AiComputer.Services;

/// <summary>
/// 网络搜索服务 - 使用公共 SearxNG 实例
/// </summary>
public class SearxngSearchService : ISearchService
{
    private readonly HttpClient _httpClient;
    private readonly InstanceTestService _instanceTestService;
    private const string Version = "1.0.0";
    private const string AvailableInstancesJsonPath = "Services/available_instances.json";
    private const int MaxRetries = 3;

    public string ServiceName => "SearxNG Search";

    /// <summary>
    /// 检查服务是否可用
    /// </summary>
    public Task<bool> IsAvailableAsync()
    {
        try
        {
            var instances = LoadAvailableInstances();
            return Task.FromResult(instances.Count > 0);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 浏览器类型枚举
    /// </summary>
    private enum BrowserType
    {
        Chrome,
        Firefox,
        Edge,
        Safari
    }

    /// <summary>
    /// 操作系统类型
    /// </summary>
    private static readonly string[] WindowsVersions = new[] { "Windows NT 10.0", "Windows NT 11.0" };
    private static readonly string[] MacOSVersions = new[] { "10_15_7", "11_0_0", "12_0_0", "13_0_0", "14_0_0" };

    /// <summary>
    /// 语言偏好列表
    /// </summary>
    private static readonly string[] AcceptLanguages = new[]
    {
        "zh-CN,zh;q=0.9,en;q=0.8",
        "zh-CN,zh;q=0.9,en-US;q=0.8,en;q=0.7",
        "en-US,en;q=0.9",
        "en-GB,en;q=0.9,en-US;q=0.8",
        "zh-TW,zh;q=0.9,en;q=0.8",
        "ja-JP,ja;q=0.9,en;q=0.8",
    };

    /// <summary>
    /// 动态生成随机的 User-Agent
    /// </summary>
    private static string GenerateRandomUserAgent()
    {
        var browserType = (BrowserType)Random.Shared.Next(4);

        switch (browserType)
        {
            case BrowserType.Chrome:
                var chromeVersion = Random.Shared.Next(115, 122);
                var chromeBuild = Random.Shared.Next(0, 10);
                var chromeOS = Random.Shared.Next(3);

                if (chromeOS == 0) // Windows
                {
                    var chromeWinVer = WindowsVersions[Random.Shared.Next(WindowsVersions.Length)];
                    return $"Mozilla/5.0 ({chromeWinVer}; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion}.0.{chromeBuild}.0 Safari/537.36";
                }
                else if (chromeOS == 1) // macOS
                {
                    var chromeMacVer = MacOSVersions[Random.Shared.Next(MacOSVersions.Length)];
                    return $"Mozilla/5.0 (Macintosh; Intel Mac OS X {chromeMacVer}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion}.0.{chromeBuild}.0 Safari/537.36";
                }
                else // Linux
                {
                    return $"Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion}.0.{chromeBuild}.0 Safari/537.36";
                }

            case BrowserType.Firefox:
                var ffVersion = Random.Shared.Next(115, 122);
                var ffOS = Random.Shared.Next(2);

                if (ffOS == 0) // Windows
                {
                    var ffWinVer = WindowsVersions[Random.Shared.Next(WindowsVersions.Length)];
                    return $"Mozilla/5.0 ({ffWinVer}; Win64; x64; rv:{ffVersion}.0) Gecko/20100101 Firefox/{ffVersion}.0";
                }
                else // macOS
                {
                    var ffMacVer = MacOSVersions[Random.Shared.Next(MacOSVersions.Length)];
                    return $"Mozilla/5.0 (Macintosh; Intel Mac OS X {ffMacVer}; rv:{ffVersion}.0) Gecko/20100101 Firefox/{ffVersion}.0";
                }

            case BrowserType.Edge:
                var edgeVersion = Random.Shared.Next(115, 122);
                var edgeBuild = Random.Shared.Next(0, 10);
                var edgeWinVer = WindowsVersions[Random.Shared.Next(WindowsVersions.Length)];
                return $"Mozilla/5.0 ({edgeWinVer}; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{edgeVersion}.0.{edgeBuild}.0 Safari/537.36 Edg/{edgeVersion}.0.{edgeBuild}.0";

            case BrowserType.Safari:
                var safariVersion = Random.Shared.Next(16, 18);
                var safariMinor = Random.Shared.Next(0, 5);
                var webkitVersion = 605 + Random.Shared.Next(0, 3);
                var safariMacVer = MacOSVersions[Random.Shared.Next(MacOSVersions.Length)];
                return $"Mozilla/5.0 (Macintosh; Intel Mac OS X {safariMacVer}) AppleWebKit/{webkitVersion}.1.15 (KHTML, like Gecko) Version/{safariVersion}.{safariMinor} Safari/{webkitVersion}.1.15";

            default:
                return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        }
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public SearxngSearchService()
    {
        _instanceTestService = new InstanceTestService();

        // 创建 HttpClientHandler 并配置 SSL
        var handler = new HttpClientHandler
        {
            // 允许所有 SSL 证书（用于公共实例）
            ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
            // 允许自动重定向
            AllowAutoRedirect = true,
            // 自动解压缩
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.Brotli
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        // 注意：不再在构造函数中添加默认请求头
        // 每次请求时动态添加随机的请求头
    }

    /// <summary>
    /// 创建随机的 HTTP 请求消息（伪装成不同用户）
    /// </summary>
    private HttpRequestMessage CreateRandomRequest(string url, string? referer = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // 动态生成随机 User-Agent
        var userAgent = GenerateRandomUserAgent();
        request.Headers.Add("User-Agent", userAgent);

        // 随机选择语言
        var acceptLanguage = AcceptLanguages[Random.Shared.Next(AcceptLanguages.Length)];
        request.Headers.Add("Accept-Language", acceptLanguage);

        // 基础请求头 - 随机化 Accept 的顺序
        var acceptFormats = new List<string>
        {
            "text/html",
            "application/xhtml+xml",
            "application/xml;q=0.9",
            "image/avif",
            "image/webp",
            "image/apng",
            "*/*;q=0.8"
        };

        // 随机打乱一些格式的顺序（保持 text/html 在前）
        var randomAccept = $"{acceptFormats[0]},{acceptFormats[1]},{acceptFormats[2]},";
        var remainingFormats = acceptFormats.Skip(3).OrderBy(_ => Random.Shared.Next()).ToList();
        randomAccept += string.Join(",", remainingFormats);
        request.Headers.Add("Accept", randomAccept);

        // Accept-Encoding - 随机决定支持哪些编码
        var encodings = new List<string> { "gzip", "deflate", "br" };
        if (Random.Shared.Next(5) > 0) // 80% 的概率支持所有编码
        {
            request.Headers.Add("Accept-Encoding", string.Join(", ", encodings));
        }
        else
        {
            request.Headers.Add("Accept-Encoding", string.Join(", ", encodings.Take(2)));
        }

        // 随机决定是否添加 DNT (60% 概率)
        if (Random.Shared.Next(10) < 6)
        {
            request.Headers.Add("DNT", "1");
        }

        request.Headers.Add("Connection", "keep-alive");
        request.Headers.Add("Upgrade-Insecure-Requests", "1");

        // Sec-Fetch 系列（模拟浏览器行为）
        request.Headers.Add("Sec-Fetch-Dest", "document");
        request.Headers.Add("Sec-Fetch-Mode", "navigate");
        request.Headers.Add("Sec-Fetch-Site", referer != null ? "same-origin" : "none");
        request.Headers.Add("Sec-Fetch-User", "?1");

        // 随机决定是否添加 Cache-Control (50% 概率)
        if (Random.Shared.Next(2) == 0)
        {
            var cacheOptions = new[] { "max-age=0", "no-cache", "no-cache, no-store" };
            request.Headers.Add("Cache-Control", cacheOptions[Random.Shared.Next(cacheOptions.Length)]);
        }

        // 随机添加 Chrome 特有的 sec-ch-ua 系列请求头（如果 UA 是 Chrome）
        if (userAgent.Contains("Chrome") && !userAgent.Contains("Edg"))
        {
            var chromeVersionMatch = System.Text.RegularExpressions.Regex.Match(userAgent, @"Chrome/(\d+)");
            if (chromeVersionMatch.Success)
            {
                var version = chromeVersionMatch.Groups[1].Value;
                request.Headers.Add("sec-ch-ua", $"\"Chromium\";v=\"{version}\", \"Google Chrome\";v=\"{version}\", \"Not-A.Brand\";v=\"99\"");
                request.Headers.Add("sec-ch-ua-mobile", "?0");

                // 随机选择平台
                var platforms = new[] { "Windows", "macOS", "Linux" };
                var platform = platforms[Random.Shared.Next(platforms.Length)];
                request.Headers.Add("sec-ch-ua-platform", $"\"{platform}\"");
            }
        }

        // 如果有 Referer，添加它
        if (referer != null)
        {
            request.Headers.Add("Referer", referer);
        }

        Console.WriteLine($"[SearchService] 使用 User-Agent: {userAgent.Substring(0, Math.Min(60, userAgent.Length))}...");

        return request;
    }

    /// <summary>
    /// 从 JSON 文件加载可用实例列表
    /// </summary>
    private List<string> LoadAvailableInstances()
    {
        try
        {
            // 首先尝试从 JSON 文件加载
            var instances = _instanceTestService.LoadResultsFromJson();

            // 只使用状态为 Available 的实例
            var availableInstances = instances
                .Where(i => i.Status == InstanceStatus.Available)
                .OrderBy(i => i.ResponseTime) // 按响应时间排序，优先使用快速实例
                .Select(i => i.Url)
                .ToList();

            if (availableInstances.Count > 0)
            {
                Console.WriteLine($"[SearchService] 从 JSON 加载了 {availableInstances.Count} 个可用实例");
                return availableInstances;
            }

            Console.WriteLine("[SearchService] JSON 文件中没有可用实例，使用默认实例");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SearchService] 加载实例失败: {ex.Message}，使用默认实例");
        }

        // 如果 JSON 加载失败，返回默认实例列表
        return new List<string>
        {
            "https://search.pollorebozado.com",
            "https://searx.tiekoetter.com",
            "https://search.inetol.net"
        };
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

        // 加载可用实例列表
        var availableInstances = LoadAvailableInstances();

        if (availableInstances.Count == 0)
        {
            Console.WriteLine("⚠ 没有可用的搜索实例，请前往【联网搜索测试】页面重新获取并测试实例");
            return new List<SearchResult>();
        }

        // 创建已使用实例的集合，避免重复使用
        var usedInstances = new HashSet<string>();

        // 最多重试 MaxRetries 次
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                // 从可用实例中随机选择一个未使用过的实例
                var availableToChoose = availableInstances.Where(i => !usedInstances.Contains(i)).ToList();

                if (availableToChoose.Count == 0)
                {
                    Console.WriteLine("⚠ 所有可用实例都已尝试过");
                    break;
                }

                var randomIndex = Random.Shared.Next(availableToChoose.Count);
                var selectedInstance = availableToChoose[randomIndex];
                usedInstances.Add(selectedInstance);

                Console.WriteLine($"[SearchService] 尝试 #{attempt + 1}/{MaxRetries}，使用实例: {selectedInstance}");

                var results = await SearchWithInstanceAsync(selectedInstance, query, maxResults, cancellationToken);

                if (results.Count > 0)
                {
                    Console.WriteLine($"✓ 搜索成功！使用实例: {selectedInstance}，返回 {results.Count} 个结果");
                    return results;
                }

                Console.WriteLine($"⚠ 实例 {selectedInstance} 返回了空结果，尝试下一个实例");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 实例搜索失败: {ex.Message}");
            }
        }

        // 所有尝试都失败，返回空结果
        Console.WriteLine($"⚠ 已尝试 {MaxRetries} 次，所有搜索实例都未能返回结果");
        Console.WriteLine("💡 建议：请前往【联网搜索测试】页面，点击重新加载按钮从 searx.space 获取最新实例列表并进行测试");
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
            // Step 1: 先访问主页，模拟真实浏览器行为（使用随机请求头）
            Console.WriteLine($"[SearchService] 正在访问主页: {instance}");
            var homeRequest = CreateRandomRequest(instance);
            var homeResponse = await _httpClient.SendAsync(homeRequest, cancellationToken);

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
                    // 使用随机请求头加载 CSS
                    var cssRequest = CreateRandomRequest(cssUrl, instance);
                    await _httpClient.SendAsync(cssRequest, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SearchService] CSS 加载失败（继续）: {ex.Message}");
                }
            }

            // Step 3: 添加随机延迟（200-800ms，增加延迟以更像人类行为）
            await Task.Delay(Random.Shared.Next(200, 800), cancellationToken);

            // Step 4: 请求搜索页面（不使用 format=json，获取 HTML，使用新的随机请求头）
            var encodedQuery = Uri.EscapeDataString(query);
            var searchUrl = $"{instance}/search?q={encodedQuery}&language=zh-CN&safesearch=0";

            Console.WriteLine($"[SearchService] 正在搜索: {searchUrl}");

            // 使用新的随机请求头进行搜索
            var searchRequest = CreateRandomRequest(searchUrl, instance);

            var searchResponse = await _httpClient.SendAsync(searchRequest, cancellationToken);
            Console.WriteLine($"[SearchService] 响应状态: {(int)searchResponse.StatusCode} {searchResponse.StatusCode}");

            searchResponse.EnsureSuccessStatusCode();

            var html = await searchResponse.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[SearchService] 响应内容长度: {html.Length} 字符");

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

/// <summary>
/// 搜索结果格式化工具
/// </summary>
public static class SearchResultFormatter
{
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

#endregion
