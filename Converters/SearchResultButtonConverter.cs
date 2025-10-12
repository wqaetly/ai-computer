using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AiComputer.Converters;

/// <summary>
/// 搜索结果按钮文本转换器
/// </summary>
public class SearchResultButtonConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? "收起" : "展开";
        }
        return "展开";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
