using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AiComputer.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiComputer.ViewModels;

/// <summary>
/// 聊天界面ViewModel
/// </summary>
public partial class ChatViewModel : ViewModelBase
{
    /// <summary>
    /// 聊天消息集合
    /// </summary>
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    /// <summary>
    /// 用户输入的消息内容
    /// </summary>
    [ObservableProperty]
    private string _inputText = string.Empty;

    /// <summary>
    /// 是否正在等待AI回复
    /// </summary>
    [ObservableProperty]
    private bool _isWaitingForResponse = false;

    public ChatViewModel()
    {
        // 添加欢迎消息
        Messages.Add(new ChatMessage
        {
            Content = "你好！我是AI助手,很高兴为你服务。有什么我可以帮助你的吗?",
            Role = MessageRole.Assistant,
            Timestamp = DateTime.Now
        });
    }

    /// <summary>
    /// 发送消息命令
    /// </summary>
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
            return;

        // 添加用户消息
        var userMessage = new ChatMessage
        {
            Content = InputText,
            Role = MessageRole.User,
            Timestamp = DateTime.Now
        };
        Messages.Add(userMessage);

        // 清空输入框
        var userInput = InputText;
        InputText = string.Empty;

        // 模拟AI思考
        IsWaitingForResponse = true;
        await Task.Delay(1500);

        // 添加AI回复 (这里是模拟回复,实际项目中应该调用AI API)
        var aiMessage = new ChatMessage
        {
            Content = GenerateMockResponse(userInput),
            Role = MessageRole.Assistant,
            Timestamp = DateTime.Now
        };
        Messages.Add(aiMessage);

        IsWaitingForResponse = false;
    }

    /// <summary>
    /// 清空聊天记录命令
    /// </summary>
    [RelayCommand]
    private void ClearMessages()
    {
        Messages.Clear();
        Messages.Add(new ChatMessage
        {
            Content = "聊天记录已清空。有什么新问题吗?",
            Role = MessageRole.Assistant,
            Timestamp = DateTime.Now
        });
    }

    /// <summary>
    /// 生成模拟的AI回复
    /// </summary>
    private string GenerateMockResponse(string userInput)
    {
        var responses = new[]
        {
            $"我理解你说的是'{userInput}'。这是一个很有趣的话题!",
            $"关于'{userInput}',我可以为你提供一些信息...",
            "这是一个很好的问题!让我为你详细解答。",
            "我明白了,这确实是个值得探讨的话题。",
            "非常感谢你的提问!我很乐意帮助你。"
        };

        var random = new Random();
        return responses[random.Next(responses.Length)];
    }
}
