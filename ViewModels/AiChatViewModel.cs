using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ai_computer.Services;
using AiComputer.Models;
using AiComputer.Services;
using AiComputer.Services.Tools;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconPacks.Avalonia;
using IconPacks.Avalonia.Material;
using SukiUI.Dialogs;

namespace AiComputer.ViewModels;

/// <summary>
/// AI 聊天 ViewModel - 支持多对话管理
/// </summary>
public partial class AiChatViewModel : PageBase
{
    private readonly DeepSeekService _deepSeekService;
    private readonly HybridSearchService _searchService;
    private readonly JDRecommendToolHelper _jdRecommendHelper;
    private readonly OcrService _ocrService;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// 对话框管理器
    /// </summary>
    private readonly ISukiDialogManager? _dialogManager;

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
    [NotifyPropertyChangedFor(nameof(SendButtonText))]
    private bool _isSending;

    /// <summary>
    /// 发送按钮文字（根据状态动态变化）
    /// </summary>
    public string SendButtonText => IsSending ? "停止" : "发送";

    /// <summary>
    /// 是否显示欢迎界面（没有消息时显示）
    /// </summary>
    public bool ShowWelcomeScreen => CurrentSession == null || CurrentSession.IsEmpty;

    /// <summary>
    /// 是否启用自动滚动到底部（当用户向上滚动时禁用，回到底部时启用）
    /// </summary>
    [ObservableProperty]
    private bool _isAutoScrollEnabled = true;

    /// <summary>
    /// 滚动阈值：距离底部多少像素内认为是"在底部"
    /// </summary>
    private const double ScrollThreshold = 50.0;

    /// <summary>
    /// ScrollViewer 引用（用于自动滚动）
    /// </summary>
    private Avalonia.Controls.ScrollViewer? _scrollViewer;

    /// <summary>
    /// 标记是否正在执行程序触发的自动滚动（用于区分用户手动滚动）
    /// </summary>
    private bool _isProgrammaticScroll = false;

    /// <summary>
    /// 是否正在拖拽图片（用于显示拖拽覆盖层）
    /// </summary>
    [ObservableProperty]
    private bool _isDraggingImage = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AiChatViewModel() : base("AI 聊天", PackIconMaterialKind.Chat, 0)
    {
        // 使用提供的 API Key
        _deepSeekService = new DeepSeekService("sk-e8ec7e0c860d4b7d98ffc4212ab2c138");

        // 初始化搜索服务（使用混合搜索，优先浏览器，降级到 SearxNG）
        _searchService = new HybridSearchService();

        // 初始化京东联盟推荐服务
        var httpClient = new HttpClient();
        var jdUnionService = new JDUnionService(httpClient);
        var jdRecommendService = new JDGoodsRecommendService(jdUnionService);
        _jdRecommendHelper = new JDRecommendToolHelper(jdRecommendService);

        // 初始化OCR服务
        _ocrService = new OcrService();
        // 异步初始化OCR服务（不阻塞构造函数）
        _ = _ocrService.InitializeAsync();

        // 注册工具
        RegisterTools();

        // 创建第一个默认会话
        CreateNewSession();
    }

    /// <summary>
    /// 构造函数（支持依赖注入）
    /// </summary>
    /// <param name="dialogManager">对话框管理器</param>
    public AiChatViewModel(ISukiDialogManager dialogManager) : base("AI 聊天", PackIconMaterialKind.Chat, 0)
    {
        _dialogManager = dialogManager;

        // 使用提供的 API Key
        _deepSeekService = new DeepSeekService("sk-e8ec7e0c860d4b7d98ffc4212ab2c138");

        // 初始化搜索服务（使用混合搜索，优先浏览器，降级到 SearxNG）
        _searchService = new HybridSearchService();

        // 初始化京东联盟推荐服务
        var httpClient = new HttpClient();
        var jdUnionService = new JDUnionService(httpClient);
        var jdRecommendService = new JDGoodsRecommendService(jdUnionService);
        _jdRecommendHelper = new JDRecommendToolHelper(jdRecommendService);

        // 初始化OCR服务
        _ocrService = new OcrService();
        // 异步初始化OCR服务（不阻塞构造函数）
        _ = _ocrService.InitializeAsync();

        // 注册工具
        RegisterTools();

        // 创建第一个默认会话
        CreateNewSession();
    }

