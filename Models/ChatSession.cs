using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiComputer.Models;

/// <summary>
/// 聊天会话模型 - 代表一个独立的对话
/// </summary>
public partial class ChatSession : ObservableObject
{
    /// <summary>
    /// 会话 ID
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 会话标题
    /// </summary>
    [ObservableProperty]
    private string _title;

    /// <summary>
    /// 消息列表
    /// </summary>
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    [ObservableProperty]
    private DateTime _lastUpdated = DateTime.Now;

    /// <summary>
    /// 消息数量
    /// </summary>
    public int MessageCount => Messages.Count;

    /// <summary>
    /// 会话预览文本（显示第一条用户消息）
    /// </summary>
    public string PreviewText
    {
        get
        {
            var firstUserMessage = Messages.FirstOrDefault(m => m.Role == MessageRole.User);
            if (firstUserMessage != null && !string.IsNullOrWhiteSpace(firstUserMessage.Content))
            {
                var content = firstUserMessage.Content;
                return content.Length > 50 ? content.Substring(0, 50) + "..." : content;
            }
            return "新对话";
        }
    }

    /// <summary>
    /// 是否为空会话（没有消息）
    /// </summary>
    public bool IsEmpty => Messages.Count == 0;

    public ChatSession(string title = "新对话")
    {
        _title = title;

        // 监听消息集合变化，自动更新标题和时间
        Messages.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(MessageCount));
            OnPropertyChanged(nameof(PreviewText));
            OnPropertyChanged(nameof(IsEmpty));
            LastUpdated = DateTime.Now;

            // 如果标题还是默认的"新对话"，自动根据第一条消息更新标题
            if (Title == "新对话" || string.IsNullOrWhiteSpace(Title))
            {
                var firstUserMessage = Messages.FirstOrDefault(m => m.Role == MessageRole.User);
                if (firstUserMessage != null && !string.IsNullOrWhiteSpace(firstUserMessage.Content))
                {
                    var content = firstUserMessage.Content.Trim();
                    Title = content.Length > 30 ? content.Substring(0, 30) + "..." : content;
                }
            }
        };
    }
}
