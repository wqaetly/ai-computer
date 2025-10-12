using System.Text;

namespace AiComputer.Services.Tools;

/// <summary>
/// XML 标签提取器
/// 用于实时过滤流式内容中的工具调用标签
/// </summary>
public class TagExtractor
{
    private readonly string _openingTag;
    private readonly string _closingTag;
    private readonly StringBuilder _buffer = new();
    private bool _insideTag = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="openingTag">开始标签（如 "<tool_use>"）</param>
    /// <param name="closingTag">结束标签（如 "</tool_use>"）</param>
    public TagExtractor(string openingTag, string closingTag)
    {
        _openingTag = openingTag;
        _closingTag = closingTag;
    }

    /// <summary>
    /// 处理流式文本块
    /// </summary>
    /// <param name="chunk">文本块</param>
    /// <returns>过滤后的文本（不包含标签内容）</returns>
    public string ProcessChunk(string chunk)
    {
        if (string.IsNullOrEmpty(chunk))
            return string.Empty;

        // 将新内容添加到buffer
        _buffer.Append(chunk);
        var bufferContent = _buffer.ToString();
        var output = new StringBuilder();

        while (bufferContent.Length > 0)
        {
            if (_insideTag)
            {
                // 在标签内部，查找结束标签
                var endTagIndex = bufferContent.IndexOf(_closingTag);
                if (endTagIndex >= 0)
                {
                    // 找到结束标签，跳过标签内容和结束标签
                    bufferContent = bufferContent.Substring(endTagIndex + _closingTag.Length);
                    _insideTag = false;
                }
                else
                {
                    // 没有找到结束标签，整个buffer都在标签内，保留在buffer中
                    _buffer.Clear();
                    _buffer.Append(bufferContent);
                    return string.Empty;
                }
            }
            else
            {
                // 不在标签内部，查找开始标签
                var startTagIndex = bufferContent.IndexOf(_openingTag);
                if (startTagIndex >= 0)
                {
                    // 找到开始标签，输出标签前的内容
                    if (startTagIndex > 0)
                    {
                        output.Append(bufferContent.Substring(0, startTagIndex));
                    }
                    // 移除已输出的内容和开始标签
                    bufferContent = bufferContent.Substring(startTagIndex + _openingTag.Length);
                    _insideTag = true;
                }
                else
                {
                    // 没有找到开始标签
                    // 检查buffer末尾是否可能是开始标签的一部分
                    var safeOutputLength = bufferContent.Length;
                    for (int i = 1; i < _openingTag.Length && i <= bufferContent.Length; i++)
                    {
                        if (bufferContent.EndsWith(_openingTag.Substring(0, i)))
                        {
                            safeOutputLength = bufferContent.Length - i;
                            break;
                        }
                    }

                    // 输出安全部分
                    if (safeOutputLength > 0)
                    {
                        output.Append(bufferContent.Substring(0, safeOutputLength));
                        bufferContent = bufferContent.Substring(safeOutputLength);
                    }

                    // 保留可能的部分标签在buffer中
                    _buffer.Clear();
                    _buffer.Append(bufferContent);
                    break;
                }
            }
        }

        // 更新buffer
        if (bufferContent.Length == 0)
        {
            _buffer.Clear();
        }

        return output.ToString();
    }

    /// <summary>
    /// 完成处理，输出剩余内容
    /// </summary>
    /// <returns>剩余的非标签内容</returns>
    public string Flush()
    {
        if (_insideTag || _buffer.Length == 0)
        {
            _buffer.Clear();
            return string.Empty;
        }

        var remaining = _buffer.ToString();
        _buffer.Clear();
        return remaining;
    }

    /// <summary>
    /// 重置状态
    /// </summary>
    public void Reset()
    {
        _buffer.Clear();
        _insideTag = false;
    }
}
