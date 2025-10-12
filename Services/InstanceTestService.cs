using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AiComputer.Services;

/// <summary>
/// 实例测试服务 - 用于测试 SearxNG 实例的可用性
/// </summary>
public class InstanceTestService
{
    private readonly HttpClient _httpClient;
    private readonly SearxSpaceApiService _apiService;
    private const string AvailableInstancesJsonPath = "Services/available_instances.json";

    public InstanceTestService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
            AllowAutoRedirect = true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(5) // 短超时，快速测试
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        _apiService = new SearxSpaceApiService();
    }

    /// <summary>
    /// 从 searx.space API 动态加载实例列表
    /// </summary>
    public async Task<List<InstanceInfo>> LoadInstancesFromApiAsync(
        CancellationToken cancellationToken = default,
        Action<string>? progressCallback = null)
    {
        try
        {
            progressCallback?.Invoke("正在从 searx.space API 获取实例列表...");
            var instances = await _apiService.FetchInstancesAsync(cancellationToken, progressCallback);

            if (instances.Count > 0)
            {
                progressCallback?.Invoke($"✓ 成功获取 {instances.Count} 个实例");
                return instances;
            }
            else
            {
                progressCallback?.Invoke("⚠ API 未返回任何实例");
                return new List<InstanceInfo>();
            }
        }
        catch (Exception ex)
        {
            progressCallback?.Invoke($"✗ API 获取失败: {ex.Message}");
            Console.WriteLine($"[InstanceTest] API 获取失败: {ex.Message}");
            return new List<InstanceInfo>();
        }
    }

    /// <summary>
    /// 测试单个实例的可用性
    /// </summary>
    public async Task<InstanceInfo> TestInstanceAsync(
        InstanceInfo instance,
        CancellationToken cancellationToken = default,
        Action<string>? progressCallback = null)
    {
        progressCallback?.Invoke($"测试中: {instance.Url}");

        var startTime = DateTime.Now;

        try
        {
            var response = await _httpClient.GetAsync(instance.Url, cancellationToken);

            instance.ResponseTime = (int)(DateTime.Now - startTime).TotalMilliseconds;
            instance.LastTestTime = DateTime.Now;

            if (response.IsSuccessStatusCode)
            {
                instance.Status = InstanceStatus.Available;
                progressCallback?.Invoke($"✓ 可用: {instance.Url} ({instance.ResponseTime}ms)");
            }
            else
            {
                instance.Status = InstanceStatus.Unavailable;
                instance.ErrorMessage = $"HTTP {(int)response.StatusCode}";
                progressCallback?.Invoke($"✗ 不可用: {instance.Url} - {instance.ErrorMessage}");
            }
        }
        catch (TaskCanceledException)
        {
            instance.Status = InstanceStatus.Timeout;
            instance.ErrorMessage = "超时";
            instance.ResponseTime = (int)(DateTime.Now - startTime).TotalMilliseconds;
            progressCallback?.Invoke($"⌛ 超时: {instance.Url}");
        }
        catch (OperationCanceledException)
        {
            instance.Status = InstanceStatus.Timeout;
            instance.ErrorMessage = "测试取消";
            progressCallback?.Invoke($"⊘ 取消: {instance.Url}");
        }
        catch (HttpRequestException ex)
        {
            instance.Status = InstanceStatus.Unavailable;
            instance.ErrorMessage = ex.Message;
            instance.ResponseTime = (int)(DateTime.Now - startTime).TotalMilliseconds;
            progressCallback?.Invoke($"✗ 不可用: {instance.Url} - {ex.Message}");
        }

        return instance;
    }

    /// <summary>
    /// 测试所有实例的可用性
    /// </summary>
    public async Task<List<InstanceInfo>> TestAllInstancesAsync(
        List<InstanceInfo> instances,
        CancellationToken cancellationToken = default,
        Action<string>? progressCallback = null,
        int maxConcurrency = 10)
    {
        progressCallback?.Invoke($"开始测试 {instances.Count} 个实例...");

        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = instances.Select(async instance =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await TestInstanceAsync(instance, cancellationToken, progressCallback);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        progressCallback?.Invoke($"测试完成！可用: {results.Count(i => i.Status == InstanceStatus.Available)}, " +
                                 $"不可用: {results.Count(i => i.Status == InstanceStatus.Unavailable)}, " +
                                 $"超时: {results.Count(i => i.Status == InstanceStatus.Timeout)}");

        return results.ToList();
    }

    /// <summary>
    /// 保存测试结果到 JSON 文件
    /// </summary>
    public void SaveResultsToJson(List<InstanceInfo> instances)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(instances, options);
            File.WriteAllText(AvailableInstancesJsonPath, json);

            Console.WriteLine($"[InstanceTest] 结果已保存到: {AvailableInstancesJsonPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InstanceTest] 保存结果失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从 JSON 文件加载测试结果
    /// </summary>
    public List<InstanceInfo> LoadResultsFromJson()
    {
        try
        {
            if (!File.Exists(AvailableInstancesJsonPath))
                return new List<InstanceInfo>();

            var json = File.ReadAllText(AvailableInstancesJsonPath);
            return JsonSerializer.Deserialize<List<InstanceInfo>>(json) ?? new List<InstanceInfo>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InstanceTest] 加载结果失败: {ex.Message}");
            return new List<InstanceInfo>();
        }
    }
}

/// <summary>
/// 实例信息
/// </summary>
public class InstanceInfo
{
    /// <summary>
    /// 实例 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public InstanceStatus Status { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public int ResponseTime { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 最后测试时间
    /// </summary>
    public DateTime LastTestTime { get; set; }
}

/// <summary>
/// 实例状态
/// </summary>
public enum InstanceStatus
{
    /// <summary>
    /// 未知
    /// </summary>
    Unknown,

    /// <summary>
    /// 可用
    /// </summary>
    Available,

    /// <summary>
    /// 不可用
    /// </summary>
    Unavailable,

    /// <summary>
    /// 超时
    /// </summary>
    Timeout
}
