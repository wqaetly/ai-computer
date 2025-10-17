using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiComputer.Models;
using AiComputer.Services;
using Avalonia.Collections;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconPacks.Avalonia;
using IconPacks.Avalonia.Material;
using SukiUI;
using SukiUI.Models;

namespace AiComputer.ViewModels;

/// <summary>
/// 设置页面 ViewModel
/// </summary>
public partial class SettingsViewModel : PageBase
{
    private readonly AppSettingsService _appSettings;
    private readonly InstanceTestService _testService;
    private readonly SukiTheme _theme = SukiTheme.GetInstance();
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// 设置类别列表
    /// </summary>
    public ObservableCollection<SettingCategory> Categories { get; }

    /// <summary>
    /// 可用的颜色主题列表
    /// </summary>
    public IAvaloniaReadOnlyList<SukiColorTheme> AvailableColors { get; }

    /// <summary>
    /// 可用的语言列表
    /// </summary>
    public List<LanguageInfo> AvailableLanguages { get; }

    /// <summary>
    /// 是否为亮色主题
    /// </summary>
    [ObservableProperty]
    private bool _isLightTheme;

    /// <summary>
    /// 当前选择的语言
    /// </summary>
    public LanguageInfo? SelectedLanguage
    {
        get => AvailableLanguages.FirstOrDefault(l => l.Code == _appSettings.Language);
        set
        {
            if (value != null && _appSettings.Language != value.Code)
            {
                _appSettings.Language = value.Code;
                OnPropertyChanged();
                Console.WriteLine($"[Settings] 语言已更改为: {value.Code}");
            }
        }
    }

    /// <summary>
    /// 当前选中的设置类别
    /// </summary>
    [ObservableProperty]
    private SettingCategory? _selectedCategory;

    /// <summary>
    /// 是否选中了外观类别
    /// </summary>
    public bool IsAppearanceSelected => SelectedCategory?.Id == "appearance";

    /// <summary>
    /// 是否选中了联网搜索类别
    /// </summary>
    public bool IsSearchSelected => SelectedCategory?.Id == "search";

    /// <summary>
    /// 是否选中了AI设置类别
    /// </summary>
    public bool IsAISelected => SelectedCategory?.Id == "ai";

    /// <summary>
    /// 所有可用的搜索服务商列表
    /// </summary>
    public List<SearchProviderItem> AvailableSearchProviders { get; }

