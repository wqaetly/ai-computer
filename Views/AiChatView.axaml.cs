using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.LogicalTree;
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
}
