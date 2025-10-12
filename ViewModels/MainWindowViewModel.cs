using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiComputer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// 页面列表
    /// </summary>
    public ObservableCollection<PageBase> Pages { get; }

    /// <summary>
    /// 当前选中的页面
    /// </summary>
    private PageBase? _activePage;

    /// <summary>
    /// 当前选中的页面（公开属性，带重复值过滤）
    /// </summary>
    public PageBase? ActivePage
    {
        get => _activePage;
        set
        {
            // 重要：防止重复设置相同的值导致重复渲染
            if (ReferenceEquals(_activePage, value))
                return;

            SetProperty(ref _activePage, value);
            OnPropertyChanged(nameof(IsAiChatPageActive));
            OnPropertyChanged(nameof(AiChatViewModel));
        }
    }

    /// <summary>
    /// 是否为 AI 聊天页面
    /// </summary>
    public bool IsAiChatPageActive => ActivePage is AiChatViewModel;

    /// <summary>
    /// AI 聊天 ViewModel（用于访问对话列表）
    /// </summary>
    public AiChatViewModel? AiChatViewModel => ActivePage as AiChatViewModel;

    public MainWindowViewModel()
    {
        // 初始化页面列表
        Pages = new ObservableCollection<PageBase>
        {
            new AiChatViewModel(),
            new SettingsViewModel()
        };

        // 默认选中第一个页面（AI 聊天）
        _activePage = Pages.FirstOrDefault();
    }

    /// <summary>
    /// 选择页面命令
    /// </summary>
    [RelayCommand]
    private void SelectPage(PageBase? page)
    {
        if (page != null)
        {
            ActivePage = page;
        }
    }
}
