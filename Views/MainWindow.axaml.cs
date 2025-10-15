using AiComputer.ViewModels;
using SukiUI.Controls;

namespace AiComputer.Views;

public partial class MainWindow : SukiWindow
{
    public MainWindow()
    {
        InitializeComponent();
        
        // 获取对话框管理器并传递给 ViewModel
        // DialogHost 在 InitializeComponent 后应该已经可用
        DataContext = new MainWindowViewModel(DialogHost.Manager);
    }
}