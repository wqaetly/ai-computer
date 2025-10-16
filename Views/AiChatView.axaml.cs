using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AiComputer.Models;
using AiComputer.ViewModels;

namespace AiComputer.Views;

public partial class AiChatView : UserControl
{
    // 保存所有思考区域ScrollViewer的弱引用，避免内存泄漏
    private readonly Dictionary<ChatMessage, WeakReference<ScrollViewer>> _reasoningScrollViewers = new();

    public AiChatView()
    {
        InitializeComponent();

        // 显式设置 AllowDrop 属性
        DragDrop.SetAllowDrop(this, true);
        Console.WriteLine("[AiChatView] AllowDrop 已设置为 true");

        // 订阅粘贴事件
        this.AddHandler(TextInputEvent, OnTextInput);

        // 订阅拖拽事件 - 使用 Tunnel 路由策略确保事件被捕获
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);

        Console.WriteLine("[AiChatView] 拖拽事件已注册");
    }

    /// <summary>
    /// 处理文本输入事件（包括粘贴）
    /// </summary>
    private async void OnTextInput(object? sender, TextInputEventArgs e)
    {
        // 检查是否是粘贴操作
        if (e.Text != null)
        {
            return; // 普通文本输入，不处理
        }

        // 尝试从剪贴板获取图片
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null)
            return;

        try
        {
            var formats = await clipboard.GetFormatsAsync();

            // 检查剪贴板是否包含图片
            if (formats.Contains("image/png") || formats.Contains("image/jpeg") ||
                formats.Contains("image/bmp") || formats.Contains("Bitmap"))
            {
                // 获取图片数据
                var data = await clipboard.GetDataAsync("Bitmap");
                if (data is Bitmap bitmap)
                {
                    await HandlePastedImageAsync(bitmap);
                    e.Handled = true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理粘贴图片时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理粘贴的图片
    /// </summary>
    private async Task HandlePastedImageAsync(Bitmap bitmap)
    {
        if (DataContext is AiChatViewModel viewModel)
        {
            await viewModel.HandlePastedImageAsync(bitmap);
        }
    }

    /// <summary>
    /// 处理滚动事件，用于检测用户是否主动滚动
    /// </summary>
    private void MessageScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (DataContext is AiChatViewModel viewModel)
        {
            viewModel.OnScrollChanged(sender, e);
        }
    }

    /// <summary>
    /// 处理思考区域 ScrollViewer 的大小变化事件，自动滚动到底部
    /// </summary>
    private void ReasoningScrollViewer_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            // 获取关联的ChatMessage（通过Tag）
            if (scrollViewer.Tag is ChatMessage message)
            {
                // 保存或更新ScrollViewer引用
                _reasoningScrollViewers[message] = new WeakReference<ScrollViewer>(scrollViewer);

                // 订阅消息的PropertyChanged事件（只订阅一次）
                if (!scrollViewer.Classes.Contains("subscribed"))
                {
                    scrollViewer.Classes.Add("subscribed");
                    message.PropertyChanged += OnMessagePropertyChanged;

                    // 当ScrollViewer从视觉树移除时，取消订阅
                    scrollViewer.DetachedFromLogicalTree += (s, args) =>
                    {
                        message.PropertyChanged -= OnMessagePropertyChanged;
                        _reasoningScrollViewers.Remove(message);
                    };
                }
            }

            // 延迟滚动，确保内容已渲染
            Dispatcher.UIThread.Post(() =>
            {
                scrollViewer.ScrollToEnd();
            }, DispatcherPriority.Background);
        }
    }

    /// <summary>
    /// 监听消息属性变化，当思考内容变化时自动滚动
    /// </summary>
    private void OnMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is ChatMessage message)
        {
            // 当ReasoningContent变化且思考内容已展开时，滚动到底部
            if (e.PropertyName == nameof(ChatMessage.ReasoningContent) && message.IsReasoningExpanded)
            {
                if (_reasoningScrollViewers.TryGetValue(message, out var weakRef) &&
                    weakRef.TryGetTarget(out var scrollViewer))
                {
                    // 延迟滚动，确保Markdown渲染完成
                    Dispatcher.UIThread.Post(() =>
                    {
                        scrollViewer.ScrollToEnd();
                    }, DispatcherPriority.Background);
                }
            }
        }
    }

    /// <summary>
    /// 检查是否包含图片文件
    /// </summary>
    private bool HasImageFiles(DragEventArgs e)
    {
        try
        {
            // 打印所有可用的数据格式，用于调试
            var formats = e.Data.GetDataFormats();
            Console.WriteLine($"[AiChatView] 拖拽数据包含的格式: {string.Join(", ", formats)}");

            // 尝试多种方式获取文件
            IEnumerable<IStorageItem>? files = null;

            // 方式1: 使用 DataFormats.Files
            if (e.Data.Contains(DataFormats.Files))
            {
                Console.WriteLine("[AiChatView] 尝试使用 DataFormats.Files 获取文件");
                files = e.Data.GetFiles();
            }

            // 方式2: 如果方式1失败，尝试使用 FileNames
            if ((files == null || !files.Any()) && e.Data.Contains(DataFormats.FileNames))
            {
                Console.WriteLine("[AiChatView] 尝试使用 DataFormats.FileNames 获取文件");
                var fileNames = e.Data.Get(DataFormats.FileNames) as IEnumerable<string>;
                if (fileNames != null)
                {
                    Console.WriteLine($"[AiChatView] 从 FileNames 获取到 {fileNames.Count()} 个文件路径");
                    // 这里我们只能检查文件名，无法直接获取 IStorageItem
                    // 但可以先检查扩展名
                    var hasImage = fileNames.Any(fileName =>
                    {
                        var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
                        var isImage = extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp";
                        Console.WriteLine($"[AiChatView] 文件路径: {fileName}, 扩展名: {extension}, 是图片: {isImage}");
                        return isImage;
                    });
                    return hasImage;
                }
            }

            // 检查是否成功获取文件
            if (files == null || !files.Any())
            {
                Console.WriteLine("[AiChatView] 无法获取文件列表");
                return false;
            }

            var fileList = files.ToList();
            Console.WriteLine($"[AiChatView] 成功获取 {fileList.Count} 个文件");

            var hasImageFile = fileList.Any(file =>
            {
                var extension = System.IO.Path.GetExtension(file.Path.LocalPath).ToLowerInvariant();
                var isImage = extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp";
                Console.WriteLine($"[AiChatView] 文件: {file.Name}, 路径: {file.Path.LocalPath}, 扩展名: {extension}, 是图片: {isImage}");
                return isImage;
            });

            Console.WriteLine($"[AiChatView] 包含图片文件: {hasImageFile}");
            return hasImageFile;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AiChatView] 检查文件出错: {ex.Message}");
            Console.WriteLine($"[AiChatView] 错误堆栈: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// 处理拖拽进入事件
    /// </summary>
    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        Console.WriteLine("[AiChatView] OnDragEnter 触发");

        if (HasImageFiles(e))
        {
            Console.WriteLine("[AiChatView] 显示拖拽覆盖层");
            if (DataContext is AiChatViewModel viewModel)
            {
                viewModel.IsDraggingImage = true;
            }
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    /// <summary>
    /// 处理拖拽悬停事件，检查是否是图片文件
    /// </summary>
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        // DragOver 必须持续返回正确的 DragEffects
        // 检查是否包含文件数据
        if (e.Data.Contains(DataFormats.Files) || e.Data.Contains(DataFormats.FileNames))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    /// <summary>
    /// 处理拖拽离开事件，隐藏拖拽覆盖层
    /// </summary>
    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        Console.WriteLine("[AiChatView] OnDragLeave 触发");

        // 隐藏拖拽覆盖层
        if (DataContext is AiChatViewModel viewModel)
        {
            viewModel.IsDraggingImage = false;
        }
    }

    /// <summary>
    /// 处理拖放事件，将图片路径以Markdown格式添加到输入框
    /// </summary>
    private async void OnDrop(object? sender, DragEventArgs e)
    {
        Console.WriteLine("[AiChatView] OnDrop 触发");

        try
        {
            // 隐藏拖拽覆盖层
            if (DataContext is not AiChatViewModel viewModel)
            {
                return;
            }

            viewModel.IsDraggingImage = false;

            // 尝试获取文件列表（支持多种方式）
            List<string> filePaths = new List<string>();

            // 方式1: 使用 DataFormats.Files
            if (e.Data.Contains(DataFormats.Files))
            {
                Console.WriteLine("[AiChatView] 从 DataFormats.Files 获取文件");
                var files = e.Data.GetFiles()?.ToList();
                if (files != null && files.Any())
                {
                    filePaths.AddRange(files.Select(f => f.Path.LocalPath));
                }
            }

            // 方式2: 使用 DataFormats.FileNames
            if (!filePaths.Any() && e.Data.Contains(DataFormats.FileNames))
            {
                Console.WriteLine("[AiChatView] 从 DataFormats.FileNames 获取文件");
                var fileNames = e.Data.Get(DataFormats.FileNames) as IEnumerable<string>;
                if (fileNames != null)
                {
                    filePaths.AddRange(fileNames);
                }
            }

            if (!filePaths.Any())
            {
                Console.WriteLine("[AiChatView] 无法获取文件列表");
                return;
            }

            Console.WriteLine($"[AiChatView] 准备处理 {filePaths.Count} 个文件");

            // 处理所有图片文件
            foreach (var filePath in filePaths)
            {
                var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
                Console.WriteLine($"[AiChatView] 处理文件: {filePath}, 扩展名: {extension}");

                // 检查是否是图片文件
                if (extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp")
                {
                    try
                    {
                        // 直接将图片路径以Markdown格式添加到输入框
                        viewModel.HandleDraggedImagePath(filePath);

                        Console.WriteLine($"[AiChatView] 成功添加图片引用: {System.IO.Path.GetFileName(filePath)}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AiChatView] 处理拖放图片时出错 ({filePath}): {ex.Message}");
                    }
                }
            }

            e.Handled = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AiChatView] 处理拖放操作时出错: {ex.Message}");
            Console.WriteLine($"[AiChatView] 错误堆栈: {ex.StackTrace}");
        }
    }
}
