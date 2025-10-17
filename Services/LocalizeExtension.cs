using System;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace AiComputer.Services;

/// <summary>
/// 本地化标记扩展，支持动态语言切换
/// </summary>
public class LocalizeExtension : MarkupExtension
{
    public LocalizeExtension(string key)
    {
        Key = key;
    }

    /// <summary>
    /// 资源键
    /// </summary>
    public string Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // 创建一个 DynamicResourceExtension 来绑定资源
        var dynamicResource = new DynamicResourceExtension(Key);
        return dynamicResource.ProvideValue(serviceProvider);
    }
}
