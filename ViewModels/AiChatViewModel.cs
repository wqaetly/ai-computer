using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiComputer.Models;
using AiComputer.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiComputer.ViewModels;

/// <summary>
/// AI 聊天 ViewModel
/// </summary>
public partial class AiChatViewModel : ViewModelBase
{
    private readonly DeepSeekService _deepSeekService;
    private readonly SearchService _searchService;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// 消息列表
    /// </summary>
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    /// <summary>
    /// 用户输入的消息
    /// </summary>
    [ObservableProperty]
    private string _inputMessage = string.Empty;

    /// <summary>
    /// 是否正在发送消息
    /// </summary>
    [ObservableProperty]
    private bool _isSending;

    /// <summary>
    /// 是否显示欢迎界面（没有消息时显示）
    /// </summary>
    public bool ShowWelcomeScreen => Messages.Count == 0;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AiChatViewModel()
    {
        // 使用提供的 API Key
        _deepSeekService = new DeepSeekService("sk-e8ec7e0c860d4b7d98ffc4212ab2c138");

        // 初始化搜索服务
        _searchService = new SearchService();

        // 监听消息集合变化，更新欢迎界面显示状态
        Messages.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ShowWelcomeScreen));
    }

    /// <summary>
    /// 发送消息命令
    /// </summary>
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputMessage))
            return;

        var userMessage = InputMessage.Trim();
        InputMessage = string.Empty;

        // 添加用户消息
        var userMsg = new ChatMessage
        {
            Role = MessageRole.User,
            Content = userMessage,
            Timestamp = DateTime.Now
        };
        // 将内容添加到 ContentBuilder 以便 Markdown 渲染
        userMsg.ContentBuilder.Append(userMessage);

        // 等待用户消息添加完成，确保消息历史准备正确
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Messages.Add(userMsg);
        });

        // 创建 AI 回复消息
        var assistantMsg = new ChatMessage
        {
            Role = MessageRole.Assistant,
            Content = string.Empty,
            ReasoningContent = string.Empty,
            IsStreaming = true,
            Status = AiMessageStatus.Waiting,
            Timestamp = DateTime.Now
        };

        // 添加 AI 消息（可以用 Post，因为不需要等待）
        Dispatcher.UIThread.Post(() =>
        {
            Messages.Add(assistantMsg);
        });

        IsSending = true;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // 准备消息历史 - 包含当前用户消息，排除即将添加的助手消息
            var messageHistory = Messages
                .Where(m => m.Role != MessageRole.System)
                .ToList();

            // 调用 API - 在后台线程执行
            await Task.Run(async () =>
            {
                await _deepSeekService.ChatCompletionStreamAsync(
                    messageHistory,
                    reasoningChunk =>
                    {
                        // 推理内容回调 - 使用 Post 非阻塞更新UI
                        Dispatcher.UIThread.Post(() =>
                        {
                            // 第一次接收到推理内容时，自动展开并更新状态
                            if (assistantMsg.ReasoningContentBuilder.Length == 0)
                            {
                                assistantMsg.IsReasoningExpanded = true;
                                assistantMsg.Status = AiMessageStatus.Thinking;
                            }
                            // 使用 ObservableStringBuilder 的 Append 方法实时更新
                            assistantMsg.ReasoningContentBuilder.Append(reasoningChunk);
                            assistantMsg.ReasoningContent += reasoningChunk; // 保持字符串同步用于状态判断
                        });
                    },
                    contentChunk =>
                    {
                        // 回答内容回调 - 使用 Post 非阻塞更新UI
                        Dispatcher.UIThread.Post(() =>
                        {
                            // 第一次接收到回答内容时，更新状态
                            if (assistantMsg.ContentBuilder.Length == 0)
                            {
                                assistantMsg.Status = AiMessageStatus.Generating;
                            }
                            // 使用 ObservableStringBuilder 的 Append 方法实时更新
                            assistantMsg.ContentBuilder.Append(contentChunk);
                            assistantMsg.Content += contentChunk; // 保持字符串同步用于状态判断
                        });
                    },
                    async (toolName, query) =>
                    {
                        // 工具调用回调 - 执行联网搜索
                        if (toolName == "web_search")
                        {
                            // 更新状态为搜索中
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                assistantMsg.Status = AiMessageStatus.Searching;
                            });

                            Console.WriteLine($"🔍 AI 请求搜索: {query}");

                            // 执行搜索
                            var searchResults = await _searchService.SearchAsync(query, 5, _cancellationTokenSource!.Token);

                            // 格式化搜索结果
                            var formattedResults = SearchService.FormatSearchResults(searchResults);

                            Console.WriteLine($"✓ 搜索完成，找到 {searchResults.Count} 条结果");

                            return formattedResults;
                        }

                        return "未知的工具调用";
                    },
                    _cancellationTokenSource.Token
                );
            }, _cancellationTokenSource.Token).ConfigureAwait(false);

            // 流式传输结束，自动收起思考内容并更新状态
            Dispatcher.UIThread.Post(() =>
            {
                assistantMsg.IsStreaming = false;
                assistantMsg.IsReasoningExpanded = false;
                assistantMsg.Status = AiMessageStatus.Completed;
            });
        }
        catch (OperationCanceledException)
        {
            // 用户取消操作
            Dispatcher.UIThread.Post(() =>
            {
                assistantMsg.IsStreaming = false;
                assistantMsg.Status = AiMessageStatus.Cancelled;
                if (string.IsNullOrWhiteSpace(assistantMsg.Content))
                {
                    var cancelMsg = "已停止生成";
                    assistantMsg.Content = cancelMsg;
                    assistantMsg.ContentBuilder.Append(cancelMsg);
                }
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                assistantMsg.IsStreaming = false;
                assistantMsg.Status = AiMessageStatus.Error;
                var errorMsg = $"错误: {ex.Message}";
                assistantMsg.Content = errorMsg;
                assistantMsg.ContentBuilder.Append(errorMsg);
            });
        }
        finally
        {
            IsSending = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 停止生成命令
    /// </summary>
    [RelayCommand]
    private void StopGeneration()
    {
        _cancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// 清空对话命令
    /// </summary>
    [RelayCommand]
    private void ClearMessages()
    {
        Messages.Clear();
    }

    /// <summary>
    /// 切换推理内容展开/收起
    /// </summary>
    [RelayCommand]
    private void ToggleReasoning(ChatMessage message)
    {
        if (message != null)
        {
            message.IsReasoningExpanded = !message.IsReasoningExpanded;
        }
    }
}
