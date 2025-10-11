using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AiComputer.Services;

/// <summary>
/// SearXNG 实例测试程序
/// </summary>
public static class TestInstances
{
    /// <summary>
    /// 执行测试
    /// </summary>
    public static async Task RunAsync()
    {
        Console.WriteLine("=== SearXNG 实例可用性测试 ===\n");

        // 文件路径
        var yamlPath = Path.Combine("Services", "instances.yml");
        var fullReportPath = Path.Combine("Services", "instances_test_report.json");
        var availableOnlyPath = Path.Combine("Services", "available_instances.json");

        if (!File.Exists(yamlPath))
        {
            Console.WriteLine($"错误: 找不到文件 {yamlPath}");
            return;
        }

        // 创建测试器（降低并发数以避免触发速率限制）
        var tester = new SearxngInstanceTester(timeoutSeconds: 15, maxConcurrentTests: 3);

        // 解析实例
        Console.WriteLine("正在解析实例列表...");
        var instances = tester.ParseInstancesFromYaml(yamlPath);
        Console.WriteLine($"找到 {instances.Count} 个实例（已过滤 .onion 地址）\n");

        // 测试所有实例
        Console.WriteLine("开始测试实例可用性...");
        Console.WriteLine("这可能需要几分钟时间，请耐心等待...\n");

        var startTime = DateTime.Now;
        var results = await tester.TestAllInstancesAsync(
            instances,
            progressCallback: (completed, total) =>
            {
                Console.Write($"\r进度: {completed}/{total} ({completed * 100 / total}%)");
            }
        );
        var elapsed = DateTime.Now - startTime;

        Console.WriteLine($"\n\n测试完成! 耗时: {elapsed.TotalSeconds:F1} 秒\n");

        // 统计结果
        var available = results.FindAll(r => r.IsAvailable);
        var unavailable = results.FindAll(r => !r.IsAvailable);

        Console.WriteLine("=== 测试结果统计 ===");
        Console.WriteLine($"总实例数: {results.Count}");
        Console.WriteLine($"可用实例: {available.Count} ({available.Count * 100.0 / results.Count:F1}%)");
        Console.WriteLine($"不可用实例: {unavailable.Count} ({unavailable.Count * 100.0 / results.Count:F1}%)");

        if (available.Count > 0)
        {
            var avgResponseTime = available.Average(r => r.ResponseTimeMs ?? 0);
            var minResponseTime = available.Min(r => r.ResponseTimeMs ?? 0);
            var maxResponseTime = available.Max(r => r.ResponseTimeMs ?? 0);

            Console.WriteLine($"\n平均响应时间: {avgResponseTime:F0} ms");
            Console.WriteLine($"最快响应时间: {minResponseTime} ms");
            Console.WriteLine($"最慢响应时间: {maxResponseTime} ms");
        }

        // 显示前 10 个最快的可用实例
        Console.WriteLine("\n=== 前 10 个最快的可用实例 ===");
        var topInstances = available.OrderBy(r => r.ResponseTimeMs).Take(10).ToList();
        for (int i = 0; i < topInstances.Count; i++)
        {
            var instance = topInstances[i];
            Console.WriteLine($"{i + 1}. {instance.Url} ({instance.ResponseTimeMs} ms)");
        }

        // 保存完整测试报告
        Console.WriteLine($"\n正在保存完整测试报告到: {fullReportPath}");
        tester.SaveResultsToJson(results, fullReportPath);

        // 保存仅可用实例列表（用于运行时）
        Console.WriteLine($"正在保存可用实例列表到: {availableOnlyPath}");
        tester.SaveAvailableInstancesOnly(results, availableOnlyPath);

        Console.WriteLine("\n测试完成!");
        Console.WriteLine($"\n文件已保存:");
        Console.WriteLine($"  - 完整报告: {fullReportPath}");
        Console.WriteLine($"  - 可用实例: {availableOnlyPath}");
    }
}
