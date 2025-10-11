using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AiComputer.Services;

/// <summary>
/// SearXNG 实例测试服务
/// </summary>
public class SearxngInstanceTester
{
    private readonly HttpClient _httpClient;
    private readonly int _timeoutSeconds;
    private readonly int _maxConcurrentTests;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="timeoutSeconds">每个实例的超时时间（秒）</param>
    /// <param name="maxConcurrentTests">最大并发测试数</param>
    public SearxngInstanceTester(int timeoutSeconds = 10, int maxConcurrentTests = 10)
    {
        _timeoutSeconds = timeoutSeconds;
        _maxConcurrentTests = maxConcurrentTests;

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
            AllowAutoRedirect = true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        // 添加完整的浏览器请求头，模拟真实浏览器访问
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
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
    /// 从 YAML 文件中解析所有实例 URL
    /// </summary>
    /// <param name="yamlFilePath">YAML 文件路径</param>
    /// <returns>实例 URL 列表</returns>
    public List<string> ParseInstancesFromYaml(string yamlFilePath)
    {
        var instances = new List<string>();
        var lines = File.ReadAllLines(yamlFilePath);

        // 正则表达式匹配 URL
        var urlPattern = new Regex(@"^(https?://[^\s:]+)", RegexOptions.Compiled);

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            var match = urlPattern.Match(trimmed);

            if (match.Success)
            {
                var url = match.Groups[1].Value;
                // 过滤掉 .onion 地址（需要 Tor）
                if (!url.Contains(".onion"))
                {
                    instances.Add(url);
                }
            }
        }

        return instances.Distinct().OrderBy(u => u).ToList();
    }

    /// <summary>
    /// 测试单个实例是否可用
    /// </summary>
    /// <param name="instanceUrl">实例 URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>测试结果</returns>
    public async Task<InstanceTestResult> TestInstanceAsync(
        string instanceUrl,
        CancellationToken cancellationToken = default)
    {
        var result = new InstanceTestResult
        {
            Url = instanceUrl,
            IsAvailable = false,
            TestedAt = DateTime.UtcNow
        };

        try
        {
            var startTime = DateTime.UtcNow;

            // 尝试访问搜索 API
            var testQuery = "test";
            var searchUrl = $"{instanceUrl}/search?q={testQuery}&format=json&language=en";

            var response = await _httpClient.GetAsync(searchUrl, cancellationToken);
            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            result.ResponseTimeMs = (int)responseTime;
            result.StatusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                // 尝试解析 JSON 以确认是有效的 SearXNG 实例
                try
                {
                    var json = JsonDocument.Parse(content);
                    if (json.RootElement.TryGetProperty("results", out _))
                    {
                        result.IsAvailable = true;
                        result.Message = "OK";
                    }
                    else
                    {
                        result.Message = "Invalid SearXNG response format";
                    }
                }
                catch (JsonException)
                {
                    result.Message = "Invalid JSON response";
                }
            }
            else if ((int)response.StatusCode == 429)
            {
                // 429 表示速率限制，但实例是在线的
                result.IsAvailable = true;
                result.Message = "OK (Rate Limited)";
            }
            else
            {
                result.Message = $"HTTP {response.StatusCode}";
            }
        }
        catch (TaskCanceledException)
        {
            result.Message = "Timeout";
        }
        catch (HttpRequestException ex)
        {
            result.Message = $"Connection failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            result.Message = $"Error: {ex.GetType().Name}";
        }

        return result;
    }

    /// <summary>
    /// 批量测试所有实例
    /// </summary>
    /// <param name="instances">实例 URL 列表</param>
    /// <param name="progressCallback">进度回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>测试结果列表</returns>
    public async Task<List<InstanceTestResult>> TestAllInstancesAsync(
        List<string> instances,
        Action<int, int>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<InstanceTestResult>();
        var semaphore = new SemaphoreSlim(_maxConcurrentTests);
        var completedCount = 0;

        var tasks = instances.Select(async instance =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = await TestInstanceAsync(instance, cancellationToken);

                lock (results)
                {
                    results.Add(result);
                    completedCount++;
                    progressCallback?.Invoke(completedCount, instances.Count);
                }

                return result;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        return results.OrderByDescending(r => r.IsAvailable)
                     .ThenBy(r => r.ResponseTimeMs)
                     .ToList();
    }

    /// <summary>
    /// 保存测试结果为 JSON 文件
    /// </summary>
    /// <param name="results">测试结果列表</param>
    /// <param name="outputPath">输出文件路径</param>
    public void SaveResultsToJson(List<InstanceTestResult> results, string outputPath)
    {
        var summary = new InstanceTestSummary
        {
            TestedAt = DateTime.UtcNow,
            TotalInstances = results.Count,
            AvailableInstances = results.Count(r => r.IsAvailable),
            UnavailableInstances = results.Count(r => !r.IsAvailable),
            Results = results
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        var json = JsonSerializer.Serialize(summary, options);
        File.WriteAllText(outputPath, json);
    }

    /// <summary>
    /// 仅保存可用实例的 URL 列表（用于运行时）
    /// </summary>
    /// <param name="results">测试结果列表</param>
    /// <param name="outputPath">输出文件路径</param>
    public void SaveAvailableInstancesOnly(List<InstanceTestResult> results, string outputPath)
    {
        var availableInstances = results
            .Where(r => r.IsAvailable)
            .OrderBy(r => r.ResponseTimeMs)
            .Select(r => r.Url)
            .ToList();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(availableInstances, options);
        File.WriteAllText(outputPath, json);
    }
}

#region 测试结果模型

/// <summary>
/// 实例测试结果
/// </summary>
public class InstanceTestResult
{
    /// <summary>
    /// 实例 URL
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 是否可用
    /// </summary>
    [JsonPropertyName("is_available")]
    public bool IsAvailable { get; set; }

    /// <summary>
    /// HTTP 状态码
    /// </summary>
    [JsonPropertyName("status_code")]
    public int? StatusCode { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    [JsonPropertyName("response_time_ms")]
    public int? ResponseTimeMs { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 测试时间
    /// </summary>
    [JsonPropertyName("tested_at")]
    public DateTime TestedAt { get; set; }
}

/// <summary>
/// 测试结果汇总
/// </summary>
public class InstanceTestSummary
{
    /// <summary>
    /// 测试时间
    /// </summary>
    [JsonPropertyName("tested_at")]
    public DateTime TestedAt { get; set; }

    /// <summary>
    /// 总实例数
    /// </summary>
    [JsonPropertyName("total_instances")]
    public int TotalInstances { get; set; }

    /// <summary>
    /// 可用实例数
    /// </summary>
    [JsonPropertyName("available_instances")]
    public int AvailableInstances { get; set; }

    /// <summary>
    /// 不可用实例数
    /// </summary>
    [JsonPropertyName("unavailable_instances")]
    public int UnavailableInstances { get; set; }

    /// <summary>
    /// 所有测试结果
    /// </summary>
    [JsonPropertyName("results")]
    public List<InstanceTestResult> Results { get; set; } = new();
}

#endregion
