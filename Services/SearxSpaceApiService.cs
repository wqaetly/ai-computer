using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AiComputer.Services;

/// <summary>
/// Searx.Space API 服务 - 从 searx.space 动态获取实例列表
/// </summary>
public class SearxSpaceApiService
{
    private readonly HttpClient _httpClient;
    private const string ApiUrl = "https://searx.space/data/instances.json";
    private const int TimeoutSeconds = 30;

    public SearxSpaceApiService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
            AllowAutoRedirect = true,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    /// <summary>
    /// 从 searx.space API 获取实例列表
    /// </summary>
    public async Task<List<InstanceInfo>> FetchInstancesAsync(
        CancellationToken cancellationToken = default,
        Action<string>? progressCallback = null)
    {
        try
        {
            progressCallback?.Invoke("正在从 searx.space 获取实例列表...");

            var response = await _httpClient.GetAsync(ApiUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiData = JsonSerializer.Deserialize<SearxSpaceApiResponse>(jsonContent);

            if (apiData?.Instances == null || apiData.Instances.Count == 0)
            {
                progressCallback?.Invoke("⚠ API 返回了空的实例列表");
                return new List<InstanceInfo>();
            }

            var totalCount = apiData.Instances.Count;
            progressCallback?.Invoke($"API 返回了 {totalCount} 个实例，正在过滤...");

            // 转换为 InstanceInfo 列表，过滤出健康的实例
            var instances = apiData.Instances
                .Where(kvp => IsInstanceHealthy(kvp.Value))
                .Select(kvp => new InstanceInfo
                {
                    Url = kvp.Key,
                    Status = InstanceStatus.Unknown,
                    LastTestTime = DateTime.Now,
                    // 如果有响应时间数据，可以预填充
                    ResponseTime = GetAverageResponseTime(kvp.Value)
                })
                .ToList();

            var filteredCount = totalCount - instances.Count;
            progressCallback?.Invoke($"✓ 从 {totalCount} 个实例中筛选出 {instances.Count} 个可用实例（过滤掉 {filteredCount} 个）");

            return instances;
        }
        catch (HttpRequestException ex)
        {
            progressCallback?.Invoke($"✗ HTTP 请求失败: {ex.Message}");
            Console.WriteLine($"[SearxSpaceApi] HTTP 请求失败: {ex.Message}");
            return new List<InstanceInfo>();
        }
        catch (TaskCanceledException)
        {
            progressCallback?.Invoke("⊘ 请求超时");
            Console.WriteLine("[SearxSpaceApi] 请求超时");
            return new List<InstanceInfo>();
        }
        catch (JsonException ex)
        {
            progressCallback?.Invoke($"✗ JSON 解析失败: {ex.Message}");
            Console.WriteLine($"[SearxSpaceApi] JSON 解析失败: {ex.Message}");
            return new List<InstanceInfo>();
        }
        catch (Exception ex)
        {
            progressCallback?.Invoke($"✗ 未知错误: {ex.Message}");
            Console.WriteLine($"[SearxSpaceApi] 未知错误: {ex.Message}");
            return new List<InstanceInfo>();
        }
    }

    /// <summary>
    /// 判断实例是否健康（基于 API 数据）
    /// </summary>
    private bool IsInstanceHealthy(SearxInstanceData data)
    {
        // 检查 HTTP 对象是否存在
        if (data.Http == null)
            return false;

        // 检查 HTTP 状态码是否为成功（200）
        if (data.Http.StatusCode != 200)
            return false;

        // 检查是否有错误信息
        if (!string.IsNullOrWhiteSpace(data.Http.Error))
            return false;

        // 检查是否明确标记为离线（可选检查）
        if (data.Network?.Asn?.Name?.Contains("offline", StringComparison.OrdinalIgnoreCase) == true)
            return false;

        return true;
    }

    /// <summary>
    /// 获取平均响应时间（秒）
    /// </summary>
    private int GetAverageResponseTime(SearxInstanceData data)
    {
        try
        {
            if (data.Timing?.Search?.All?.Median != null)
            {
                // 转换为毫秒
                return (int)(data.Timing.Search.All.Median * 1000);
            }
        }
        catch
        {
            // 忽略解析错误
        }

        return 0;
    }
}

#region API 响应模型

/// <summary>
/// Searx.Space API 响应
/// </summary>
public class SearxSpaceApiResponse
{
    [JsonPropertyName("instances")]
    public Dictionary<string, SearxInstanceData> Instances { get; set; } = new();
}

/// <summary>
/// 单个实例的数据
/// </summary>
public class SearxInstanceData
{
    [JsonPropertyName("http")]
    public SearxHttpInfo? Http { get; set; }

    [JsonPropertyName("timing")]
    public SearxTimingInfo? Timing { get; set; }

    [JsonPropertyName("network")]
    public SearxNetworkInfo? Network { get; set; }

    [JsonPropertyName("tls")]
    public SearxTlsInfo? Tls { get; set; }
}

public class SearxHttpInfo
{
    [JsonPropertyName("status_code")]
    public int? StatusCode { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("grade")]
    public string? Grade { get; set; }

    [JsonPropertyName("gradeUrl")]
    public string? GradeUrl { get; set; }
}

public class SearxTimingInfo
{
    [JsonPropertyName("search")]
    public SearxSearchTiming? Search { get; set; }
}

public class SearxSearchTiming
{
    [JsonPropertyName("all")]
    public SearxTimingStats? All { get; set; }
}

public class SearxTimingStats
{
    [JsonPropertyName("median")]
    public double? Median { get; set; }

    [JsonPropertyName("mean")]
    public double? Mean { get; set; }

    [JsonPropertyName("stdev")]
    public double? Stdev { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class SearxNetworkInfo
{
    [JsonPropertyName("asn")]
    public SearxAsnInfo? Asn { get; set; }
}

public class SearxAsnInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

public class SearxTlsInfo
{
    [JsonPropertyName("grade")]
    public string? Grade { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }
}

#endregion
