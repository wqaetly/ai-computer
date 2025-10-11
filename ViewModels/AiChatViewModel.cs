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
using Material.Icons;

namespace AiComputer.ViewModels;

/// <summary>
/// AI 聊天 ViewModel - 支持多对话管理
/// </summary>
public partial class AiChatViewModel : PageBase
{
    private readonly DeepSeekService _deepSeekService;
    private readonly SearchService _searchService;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// 所有对话会话列表
    /// </summary>
    public ObservableCollection<ChatSession> Sessions { get; } = new();

    /// <summary>
    /// 当前选中的对话会话
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Messages))]
    [NotifyPropertyChangedFor(nameof(ShowWelcomeScreen))]
    private ChatSession? _currentSession;

    /// <summary>
    /// 当前会话的消息列表（用于UI绑定）
    /// </summary>
    public ObservableCollection<ChatMessage>? Messages => CurrentSession?.Messages;

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
    public bool ShowWelcomeScreen => CurrentSession == null || CurrentSession.IsEmpty;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AiChatViewModel() : base("AI 聊天", MaterialIconKind.Chat, 0)
    {
        // 使用提供的 API Key
        _deepSeekService = new DeepSeekService("sk-e8ec7e0c860d4b7d98ffc4212ab2c138");

        // 初始化搜索服务
        _searchService = new SearchService();

        // 创建第一个默认会话
        CreateNewSession();
    }

    /// <summary>
    /// 切换会话
    /// </summary>
    [RelayCommand]
    private void SwitchSession(ChatSession session)
    {
        if (session != null && session != CurrentSession)
        {
            CurrentSession = session;
        }
    }

    /// <summary>
    /// 创建新会话
    /// </summary>
    [RelayCommand]
    private void CreateNewSession()
    {
        var newSession = new ChatSession($"对话 {Sessions.Count + 1}");
        Sessions.Add(newSession);
        CurrentSession = newSession;

        // 监听新会话的消息变化
        newSession.Messages.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ShowWelcomeScreen));
        };
    }

    /// <summary>
    /// 删除会话
    /// </summary>
    [RelayCommand]
    private void DeleteSession(ChatSession session)
    {
        if (session == null || Sessions.Count <= 1)
            return; // 至少保留一个会话

        var index = Sessions.IndexOf(session);
        Sessions.Remove(session);

        // 如果删除的是当前会话，切换到相邻的会话
        if (session == CurrentSession)
        {
            if (Sessions.Count > 0)
            {
                // 优先选择后一个，如果没有则选择前一个
                CurrentSession = index < Sessions.Count ? Sessions[index] : Sessions[Sessions.Count - 1];
            }
            else
            {
                // 如果没有会话了，创建一个新的
                CreateNewSession();
            }
        }
    }

    /// <summary>
    /// 重命名会话
    /// </summary>
    [RelayCommand]
    private void RenameSession(ChatSession session)
    {
        if (session != null)
        {
            // TODO: 可以实现一个对话框来输入新标题
            // 这里暂时使用简单的方式
        }
    }

    /// <summary>
    /// 发送消息命令
    /// </summary>
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputMessage) || CurrentSession == null)
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
            CurrentSession.Messages.Add(userMsg);
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
            CurrentSession.Messages.Add(assistantMsg);
        });

        IsSending = true;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // 准备消息历史 - 包含当前用户消息，排除即将添加的助手消息
            var messageHistory = CurrentSession.Messages
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
    /// 清空当前对话命令
    /// </summary>
    [RelayCommand]
    private void ClearMessages()
    {
        CurrentSession?.Messages.Clear();
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
