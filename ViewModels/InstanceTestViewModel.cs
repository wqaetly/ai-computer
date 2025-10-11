using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiComputer.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconPacks.Avalonia;
using IconPacks.Avalonia.Material;

namespace AiComputer.ViewModels;

/// <summary>
/// 实例测试页面 ViewModel
/// </summary>
public partial class InstanceTestViewModel : PageBase
{
    private readonly InstanceTestService _testService;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// 实例列表
    /// </summary>
    public ObservableCollection<InstanceInfo> Instances { get; } = new();

    /// <summary>
    /// 是否正在测试
    /// </summary>
    [ObservableProperty]
    private bool _isTesting;

    /// <summary>
    /// 测试进度消息
    /// </summary>
    [ObservableProperty]
    private string _progressMessage = "点击 \"开始测试\" 按钮开始测试实例可用性";

    /// <summary>
    /// 可用实例数量
    /// </summary>
    [ObservableProperty]
    private int _availableCount;

    /// <summary>
    /// 不可用实例数量
    /// </summary>
    [ObservableProperty]
    private int _unavailableCount;

    /// <summary>
    /// 超时实例数量
    /// </summary>
    [ObservableProperty]
    private int _timeoutCount;

    /// <summary>
    /// 总数量
    /// </summary>
    [ObservableProperty]
    private int _totalCount;

    /// <summary>
    /// 测试按钮文字
    /// </summary>
    [ObservableProperty]
    private string _testButtonText = "开始测试";

    /// <summary>
    /// 测试按钮图标
    /// </summary>
    [ObservableProperty]
    private PackIconMaterialKind _testButtonIcon = PackIconMaterialKind.Play;

    public InstanceTestViewModel() : base("联网搜索测试", PackIconMaterialKind.Web, 1)
    {
        _testService = new InstanceTestService();
        LoadInstances();
    }

    /// <summary>
    /// 加载实例列表
    /// </summary>
    private void LoadInstances()
    {
        // 先尝试加载已保存的测试结果
        var savedInstances = _testService.LoadResultsFromJson();

        if (savedInstances.Count > 0)
        {
            foreach (var instance in savedInstances)
            {
                Instances.Add(instance);
            }
            UpdateCounts();
            ProgressMessage = $"已加载 {savedInstances.Count} 个实例的历史测试结果";
        }
        else
        {
            // 如果没有保存的结果，从 YAML 加载
            var instances = _testService.LoadInstancesFromYml();
            foreach (var instance in instances)
            {
                Instances.Add(instance);
            }
            TotalCount = instances.Count;
            ProgressMessage = $"已加载 {instances.Count} 个实例，等待测试";
        }
    }

    /// <summary>
    /// 开始测试命令
    /// </summary>
    [RelayCommand]
    private async Task StartTestAsync()
    {
        if (IsTesting)
        {
            // 如果正在测试，则停止测试
            StopTest();
            return;
        }

        IsTesting = true;
        TestButtonText = "停止测试";
        TestButtonIcon = PackIconMaterialKind.Stop;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // 重置所有实例状态
            foreach (var instance in Instances)
            {
                instance.Status = InstanceStatus.Unknown;
                instance.ErrorMessage = null;
                instance.ResponseTime = 0;
            }
            UpdateCounts();

            ProgressMessage = "正在测试...";

            var instances = Instances.ToList();

            await _testService.TestAllInstancesAsync(
                instances,
                _cancellationTokenSource.Token,
                progressCallback: message =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        ProgressMessage = message;
                        UpdateCounts();
                    });
                },
                maxConcurrency: 15 // 并发测试 15 个
            );

            // 保存结果
            _testService.SaveResultsToJson(instances);

            UpdateCounts();
            ProgressMessage = $"测试完成！可用: {AvailableCount}, 不可用: {UnavailableCount}, 超时: {TimeoutCount}";
        }
        catch (OperationCanceledException)
        {
            ProgressMessage = "测试已取消";
        }
        catch (Exception ex)
        {
            ProgressMessage = $"测试失败: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
            TestButtonText = "开始测试";
            TestButtonIcon = PackIconMaterialKind.Play;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 停止测试
    /// </summary>
    private void StopTest()
    {
        _cancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// 更新统计数量
    /// </summary>
    private void UpdateCounts()
    {
        AvailableCount = Instances.Count(i => i.Status == InstanceStatus.Available);
        UnavailableCount = Instances.Count(i => i.Status == InstanceStatus.Unavailable);
        TimeoutCount = Instances.Count(i => i.Status == InstanceStatus.Timeout);
        TotalCount = Instances.Count;
    }

    /// <summary>
    /// 重新加载实例命令
    /// </summary>
    [RelayCommand]
    private void ReloadInstances()
    {
        Instances.Clear();
        LoadInstances();
    }
}
