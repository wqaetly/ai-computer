using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AiComputer.Converters;

/// <summary>
/// 推理内容按钮文本转换器
/// </summary>
public class ReasoningButtonConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? "🧠 收起思考过程" : "🧠 查看思考过程";
        }
        return "🧠 查看思考过程";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