    /// <summary>
    /// 当前选中的搜索服务商项
    /// </summary>
    public SearchProviderItem? SelectedSearchProviderItem
    {
        get => AvailableSearchProviders.FirstOrDefault(p => p.Provider == _appSettings.SearchProvider);
        set
        {
            if (value != null && _appSettings.SearchProvider != value.Provider)
            {
                _appSettings.SearchProvider = value.Provider;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSearxNG));
                Console.WriteLine($"[Settings] 搜索服务商已更改为: {value.Provider}");

                // 如果切换到 SearxNG，自动加载实例列表
                if (value.Provider == SearchProvider.SearxNG && Instances.Count == 0)
                {
                    _ = LoadInstancesFromApiAsync();
                }
            }
        }
    }

    /// <summary>
    /// 是否选择了 SearxNG 搜索提供商（用于条件显示测试区域）
    /// </summary>
    public bool IsSearxNG => _appSettings.SearchProvider == SearchProvider.SearxNG;

    /// <summary>
    /// 是否启用深度思考（DeepSeek API）
    /// </summary>
    public bool EnableDeepThinking
    {
        get => _appSettings.EnableDeepThinking;
        set
        {
            if (_appSettings.EnableDeepThinking != value)
            {
                _appSettings.EnableDeepThinking = value;
                OnPropertyChanged();
                Console.WriteLine($"[Settings] 深度思考已{(value ? "启用" : "禁用")}");
            }
        }
    }

    /// <summary>
    /// 所有可用的电商平台供应商列表
    /// </summary>
    public List<ECommerceProviderItem> AvailableECommerceProviders { get; }

    /// <summary>
    /// 当前选中的电商平台供应商项
    /// </summary>
    public ECommerceProviderItem? SelectedECommerceProviderItem
    {
        get => AvailableECommerceProviders.FirstOrDefault(p => p.Provider == _appSettings.ECommerceProvider);
        set
        {
            if (value != null && _appSettings.ECommerceProvider != value.Provider)
            {
                _appSettings.ECommerceProvider = value.Provider;
                OnPropertyChanged();
                Console.WriteLine($"[Settings] 电商平台已更改为: {value.Provider}");
            }
        }
    }

    /// <summary>
    /// SearxNG 实例列表
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
    private string _progressMessage = "点击\"加载实例\"按钮从 searx.space 获取 SearxNG 实例列表";

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

    /// <summary>
    /// 构造函数
    /// </summary>
    public SettingsViewModel() : base(LocalizationManager.Instance.GetString("Settings.Title"), PackIconMaterialKind.Cog, 99)
    {
        _appSettings = AppSettingsService.Instance;
        _testService = new InstanceTestService();

        // 初始化主题相关属性
        AvailableColors = _theme.ColorThemes;
        IsLightTheme = _theme.ActiveBaseTheme == ThemeVariant.Light;

        // 初始化语言列表
        AvailableLanguages = LocalizationManager.Instance.SupportedLanguages;

        // 监听主题变更事件
        _theme.OnBaseThemeChanged += variant =>
            IsLightTheme = variant == ThemeVariant.Light;

        // 监听配置服务的属性变更，同步到UI
        _appSettings.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AppSettingsService.SearchProvider))
            {
                OnPropertyChanged(nameof(SelectedSearchProviderItem));
                OnPropertyChanged(nameof(IsSearxNG));
            }
            else if (e.PropertyName == nameof(AppSettingsService.Language))
            {
                OnPropertyChanged(nameof(SelectedLanguage));
            }
        };

        // 初始化搜索服务商列表
        AvailableSearchProviders = new List<SearchProviderItem>
        {
            new SearchProviderItem
            {
                Provider = SearchProvider.Baidu,
                DisplayName = LocalizationManager.Instance.GetString("Settings.SearchProvider.Baidu"),
                Description = LocalizationManager.Instance.GetString("Settings.SearchProvider.Baidu.Desc")
            },
            new SearchProviderItem
            {
                Provider = SearchProvider.Bing,
                DisplayName = LocalizationManager.Instance.GetString("Settings.SearchProvider.Bing"),
                Description = LocalizationManager.Instance.GetString("Settings.SearchProvider.Bing.Desc")
            },
            new SearchProviderItem
            {
                Provider = SearchProvider.SearxNG,
                DisplayName = LocalizationManager.Instance.GetString("Settings.SearchProvider.SearxNG"),
                Description = LocalizationManager.Instance.GetString("Settings.SearchProvider.SearxNG.Desc")
            },
        };

        // 初始化电商平台供应商列表
        AvailableECommerceProviders = new List<ECommerceProviderItem>
        {
            new ECommerceProviderItem
            {
                Provider = ECommerceProvider.PinDuoDuo,
                DisplayName = LocalizationManager.Instance.GetString("Settings.ECommerce.PDD"),
                Description = LocalizationManager.Instance.GetString("Settings.ECommerce.PDD.Desc"),
                Icon = PackIconMaterialKind.Shopping
            },
            new ECommerceProviderItem
            {
                Provider = ECommerceProvider.JingDong,
                DisplayName = LocalizationManager.Instance.GetString("Settings.ECommerce.JD"),
                Description = LocalizationManager.Instance.GetString("Settings.ECommerce.JD.Desc"),
                Icon = PackIconMaterialKind.Store
            },
            new ECommerceProviderItem
            {
                Provider = ECommerceProvider.TaoBao,
                DisplayName = LocalizationManager.Instance.GetString("Settings.ECommerce.Taobao"),
                Description = LocalizationManager.Instance.GetString("Settings.ECommerce.Taobao.Desc"),
                Icon = PackIconMaterialKind.ShoppingOutline
            }
        };

        // 初始化设置类别列表
        Categories = new ObservableCollection<SettingCategory>
        {
            new SettingCategory
            {
                Id = "appearance",
                Name = LocalizationManager.Instance.GetString("Settings.Category.Appearance"),
                Icon = PackIconMaterialKind.Palette,
                Description = LocalizationManager.Instance.GetString("Settings.Category.Appearance.Desc")
            },
            new SettingCategory
            {
                Id = "ai",
                Name = LocalizationManager.Instance.GetString("Settings.Category.AI"),
                Icon = PackIconMaterialKind.Brain,
                Description = LocalizationManager.Instance.GetString("Settings.Category.AI.Desc")
            },
            new SettingCategory
            {
                Id = "search",
                Name = LocalizationManager.Instance.GetString("Settings.Category.Search"),
                Icon = PackIconMaterialKind.CloudSearch,
                Description = LocalizationManager.Instance.GetString("Settings.Category.Search.Desc")
            }
        };

        // 默认选中第一个类别
        SelectedCategory = Categories.FirstOrDefault();

        // 监听语言变化事件
        LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;

        // 如果当前选择的是 SearxNG，自动加载实例列表
        if (IsSearxNG)
        {
            _ = LoadInstancesFromApiAsync();
        }
    }

    /// <summary>
    /// 语言变化事件处理
    /// </summary>
    private void OnLanguageChanged(object? sender, string newLanguage)
    {
        // 更新页面标题
        DisplayName = LocalizationManager.Instance.GetString("Settings.Title");

        // 更新类别列表
        foreach (var category in Categories)
        {
            switch (category.Id)
            {
                case "appearance":
                    category.Name = LocalizationManager.Instance.GetString("Settings.Category.Appearance");
                    category.Description = LocalizationManager.Instance.GetString("Settings.Category.Appearance.Desc");
                    break;
                case "ai":
                    category.Name = LocalizationManager.Instance.GetString("Settings.Category.AI");
                    category.Description = LocalizationManager.Instance.GetString("Settings.Category.AI.Desc");
                    break;
                case "search":
                    category.Name = LocalizationManager.Instance.GetString("Settings.Category.Search");
                    category.Description = LocalizationManager.Instance.GetString("Settings.Category.Search.Desc");
                    break;
            }
        }

        // 更新搜索服务商列表
        foreach (var provider in AvailableSearchProviders)
        {
            switch (provider.Provider)
            {
                case SearchProvider.Baidu:
                    provider.DisplayName = LocalizationManager.Instance.GetString("Settings.SearchProvider.Baidu");
                    provider.Description = LocalizationManager.Instance.GetString("Settings.SearchProvider.Baidu.Desc");
                    break;
                case SearchProvider.Bing:
                    provider.DisplayName = LocalizationManager.Instance.GetString("Settings.SearchProvider.Bing");
                    provider.Description = LocalizationManager.Instance.GetString("Settings.SearchProvider.Bing.Desc");
                    break;
                case SearchProvider.SearxNG:
                    provider.DisplayName = LocalizationManager.Instance.GetString("Settings.SearchProvider.SearxNG");
                    provider.Description = LocalizationManager.Instance.GetString("Settings.SearchProvider.SearxNG.Desc");
                    break;
            }
        }

        // 更新电商平台供应商列表
        foreach (var provider in AvailableECommerceProviders)
        {
            switch (provider.Provider)
            {
                case ECommerceProvider.PinDuoDuo:
                    provider.DisplayName = LocalizationManager.Instance.GetString("Settings.ECommerce.PDD");
                    provider.Description = LocalizationManager.Instance.GetString("Settings.ECommerce.PDD.Desc");
                    break;
                case ECommerceProvider.JingDong:
                    provider.DisplayName = LocalizationManager.Instance.GetString("Settings.ECommerce.JD");
                    provider.Description = LocalizationManager.Instance.GetString("Settings.ECommerce.JD.Desc");
                    break;
                case ECommerceProvider.TaoBao:
                    provider.DisplayName = LocalizationManager.Instance.GetString("Settings.ECommerce.Taobao");
                    provider.Description = LocalizationManager.Instance.GetString("Settings.ECommerce.Taobao.Desc");
                    break;
            }
        }

        // 更新测试按钮文本
        TestButtonText = IsTesting
            ? LocalizationManager.Instance.GetString("Settings.SearxNG.StopTest")
            : LocalizationManager.Instance.GetString("Settings.SearxNG.StartTest");

        // 强制刷新UI
        OnPropertyChanged(nameof(SelectedSearchProviderItem));
        OnPropertyChanged(nameof(SelectedECommerceProviderItem));
    }

    /// <summary>
    /// 当亮色/暗色主题切换时调用
    /// </summary>
    partial void OnIsLightThemeChanged(bool value) =>
        _theme.ChangeBaseTheme(value ? ThemeVariant.Light : ThemeVariant.Dark);

    /// <summary>
    /// 当选中的类别改变时调用
    /// </summary>
    partial void OnSelectedCategoryChanged(SettingCategory? value)
    {
        OnPropertyChanged(nameof(IsAppearanceSelected));
        OnPropertyChanged(nameof(IsAISelected));
        OnPropertyChanged(nameof(IsSearchSelected));
    }

    /// <summary>
    /// 切换到指定颜色主题的命令
    /// </summary>
    [RelayCommand]
    private void SwitchToColorTheme(SukiColorTheme colorTheme) =>
        _theme.ChangeColorTheme(colorTheme);

    /// <summary>
    /// 从 searx.space API 加载实例列表
    /// </summary>
    private async Task LoadInstancesFromApiAsync()
    {
        ProgressMessage = "正在从 searx.space/data/instances.json 获取实例列表...";

        try
        {
            var instances = await _testService.LoadInstancesFromApiAsync(
                progressCallback: message =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        ProgressMessage = message;
                    });
                }
            );

            if (instances.Count > 0)
            {
                foreach (var instance in instances)
                {
                    Instances.Add(instance);
                }
                UpdateCounts();
                ProgressMessage = $"已获取 {instances.Count} 个 SearxNG 实例，点击 \"开始测试\" 按钮进行可用性测试";
            }
            else
            {
                ProgressMessage = "未能从 API 获取实例列表，请检查网络连接后点击 \"重新加载\" 重试";
            }
        }
        catch (Exception ex)
        {
            ProgressMessage = $"加载失败: {ex.Message}";
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
    /// 重新加载实例命令（从 API 获取最新列表）
    /// </summary>
    [RelayCommand]
    private async Task ReloadInstancesAsync()
    {
        if (IsTesting)
        {
            ProgressMessage = "请先停止测试再重新加载";
            return;
        }

        Instances.Clear();
        await LoadInstancesFromApiAsync();
    }
}

/// <summary>
/// 设置类别模型
/// </summary>
public partial class SettingCategory : ObservableObject
{
    /// <summary>
    /// 类别ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 类别名称
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    public PackIconMaterialKind Icon { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [ObservableProperty]
    private string _description = string.Empty;
}

/// <summary>
/// 搜索服务商项（用于UI显示）
/// </summary>
public partial class SearchProviderItem : ObservableObject
{
    /// <summary>
    /// 服务商枚举值
    /// </summary>
    public SearchProvider Provider { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    [ObservableProperty]
    private string _displayName = string.Empty;

    /// <summary>
    /// 描述信息
    /// </summary>
    [ObservableProperty]
    private string _description = string.Empty;
}

/// <summary>
/// 电商平台供应商项（用于UI显示）
/// </summary>
public partial class ECommerceProviderItem : ObservableObject
{
    /// <summary>
    /// 供应商枚举值
    /// </summary>
    public ECommerceProvider Provider { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    [ObservableProperty]
    private string _displayName = string.Empty;

    /// <summary>
    /// 描述信息
    /// </summary>
    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    public PackIconMaterialKind Icon { get; set; } = PackIconMaterialKind.Shopping;
}
