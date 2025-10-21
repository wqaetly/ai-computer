using AiComputer.ViewModels;
using SukiUI.Controls;
using System.ComponentModel;
using System.Linq;

namespace AiComputer.Views;

public partial class MainWindow : SukiWindow
{
    public MainWindow()
    {
        InitializeComponent();

        // 获取对话框管理器并传递给 ViewModel
        // DialogHost 在 InitializeComponent 后应该已经可用
        DataContext = new MainWindowViewModel(DialogHost.Manager);

        // 监听窗口关闭事件，确保在退出时保存数据
        Closing += OnWindowClosing;
    }

    /// <summary>
    /// 窗口关闭前的处理 - 确保保存所有数据
    /// </summary>
    private async void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            // 查找 AiChatViewModel 并触发最终保存
            var aiChatViewModel = viewModel.Pages.OfType<AiChatViewModel>().FirstOrDefault();
            if (aiChatViewModel != null)
            {
                // 取消关闭，等待保存完成
                e.Cancel = true;

                // 执行保存
                await aiChatViewModel.SaveOnExitAsync();

                // 保存完成后真正关闭窗口
                Closing -= OnWindowClosing; // 移除事件处理器，避免递归
                Close();
            }
        }
    }
}