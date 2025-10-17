using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AiComputer.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiComputer.Services;

/// <summary>
/// 应用设置服务 - 管理应用配置
/// </summary>
public partial class AppSettingsService : ObservableObject
{
    private static AppSettingsService? _instance;
    private static readonly object _lock = new();
    private readonly string _settingsFilePath;

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static AppSettingsService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new AppSettingsService();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 搜索服务提供商
    /// </summary>
    [ObservableProperty]
    private SearchProvider _searchProvider = SearchProvider.Baidu; // 默认使用百度

    /// <summary>
    /// 是否启用深度思考（DeepSeek API）
    /// </summary>
    [ObservableProperty]
    private bool _enableDeepThinking = false; // 默认禁用

    /// <summary>
    /// 电商平台供应商
    /// </summary>
    [ObservableProperty]
    private ECommerceProvider _eCommerceProvider = ECommerceProvider.PinDuoDuo; // 默认使用拼多多

    /// <summary>
    /// 应用程序语言
    /// </summary>
    [ObservableProperty]
    private string _language = "zh-CN"; // 默认中文

    /// <summary>
    /// 私有构造函数（单例模式）
    /// </summary>
    private AppSettingsService()
    {
        // 设置配置文件路径（保存在应用数据目录）
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataFolder, "AiComputer");

        // 确保目录存在
        Directory.CreateDirectory(appFolder);

        _settingsFilePath = Path.Combine(appFolder, "settings.json");

        // 加载设置
        _ = LoadSettingsAsync();

        // 监听属性变更，自动保存
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SearchProvider) ||
                e.PropertyName == nameof(EnableDeepThinking) ||
                e.PropertyName == nameof(ECommerceProvider) ||
                e.PropertyName == nameof(Language))
            {
                _ = SaveSettingsAsync();

                // 语言变更时，更新 LocalizationManager
                if (e.PropertyName == nameof(Language))
                {
                    LocalizationManager.Instance.CurrentLanguage = Language;
                }
            }
        };
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                Console.WriteLine("[AppSettings] 配置文件不存在，使用默认设置");
                return;
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettingsData>(json);

            if (settings != null)
            {
                SearchProvider = settings.SearchProvider;
                EnableDeepThinking = settings.EnableDeepThinking;
                ECommerceProvider = settings.ECommerceProvider;
                Language = settings.Language;
                Console.WriteLine($"[AppSettings] 已加载配置: SearchProvider={SearchProvider}, EnableDeepThinking={EnableDeepThinking}, ECommerceProvider={ECommerceProvider}, Language={Language}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppSettings] 加载配置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        try
        {
            var settings = new AppSettingsData
            {
                SearchProvider = SearchProvider,
                EnableDeepThinking = EnableDeepThinking,
                ECommerceProvider = ECommerceProvider,
                Language = Language
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(settings, options);
            await File.WriteAllTextAsync(_settingsFilePath, json);

            Console.WriteLine($"[AppSettings] 已保存配置: SearchProvider={SearchProvider}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppSettings] 保存配置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 手动保存设置（供外部调用）
    /// </summary>
    public Task SaveAsync() => SaveSettingsAsync();

    /// <summary>
    /// 手动加载设置（供外部调用）
    /// </summary>
    public Task LoadAsync() => LoadSettingsAsync();
}

/// <summary>
/// 应用设置数据模型（用于序列化）
/// </summary>
internal class AppSettingsData
{
    public SearchProvider SearchProvider { get; set; } = SearchProvider.Baidu; // 默认百度
    public bool EnableDeepThinking { get; set; } = false; // 默认禁用深度思考
    public ECommerceProvider ECommerceProvider { get; set; } = ECommerceProvider.PinDuoDuo; // 默认拼多多
    public string Language { get; set; } = "zh-CN"; // 默认中文
}