    /// <summary>
    /// 处理粘贴的图片（保存为临时文件并以Markdown格式引用）
    /// </summary>
    public async Task HandlePastedImageAsync(Bitmap bitmap)
    {
        try
        {
            // 创建临时图片目录
            var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AiComputer", "Images");
            if (!System.IO.Directory.Exists(tempDir))
            {
                System.IO.Directory.CreateDirectory(tempDir);
            }

            // 生成唯一的文件名
            var fileName = $"pasted_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.png";
            var filePath = System.IO.Path.Combine(tempDir, fileName);

            // 保存Bitmap到文件
            await Task.Run(() =>
            {
                bitmap.Save(filePath);
            });

            // 将图片路径以Markdown格式添加到输入框
            HandleDraggedImagePath(filePath);

            Console.WriteLine($"已保存粘贴的图片: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理粘贴图片失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理拖拽的图片文件（以Markdown格式添加到输入框）
    /// </summary>
    public void HandleDraggedImagePath(string imagePath)
    {
        try
        {
            // 获取文件名（不含路径）
            var fileName = System.IO.Path.GetFileName(imagePath);

            // 构建Markdown格式的图片引用
            var markdownImage = $"![{fileName}]({imagePath})";

            // 添加到输入框
            if (!string.IsNullOrWhiteSpace(InputMessage))
            {
                // 如果输入框已有内容，在末尾添加（换行）
                InputMessage += "\n" + markdownImage;
            }
            else
            {
                InputMessage = markdownImage;
            }

            Console.WriteLine($"已添加图片引用: {markdownImage}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理拖拽图片路径失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 选择图片文件命令
    /// </summary>
    [RelayCommand]
    private async Task SelectImageAsync()
    {
        try
        {
            // 获取顶层窗口
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null)
            {
                Console.WriteLine("无法获取主窗口");
                return;
            }

            // 创建文件选择对话框
            var storageProvider = topLevel.StorageProvider;

            // 定义图片文件类型过滤器
            var fileTypeFilter = new Avalonia.Platform.Storage.FilePickerFileType("图片文件")
            {
                Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp" },
                MimeTypes = new[] { "image/*" }
            };

            var options = new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "选择图片文件",
                AllowMultiple = false,
                FileTypeFilter = new[] { fileTypeFilter }
            };

            // 打开文件选择对话框
            var files = await storageProvider.OpenFilePickerAsync(options);

            if (files.Count > 0)
            {
                var filePath = files[0].Path.LocalPath;

                // 直接将图片路径以Markdown格式添加到输入框（和拖拽图片一样的处理）
                HandleDraggedImagePath(filePath);

                Console.WriteLine($"已选择图片文件: {filePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"选择图片文件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 注册所有可用工具（根据设置动态注册）
    /// </summary>
    private void RegisterTools()
    {
        // 注册网络搜索工具
        var webSearchTool = new WebSearchTool(async (query) =>
        {
            var searchResults = await _searchService.SearchAsync(query, 5, CancellationToken.None);
            return SearchResultFormatter.FormatSearchResults(searchResults);
        });
        _deepSeekService.RegisterTool(webSearchTool);

        // 仅在启用京东价格查询时注册京东商品推荐工具
        if (AppSettingsService.Instance.EnableJDPriceQuery)
        {
            var jdProductTool = new JDProductRecommendTool(async (keyword, minPrice, maxPrice, count) =>
            {
                return await _jdRecommendHelper.RecommendAndFormatAsync(keyword, minPrice, maxPrice, count);
            });
            _deepSeekService.RegisterTool(jdProductTool);
            Console.WriteLine("[AiChat] 京东商品推荐工具已注册");
        }
        else
        {
            Console.WriteLine("[AiChat] 京东商品推荐工具未注册（功能已禁用）");
        }
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
        if (session == null) return;
        
        // 切换编辑状态
        session.IsEditing = true;
    }

    /// <summary>
    /// 完成重命名
    /// </summary>
    [RelayCommand]
    private void FinishRename(ChatSession session)
    {
        if (session == null) return;
        
        // 退出编辑状态
        session.IsEditing = false;
        
        // 如果标题为空，恢复默认标题
        if (string.IsNullOrWhiteSpace(session.Title))
        {
            session.Title = "新对话";
        }
    }

    /// <summary>
    /// 发送或停止命令（统一按钮）
    /// </summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task SendOrStopAsync()
    {
        if (IsSending)
        {
            // 如果正在发送，则停止
            StopGeneration();
            return;
        }

        // 否则发送消息
        await SendMessageAsync();
    }

    /// <summary>
    /// 发送消息命令
    /// </summary>
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

            // 添加新消息后滚动到底部
            ScrollToBottom();
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

            // 添加新消息后滚动到底部
            ScrollToBottom();
        });

        IsSending = true;
        _cancellationTokenSource = new CancellationTokenSource();

        // 使用数组包装以便在闭包中修改引用（工具调用后会切换到新气泡）
        var currentMsgHolder = new[] { assistantMsg };

        // 保存所有工具气泡的字典（key: toolName, value: bubble）
        var toolBubbles = new Dictionary<string, ChatMessage>();

        // 记录当前这轮工具调用的工具名称列表（用于工具完成回调）
        var currentRoundTools = new List<string>();

        try
        {
            // 根据设置决定是否使用深度思考模型
            var useReasoningModel = AppSettingsService.Instance.EnableDeepThinking;

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
                            var currentMsg = currentMsgHolder[0];
                            // 第一次接收到推理内容时，自动展开并更新状态
                            if (currentMsg.ReasoningContentBuilder.Length == 0)
                            {
                                currentMsg.IsReasoningExpanded = true;
                                currentMsg.Status = AiMessageStatus.Thinking;
                            }
                            // 使用 ObservableStringBuilder 的 Append 方法实时更新
                            currentMsg.ReasoningContentBuilder.Append(reasoningChunk);
                            currentMsg.ReasoningContent += reasoningChunk; // 保持字符串同步用于状态判断

                            // 自动滚动到底部（如果启用）
                            ScrollToBottom();
                        });
                    },
                    contentChunk =>
                    {
                        // 回答内容回调 - 使用 Post 非阻塞更新UI
                        Dispatcher.UIThread.Post(() =>
                        {
                            var currentMsg = currentMsgHolder[0];
                            // 第一次接收到回答内容时，更新状态
                            if (currentMsg.ContentBuilder.Length == 0)
                            {
                                currentMsg.Status = AiMessageStatus.Generating;
                            }
                            // 使用 ObservableStringBuilder 的 Append 方法实时更新
                            currentMsg.ContentBuilder.Append(contentChunk);
                            currentMsg.Content += contentChunk; // 保持字符串同步用于状态判断

                            // 自动滚动到底部（如果启用）
                            ScrollToBottom();
                        });
                    },
                    (toolName, toolArgs) =>
                    {
                        // 工具调用回调 - 根据不同工具提取参数并显示状态
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            string displayText;
                            string icon;

                            // 解析工具参数
                            var argsDoc = System.Text.Json.JsonDocument.Parse(toolArgs);
                            var argsRoot = argsDoc.RootElement;

                            // 根据工具类型提取不同的参数
                            if (toolName == "web_search")
                            {
                                var query = argsRoot.GetProperty("query").GetString() ?? "";
                                displayText = $"正在搜索: {query}";
                                icon = "🔍";
                                Console.WriteLine($"[UI] Tool called: web_search, query: {query}");
                            }
                            else if (toolName == "recommend_jd_product")
                            {
                                var keyword = argsRoot.GetProperty("keyword").GetString() ?? "";
                                var count = argsRoot.TryGetProperty("count", out var countProp) ? countProp.GetInt32() : 3;
                                displayText = $"正在推荐商品: {keyword} (数量: {count})";
                                icon = "🛒";
                                Console.WriteLine($"[UI] Tool called: recommend_jd_product, keyword: {keyword}, count: {count}");
                            }
                            else
                            {
                                // 未知工具
                                displayText = $"正在执行工具: {toolName}";
                                icon = "⚙️";
                                Console.WriteLine($"[UI] Tool called: {toolName}");
                            }

                            // 为每个工具创建独立的气泡
                            if (!toolBubbles.ContainsKey(toolName))
                            {
                                var toolBubble = new ChatMessage
                                {
                                    Role = MessageRole.Assistant,
                                    Content = $"{icon} {displayText}",
                                    IsStreaming = false,
                                    Status = AiMessageStatus.Searching,
                                    Timestamp = DateTime.Now,
                                    ToolName = toolName,
                                    ToolArguments = toolArgs
                                };
                                toolBubbles[toolName] = toolBubble;
                                CurrentSession.Messages.Add(toolBubble);
                                Console.WriteLine($"[UI] Created new tool bubble for: {toolName}");
                            }
                            else
                            {
                                // 更新已有工具气泡的状态（表示正在执行）
                                var existingBubble = toolBubbles[toolName];
                                existingBubble.Content = $"{icon} {displayText}";
                                existingBubble.ContentBuilder.Clear();
                                existingBubble.ContentBuilder.Append($"{icon} {displayText}");
                                Console.WriteLine($"[UI] Updated tool bubble for: {toolName}");
                            }

                            // 记录本轮调用的工具（用于工具完成回调）
                            if (!currentRoundTools.Contains(toolName))
                            {
                                currentRoundTools.Add(toolName);
                            }

                        }).Wait();
                    },
                    toolResults =>
                    {
                        // 工具完成回调 - 更新本轮调用的工具气泡状态和内容
                        Dispatcher.UIThread.Post(() =>
                        {
                            Console.WriteLine($"[UI] Tools completed, updating {currentRoundTools.Count} bubbles");

                            // 遍历本轮调用的所有工具，更新对应的气泡
                            foreach (var toolName in currentRoundTools)
                            {
                                if (toolBubbles.TryGetValue(toolName, out var toolBubble))
                                {
                                    Console.WriteLine($"[UI] Updating bubble for tool: {toolName}");

                                    // 更新状态
                                    toolBubble.Status = AiMessageStatus.SearchCompleted;

                                    // 根据工具类型格式化结果
                                    string formattedResults;
                                    if (toolName == "recommend_jd_product")
                                    {
                                        // 京东商品推荐结果已经格式化好，直接使用
                                        formattedResults = ExtractToolResult(toolResults);
                                        Console.WriteLine($"[UI] JD product recommendation completed");
                                    }
                                    else if (toolName == "web_search")
                                    {
                                        // 网络搜索结果需要格式化
                                        formattedResults = FormatToolResultsForUser(toolResults);
                                        Console.WriteLine($"[UI] Web search completed");
                                    }
                                    else
                                    {
                                        // 其他工具，提取原始结果
                                        formattedResults = ExtractToolResult(toolResults);
                                    }

                                    toolBubble.Content = formattedResults;
                                    toolBubble.ContentBuilder.Clear();
                                    toolBubble.ContentBuilder.Append(formattedResults);

                                    // 不再自动收起搜索结果，让用户自行控制
                                }
                            }

                            // 清空本轮工具列表，为下一轮准备
                            currentRoundTools.Clear();
                        });
                    },
                    useReasoningModel,
                    _cancellationTokenSource.Token
                );
            }, _cancellationTokenSource.Token).ConfigureAwait(false);

            // 流式传输结束，自动收起思考内容并更新状态
            Dispatcher.UIThread.Post(() =>
            {
                var currentMsg = currentMsgHolder[0];
                currentMsg.IsStreaming = false;
                currentMsg.IsReasoningExpanded = false;
                currentMsg.Status = AiMessageStatus.Completed;
            });
        }
        catch (OperationCanceledException)
        {
            // 用户取消操作
            Dispatcher.UIThread.Post(() =>
            {
                var currentMsg = currentMsgHolder[0];
                currentMsg.IsStreaming = false;
                currentMsg.Status = AiMessageStatus.Cancelled;
                if (string.IsNullOrWhiteSpace(currentMsg.Content))
                {
                    var cancelMsg = "已停止生成";
                    currentMsg.Content = cancelMsg;
                    currentMsg.ContentBuilder.Append(cancelMsg);
                }
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var currentMsg = currentMsgHolder[0];
                currentMsg.IsStreaming = false;
                currentMsg.Status = AiMessageStatus.Error;
                var errorMsg = $"错误: {ex.Message}";
                currentMsg.Content = errorMsg;
                currentMsg.ContentBuilder.Append(errorMsg);
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

    /// <summary>
    /// 切换搜索结果展开/收起
    /// </summary>
    [RelayCommand]
    private void ToggleSearchResult(ChatMessage message)
    {
        if (message != null)
        {
            message.IsSearchResultExpanded = !message.IsSearchResultExpanded;
        }
    }

    /// <summary>
    /// 处理滚动位置变化（用于检测用户是否主动滚动）
    /// </summary>
    /// <param name="scrollViewer">ScrollViewer 控件</param>
    public void OnScrollChanged(object? sender, Avalonia.Controls.ScrollChangedEventArgs e)
    {
        if (sender is not Avalonia.Controls.ScrollViewer scrollViewer)
            return;

        // 保存 ScrollViewer 引用
        _scrollViewer = scrollViewer;

        // 如果是程序触发的滚动，忽略此次事件（不改变自动滚动状态）
        if (_isProgrammaticScroll)
        {
            return;
        }

        // 计算是否在底部
        var offset = scrollViewer.Offset.Y;
        var extent = scrollViewer.Extent.Height;
        var viewport = scrollViewer.Viewport.Height;

        // 如果滚动位置 + 可视高度 >= 总高度 - 阈值，则认为在底部
        var isAtBottom = (offset + viewport) >= (extent - ScrollThreshold);

        // 用户手动滚动时：根据是否在底部更新自动滚动状态
        IsAutoScrollEnabled = isAtBottom;
    }

    /// <summary>
    /// 滚动到底部（在流式输出时调用）
    /// </summary>
    public void ScrollToBottom()
    {
        if (_scrollViewer == null || !IsAutoScrollEnabled)
            return;

        // 标记为程序触发的滚动
        _isProgrammaticScroll = true;

        // 使用 ScrollToEnd 方法滚动到底部
        Dispatcher.UIThread.Post(() =>
        {
            _scrollViewer?.ScrollToEnd();

            // 延迟重置标志，确保 ScrollChanged 事件已处理完成
            Task.Delay(100).ContinueWith(_ =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _isProgrammaticScroll = false;
                });
            });
        }, Avalonia.Threading.DispatcherPriority.Background);
    }

    /// <summary>
    /// 从XML格式的工具结果中提取实际内容
    /// </summary>
    private string ExtractToolResult(string toolResults)
    {
        var lines = toolResults.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var resultContent = new System.Text.StringBuilder();
        var inResult = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("<result>"))
            {
                inResult = true;
                var content = trimmedLine.Replace("<result>", "").Replace("</result>", "").Trim();
                if (!string.IsNullOrEmpty(content))
                {
                    resultContent.AppendLine(content);
                }
            }
            else if (trimmedLine.EndsWith("</result>"))
            {
                inResult = false;
                var content = trimmedLine.Replace("</result>", "").Trim();
                if (!string.IsNullOrEmpty(content))
                {
                    resultContent.AppendLine(content);
                }
            }
            else if (inResult && !trimmedLine.StartsWith("<"))
            {
                resultContent.AppendLine(trimmedLine);
            }
        }

        return resultContent.ToString().TrimEnd();
    }

    /// <summary>
    /// 格式化工具执行结果，供用户查看（精简版：只显示概述和链接）
    /// </summary>
    private string FormatToolResultsForUser(string toolResults)
    {
        // 解析XML格式的工具结果
        var lines = toolResults.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var formattedOutput = "### 📚 搜索概览\n\n";

        var inResult = false;
        var resultContent = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("<result>"))
            {
                inResult = true;
                var content = trimmedLine.Replace("<result>", "").Replace("</result>", "").Trim();
                if (!string.IsNullOrEmpty(content))
                {
                    resultContent.AppendLine(content);
                }
            }
            else if (trimmedLine.EndsWith("</result>"))
            {
                inResult = false;
                var content = trimmedLine.Replace("</result>", "").Trim();
                if (!string.IsNullOrEmpty(content))
                {
                    resultContent.AppendLine(content);
                }
            }
            else if (inResult && !trimmedLine.StartsWith("<"))
            {
                resultContent.AppendLine(trimmedLine);
            }
        }

        // 提取搜索结果中的各个条目，并格式化为简洁形式
        var resultText = resultContent.ToString();

        // 使用正则表达式提取搜索结果条目
        var pattern = @"(\d+)\.\s+\*\*(.+?)\*\*\s+来源:\s+(.+?)\s+链接:\s+(.+?)\s+摘要:\s+(.+?)(?=\n\d+\.\s+\*\*|\z)";
        var matches = System.Text.RegularExpressions.Regex.Matches(resultText, pattern, System.Text.RegularExpressions.RegexOptions.Singleline);

        if (matches.Count > 0)
        {
            var count = 0;
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                count++;
                var title = match.Groups[2].Value.Trim();
                var url = match.Groups[4].Value.Trim();
                var snippet = match.Groups[5].Value.Trim();

                // 精简摘要到50字以内
                if (snippet.Length > 50)
                {
                    snippet = snippet.Substring(0, 50) + "...";
                }

                formattedOutput += $"{count}. **[{title}]({url})**  \n";
                formattedOutput += $"   _{snippet}_\n\n";
            }

            formattedOutput += $"\n💡 共找到 {count} 条相关信息";
        }
        else
        {
            // 如果无法解析，显示简化的原始结果
            formattedOutput += "搜索已完成，结果已用于生成回答。";
        }

        return formattedOutput.TrimEnd();
    }
}
