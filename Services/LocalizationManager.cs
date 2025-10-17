using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;

namespace AiComputer.Services;

/// <summary>
/// 多语言管理服务
/// </summary>
public class LocalizationManager
{
    private static LocalizationManager? _instance;
    private string _currentLanguage = "zh-CN";

    public static LocalizationManager Instance => _instance ??= new LocalizationManager();

    /// <summary>
    /// 当前语言
    /// </summary>
    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                LoadLanguage(value);
                LanguageChanged?.Invoke(this, value);
            }
        }
    }

    /// <summary>
    /// 语言更改事件
    /// </summary>
    public event EventHandler<string>? LanguageChanged;

    /// <summary>
    /// 支持的语言列表
    /// </summary>
    public List<LanguageInfo> SupportedLanguages { get; } = new()
    {
        new LanguageInfo("zh-CN", "简体中文"),
        new LanguageInfo("en-US", "English")
    };

    private LocalizationManager()
    {
    }

    /// <summary>
    /// 初始化多语言系统
    /// </summary>
    /// <param name="language">初始语言代码</param>
    public void Initialize(string language = "zh-CN")
    {
        _currentLanguage = language;
        LoadLanguage(language);
    }

    /// <summary>
    /// 加载指定语言的资源字典
    /// </summary>
    /// <param name="language">语言代码</param>
    private void LoadLanguage(string language)
    {
        try
        {
            var app = Application.Current;
            if (app == null)
            {
                Console.WriteLine("[Localization] Application.Current is null");
                return;
            }

            // 加载新的语言资源
            var languageUri = new Uri($"avares://AiComputer/Resources/Languages/{language}.axaml");
            var newLanguageDict = (ResourceDictionary)AvaloniaXamlLoader.Load(languageUri);

            if (newLanguageDict == null)
            {
                Console.WriteLine($"[Localization] Failed to load language dictionary for: {language}");
                return;
            }

            // 查找并移除旧的语言资源字典
            ResourceDictionary? oldLanguageDict = null;
            foreach (var dict in app.Resources.MergedDictionaries.OfType<ResourceDictionary>())
            {
                // 检查是否是语言资源字典（包含特定的键）
                if (dict.Count > 0 && dict.ContainsKey("App.Title"))
                {
                    oldLanguageDict = dict;
                    break;
                }
            }

            // 移除旧的语言资源
            if (oldLanguageDict != null)
            {
                app.Resources.MergedDictionaries.Remove(oldLanguageDict);
                Console.WriteLine("[Localization] Old language dictionary removed");
            }

            // 添加新的语言资源到最前面，确保优先级最高
            app.Resources.MergedDictionaries.Insert(0, newLanguageDict);

            Console.WriteLine($"[Localization] Language switched to: {language}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Localization] Failed to load language '{language}': {ex.Message}");
            Console.WriteLine($"[Localization] Stack trace: {ex.StackTrace}");

            // 如果加载失败，尝试加载默认语言
            if (language != "zh-CN")
            {
                Console.WriteLine("[Localization] Falling back to zh-CN");
                LoadLanguage("zh-CN");
            }
        }
    }

    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    /// <param name="key">资源键</param>
    /// <returns>本地化字符串</returns>
    public string GetString(string key)
    {
        try
        {
            var app = Application.Current;
            if (app?.Resources.TryGetResource(key, null, out var resource) == true && resource is string str)
            {
                return str;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get localized string for key '{key}': {ex.Message}");
        }

        return key; // 返回键作为后备
    }
}

/// <summary>
/// 语言信息
/// </summary>
public class LanguageInfo
{
    public string Code { get; }
    public string DisplayName { get; }

    public LanguageInfo(string code, string displayName)
    {
        Code = code;
        DisplayName = displayName;
    }
}
